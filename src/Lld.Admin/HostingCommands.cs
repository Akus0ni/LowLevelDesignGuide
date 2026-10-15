using System.CommandLine;
using System.Globalization;
using System.Security.Cryptography;
using Lld.Application.Hosting;
using Lld.Domain.Common;
using Lld.Infrastructure.Hosting;
using Lld.ServerStatus;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Admin;

/// <summary>Self-hosting commands (spec 002): secrets, server status, request budget and backups.</summary>
internal static class HostingCommands
{
    // Configuration keys stored as protected secrets.
    public const string JwtSigningKey = "Jwt:SigningKey";
    public const string StatusPublicKey = "ServerStatus:PublicKey";
    public const string StatusPublicUrl = "ServerStatus:PublicUrl";

    private const int JwtKeyBytes = 48;
    private const int TopicBytes = 16; // 128 bits: not guessable
    private const string TopicPrefix = "lld-";

    public static IEnumerable<Command> All(IServiceProvider services, TextWriter output, TextWriter error) =>
    [
        HostingCommand(services, output, error),
        StatusCommand(services, output, error),
        BackupCommand(services, output),
    ];

    private static Command HostingCommand(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var secretsPath = new Option<string?>("--secrets-path") { Description = "Override Hosting:SecretsPath." };
        var publicUrl = new Option<string?>("--public-url") { Description = "The tunnel address, e.g. https://name.ngrok-free.dev." };
        var init = new Command("init", "Create any missing hosting secret (JWT key, status key pair, ntfy topic). Existing ones are kept.")
        {
            secretsPath, publicUrl,
        };
        init.SetAction(parse =>
        {
            var store = OpenStore(services, parse.GetValue(secretsPath));
            if (store.IsFailure)
            {
                return AdminCli.Fail(error, store.Error!);
            }

            var secrets = store.Value;
            Report(output, JwtSigningKey, CreateIfMissing(secrets, JwtSigningKey, () => RandomToken(JwtKeyBytes)));
            Report(output, ServerStatusOptions.TopicKey, CreateIfMissing(secrets, ServerStatusOptions.TopicKey, NewTopic));
            if (!secrets.Exists(ServerStatusOptions.PrivateKeyKey))
            {
                WriteKeyPair(secrets);
                Report(output, "ServerStatus key pair", created: true);
            }
            else
            {
                Report(output, "ServerStatus key pair", created: false);
            }

            if (parse.GetValue(publicUrl) is { Length: > 0 } url)
            {
                secrets.Write(StatusPublicUrl, url.TrimEnd('/'));
                output.WriteLine($"  set      {StatusPublicUrl} = {url.TrimEnd('/')}");
            }

            PrintAppSettings(output, secrets);
            return AdminCli.Success;
        });

        return new Command("hosting", "Set up this PC to host the API.") { init };
    }

    private static Command StatusCommand(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var secretsPath = new Option<string?>("--secrets-path") { Description = "Override Hosting:SecretsPath." };
        var force = new Option<bool>("--force") { Description = "Required: apps keep the old public key until they are rebuilt." };
        var keygen = new Command("keygen", "Replace the status signing key pair.") { secretsPath, force };
        keygen.SetAction(parse =>
        {
            var store = OpenStore(services, parse.GetValue(secretsPath));
            if (store.IsFailure)
            {
                return AdminCli.Fail(error, store.Error!);
            }

            if (store.Value.Exists(ServerStatusOptions.PrivateKeyKey) && !parse.GetValue(force))
            {
                return AdminCli.Fail(error, new Error("status.key_exists",
                    "A status key already exists. Re-run with --force, then rebuild the apps with the new public key."));
            }

            WriteKeyPair(store.Value);
            output.WriteLine("New status key pair created. Restart the API service and rebuild the apps.");
            PrintAppSettings(output, store.Value);
            return AdminCli.Success;
        });

        var state = new Argument<string>("state") { Description = "online or offline." };
        state.AcceptOnlyFromAmong("online", "offline");
        var publish = new Command("publish", "Publish a signed status message now (testing, planned maintenance).") { state };
        publish.SetAction(async (parse, ct) =>
        {
            var announcer = services.GetRequiredService<IStatusAnnouncer>();
            if (!announcer.IsConfigured)
            {
                return AdminCli.Fail(error, new Error("status.not_configured", "No ServerStatus:Topic / PrivateKey. Run 'hosting init' first."));
            }

            var serverState = parse.GetValue(state) == "online" ? ServerState.Online : ServerState.Offline;
            var time = services.GetRequiredService<TimeProvider>();
            await announcer.AnnounceAsync(StatusMessage.Create(serverState, StatusReasons.Manual, time.GetUtcNow()), ct);
            output.WriteLine($"Published: {parse.GetValue(state)}.");
            return AdminCli.Success;
        });

        var budget = new Command("budget", "Show this month's tunnel usage against the free limits.");
        budget.SetAction(async (_, ct) =>
        {
            var usage = await AdminCli.Run<GetBudgetUsage, BudgetUsage>(services, u => u.ExecuteAsync(ct));
            PrintBudget(output, usage);
            return AdminCli.Success;
        });

        return new Command("status", "Server status announcements and the request budget.") { keygen, publish, budget };
    }

    private static Command BackupCommand(IServiceProvider services, TextWriter output)
    {
        var run = new Command("run", "Back up the database now and prune old backups.");
        run.SetAction(async (_, ct) =>
        {
            var result = await services.GetRequiredService<SqliteBackupService>().RunAsync(ct);
            output.WriteLine($"Backup written: {result.Created.Path} ({Size(result.Created.SizeBytes)})");
            result.Pruned.ToList().ForEach(p => output.WriteLine($"Pruned: {p.Path}"));
            return AdminCli.Success;
        });

        var list = new Command("list", "List kept backups, newest first.");
        list.SetAction(_ =>
        {
            var backups = services.GetRequiredService<SqliteBackupService>().List();
            Table.Write(output, ["TAKEN (LOCAL)", "SIZE", "FILE"], backups.Select(b => new[]
            {
                b.TakenAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), Size(b.SizeBytes), b.Path,
            }));
            return AdminCli.Success;
        });

        return new Command("backup", "Database backups.") { run, list };
    }

    private static Result<ProtectedSecretStore> OpenStore(IServiceProvider services, string? overridePath)
    {
        var path = overridePath ?? services.GetRequiredService<HostingOptions>().SecretsPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return new Error("hosting.no_secrets_path",
                "Hosting:SecretsPath is not set. Use the installed lld-admin (C:\\LldGuide\\admin) or pass --secrets-path.");
        }

        return OperatingSystem.IsWindows()
            ? new ProtectedSecretStore(path)
            : new Error("hosting.windows_only", "Hosting secrets use Windows DPAPI; run this on the Windows PC that hosts the API.");
    }

    private static bool CreateIfMissing(ProtectedSecretStore secrets, string key, Func<string> create)
    {
        if (secrets.Exists(key))
        {
            return false;
        }

        secrets.Write(key, create());
        return true;
    }

    private static void WriteKeyPair(ProtectedSecretStore secrets)
    {
        var keys = StatusKeyPair.Create();
        secrets.Write(ServerStatusOptions.PrivateKeyKey, keys.PrivateKeyPkcs8);
        secrets.Write(StatusPublicKey, keys.PublicKeySpki);
    }

    private static void PrintAppSettings(TextWriter output, ProtectedSecretStore secrets)
    {
        var values = secrets.ReadAll();
        output.WriteLine();
        output.WriteLine("App build settings (see docs/hosting.md):");
        output.WriteLine($"  ServerStatus:PublicKey = {values.GetValueOrDefault(StatusPublicKey)}   (commit in src/Lld.App/appsettings.json)");
        output.WriteLine($"  ServerStatus:Topic     = {values.GetValueOrDefault(ServerStatusOptions.TopicKey)}   (local only: appsettings.local.json)");
        if (values.GetValueOrDefault(StatusPublicUrl) is { } url)
        {
            output.WriteLine($"  Api:BaseUrl            = {url}   (local only: appsettings.local.json)");
        }
    }

    private static void PrintBudget(TextWriter output, BudgetUsage usage)
    {
        output.WriteLine($"Window: {usage.Window.Key}, resets {usage.Window.ResetsAt:yyyy-MM-dd} (UTC)");
        Table.Write(output, ["", "USED", "LIMIT", "%"],
        [
            ["Requests", usage.Requests.ToString("N0", CultureInfo.InvariantCulture), usage.Limits.MonthlyRequests.ToString("N0", CultureInfo.InvariantCulture), Percent(usage.RequestsPercent)],
            ["Data", Size(usage.Bytes), Size(usage.Limits.MonthlyBytes), Percent(usage.BytesPercent)],
        ]);
        output.WriteLine(usage.IsNearLimit
            ? $"Near the limit (>= {usage.Limits.ThresholdPercent}%): apps are told the server is offline until the reset."
            : $"Apps are stopped at {usage.Limits.ThresholdPercent}%. Figures are an estimate; compare with the ngrok dashboard.");
    }

    private static void Report(TextWriter output, string what, bool created) =>
        output.WriteLine($"  {(created ? "created" : "kept   ")}  {what}");

    private static string NewTopic() => TopicPrefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(TopicBytes));

    private static string RandomToken(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    private static string Percent(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private static string Size(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):0.00} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):0.0} MB",
        >= 1L << 10 => $"{bytes / 1024.0:0.0} KB",
        _ => $"{bytes} B",
    };
}
