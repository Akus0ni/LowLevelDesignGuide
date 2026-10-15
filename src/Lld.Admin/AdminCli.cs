using System.CommandLine;
using System.Globalization;
using Lld.Application.Abstractions;
using Lld.Application.Access;
using Lld.Application.Accounts;
using Lld.Domain.Access;
using Lld.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Admin;

/// <summary>
/// The admin console's command tree. Each command opens a DI scope, runs one use case and prints the result.
/// It is another client of the same application layer the API uses.
/// </summary>
internal static class AdminCli
{
    public const int Success = 0;
    public const int Failure = 1;

    public static Task<int> RunAsync(string[] args, IServiceProvider services, TextWriter output, TextWriter error)
    {
        var root = new RootCommand("LLD Guide admin console: access requests, users, the audit log and self-hosting.")
        {
            RequestsCommand(services, output, error),
            UsersCommand(services, output, error),
            AuditCommand(services, output),
        };
        foreach (var command in HostingCommands.All(services, output, error))
        {
            root.Subcommands.Add(command);
        }

        var invocation = new InvocationConfiguration { Output = output, Error = error };
        return root.Parse(args).InvokeAsync(invocation);
    }

    private static Command RequestsCommand(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var all = new Option<bool>("--all") { Description = "Include approved and rejected requests." };
        var list = new Command("list", "List access requests (pending only unless --all).") { all };
        list.SetAction(async (parse, ct) =>
        {
            var status = parse.GetValue(all) ? (AccessRequestStatus?)null : AccessRequestStatus.Pending;
            var requests = await Run<ListAccessRequests, IReadOnlyList<AccessRequest>>(services, u => u.ExecuteAsync(status, ct));
            Table.Write(output, ["ID", "EMAIL", "NAME", "STATUS", "REQUESTED (UTC)", "NOTE"], requests.Select(r => new[]
            {
                ShortId(r.Id), r.Email.Value, r.Name, r.Status.ToString(), Utc(r.CreatedAt), r.Note ?? string.Empty,
            }));
            return Success;
        });

        var approveId = new Argument<string>("id") { Description = "Request id from 'requests list' (its first characters are enough), or the email of a pending request." };
        var approve = new Command("approve", "Approve a request and create the account with a temporary password.") { approveId };
        approve.SetAction(async (parse, ct) =>
        {
            var id = await ResolveRequestIdAsync(services, parse.GetValue(approveId)!, ct);
            if (id.IsFailure)
            {
                return Fail(error, id.Error!);
            }

            var issued = await Run<ApproveAccessRequest, Result<IssuedCredentials>>(services, u => u.ExecuteAsync(id.Value, ct));
            return issued.IsSuccess ? PrintCredentials(output, "Approved access for", issued.Value) : Fail(error, issued.Error!);
        });

        var rejectId = new Argument<string>("id") { Description = "Request id from 'requests list' (first characters), or the email of a pending request." };
        var reject = new Command("reject", "Reject a pending request.") { rejectId };
        reject.SetAction(async (parse, ct) =>
        {
            var id = await ResolveRequestIdAsync(services, parse.GetValue(rejectId)!, ct);
            if (id.IsFailure)
            {
                return Fail(error, id.Error!);
            }

            var rejected = await Run<RejectAccessRequest, Result>(services, u => u.ExecuteAsync(id.Value, ct));
            if (rejected.IsFailure)
            {
                return Fail(error, rejected.Error!);
            }

            output.WriteLine("Request rejected.");
            return Success;
        });

        return new Command("requests", "Review access requests.") { list, approve, reject };
    }

    private static Command UsersCommand(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var createEmail = new Argument<string>("email");
        var name = new Option<string?>("--name") { Description = "Display name (defaults to the email)." };
        var role = new Option<string>("--role") { Description = "admin or learner.", DefaultValueFactory = _ => "learner" };
        role.AcceptOnlyFromAmong("admin", "learner");
        var create = new Command("create", "Create a user with a temporary password.") { createEmail, name, role };
        create.SetAction(async (parse, ct) =>
        {
            var roleName = parse.GetValue(role) == "admin" ? Roles.Admin : Roles.Learner;
            var command = new CreateUserCommand(parse.GetValue(createEmail)!, parse.GetValue(name), roleName);
            var issued = await Run<CreateUser, Result<IssuedCredentials>>(services, u => u.ExecuteAsync(command, ct));
            return issued.IsSuccess ? PrintCredentials(output, $"Created {roleName.ToLowerInvariant()}", issued.Value) : Fail(error, issued.Error!);
        });

        var resetEmail = new Argument<string>("email");
        var reset = new Command("reset-password", "Issue a new temporary password and sign the user out everywhere.") { resetEmail };
        reset.SetAction(async (parse, ct) =>
        {
            var issued = await Run<ResetPassword, Result<IssuedCredentials>>(services, u => u.ExecuteAsync(parse.GetValue(resetEmail)!, ct));
            return issued.IsSuccess ? PrintCredentials(output, "Reset the password of", issued.Value) : Fail(error, issued.Error!);
        });

        var disableEmail = new Argument<string>("email");
        var disable = new Command("disable", "Disable a user and end all their sessions.") { disableEmail };
        disable.SetAction(async (parse, ct) =>
        {
            var disabled = await Run<DisableUser, Result>(services, u => u.ExecuteAsync(parse.GetValue(disableEmail)!, ct));
            if (disabled.IsFailure)
            {
                return Fail(error, disabled.Error!);
            }

            output.WriteLine("User disabled.");
            return Success;
        });

        var list = new Command("list", "List users.");
        list.SetAction(async (_, ct) =>
        {
            var users = await Run<ListUsers, IReadOnlyList<UserAccount>>(services, u => u.ExecuteAsync(ct));
            Table.Write(output, ["EMAIL", "NAME", "ROLES", "STATUS"], users.Select(u => new[]
            {
                u.Email.Value,
                u.DisplayName,
                string.Join(',', u.Roles),
                u.IsDisabled ? "disabled" : u.MustChangePassword ? "must change password" : "active",
            }));
            return Success;
        });

        return new Command("users", "Manage users.") { create, reset, disable, list };
    }

    private static Command AuditCommand(IServiceProvider services, TextWriter output)
    {
        var take = new Option<int>("--take") { Description = "How many entries (newest first).", DefaultValueFactory = _ => ListAuditEntries.DefaultTake };
        var list = new Command("list", "Show recent security events.") { take };
        list.SetAction(async (parse, ct) =>
        {
            var entries = await Run<ListAuditEntries, IReadOnlyList<Domain.Audit.AuditEntry>>(services, u => u.ExecuteAsync(parse.GetValue(take), ct));
            Table.Write(output, ["WHEN (UTC)", "EVENT", "EMAIL", "DETAILS"], entries.Select(e => new[]
            {
                Utc(e.OccurredAt), e.Type.ToString(), e.Email ?? string.Empty, e.Details ?? string.Empty,
            }));
            return Success;
        });

        return new Command("audit", "Read the audit log.") { list };
    }

    internal static async Task<TResult> Run<TUseCase, TResult>(IServiceProvider services, Func<TUseCase, Task<TResult>> action)
        where TUseCase : notnull
    {
        await using var scope = services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    /// <summary>Accepts a full id, a unique prefix of one (as shown by 'requests list'), or the email of a pending request.</summary>
    private static async Task<Result<Guid>> ResolveRequestIdAsync(IServiceProvider services, string input, CancellationToken ct)
    {
        if (Guid.TryParse(input, out var id))
        {
            return id;
        }

        if (input.Contains('@', StringComparison.Ordinal))
        {
            var pending = await Run<ListAccessRequests, IReadOnlyList<AccessRequest>>(services, u => u.ExecuteAsync(AccessRequestStatus.Pending, ct));
            var byEmail = pending.FirstOrDefault(r => string.Equals(r.Email.Value, input.Trim(), StringComparison.OrdinalIgnoreCase));
            return byEmail is not null
                ? byEmail.Id
                : new Error("access_request.not_found", $"No pending access request for '{input}'.");
        }

        var requests = await Run<ListAccessRequests, IReadOnlyList<AccessRequest>>(services, u => u.ExecuteAsync(null, ct));
        var matches = requests.Where(r => r.Id.ToString("N").StartsWith(input, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0].Id,
            0 => new Error("access_request.not_found", $"No access request starts with '{input}'."),
            _ => new Error("access_request.ambiguous", $"'{input}' matches {matches.Count} requests; type more characters."),
        };
    }

    private static int PrintCredentials(TextWriter output, string action, IssuedCredentials credentials)
    {
        output.WriteLine($"{action} {credentials.Email}.");
        output.WriteLine($"Temporary password (shown once): {credentials.TemporaryPassword}");
        output.WriteLine("Share it with the user privately; they must choose a new password when they first sign in.");
        return Success;
    }

    internal static int Fail(TextWriter error, Error failure)
    {
        error.WriteLine($"Error: {failure.Message} ({failure.Code})");
        return Failure;
    }

    private static string ShortId(Guid id) => id.ToString("N")[..8];

    private static string Utc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}

/// <summary>Minimal fixed-width table for console output.</summary>
internal static class Table
{
    public static void Write(TextWriter output, string[] headers, IEnumerable<string[]> rows)
    {
        var data = rows.ToList();
        if (data.Count == 0)
        {
            output.WriteLine("(none)");
            return;
        }

        var widths = headers.Select((h, i) => Math.Max(h.Length, data.Max(r => r[i].Length))).ToArray();
        output.WriteLine(Format(headers, widths));
        data.ForEach(row => output.WriteLine(Format(row, widths)));
    }

    private static string Format(string[] cells, int[] widths) =>
        string.Join("  ", cells.Select((c, i) => c.PadRight(widths[i]))).TrimEnd();
}
