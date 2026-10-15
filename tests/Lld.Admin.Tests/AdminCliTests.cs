using FluentAssertions;
using Lld.Application;
using Lld.Application.Access;
using Lld.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Admin.Tests;

/// <summary>Runs the real console commands against a throw-away SQLite file.</summary>
public sealed class AdminCliTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), "lld-admin-tests", $"{Guid.NewGuid():N}.db");
    private ServiceProvider _services = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Path"] = _databasePath })
            .Build();
        _services = new ServiceCollection().AddLogging().AddLldApplication().AddLldInfrastructure(configuration).BuildServiceProvider();
        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task UsersCreate_Admin_PrintsTemporaryPasswordOnce()
    {
        var (code, output, _) = await RunAsync("users", "create", "owner@example.com", "--role", "admin");

        code.Should().Be(0);
        output.Should().Contain("Created admin owner@example.com").And.Contain("Temporary password (shown once): ");

        var (_, list, _) = await RunAsync("users", "list");
        list.Should().Contain("owner@example.com").And.Contain("Admin").And.Contain("must change password");
    }

    [Fact]
    public async Task RequestsApprove_ByShortId_CreatesTheAccount()
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SubmitAccessRequest>()
                .ExecuteAsync(new SubmitAccessRequestCommand("ada@example.com", "Ada", "please"), Ct);
        }

        var (_, pending, _) = await RunAsync("requests", "list");
        pending.Should().Contain("ada@example.com").And.Contain("Pending");
        var shortId = pending.Split('\n')[1][..8];

        var (code, approved, _) = await RunAsync("requests", "approve", shortId);

        code.Should().Be(0);
        approved.Should().Contain("Approved access for ada@example.com").And.Contain("Temporary password");
        (await RunAsync("requests", "list")).Output.Should().Contain("(none)");
        (await RunAsync("requests", "list", "--all")).Output.Should().Contain("Approved");
    }

    [Fact]
    public async Task RequestsApproveAndReject_ByEmail_FindThePendingRequest()
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            var submit = scope.ServiceProvider.GetRequiredService<SubmitAccessRequest>();
            await submit.ExecuteAsync(new SubmitAccessRequestCommand("grace@example.com", "Grace", null), Ct);
            await submit.ExecuteAsync(new SubmitAccessRequestCommand("linus@example.com", "Linus", null), Ct);
        }

        var (approvedCode, approved, _) = await RunAsync("requests", "approve", "Grace@Example.com");
        var (rejectedCode, rejected, _) = await RunAsync("requests", "reject", "linus@example.com");
        var (missingCode, _, missing) = await RunAsync("requests", "approve", "nobody@example.com");

        approvedCode.Should().Be(0);
        approved.Should().Contain("Approved access for grace@example.com");
        rejectedCode.Should().Be(0);
        rejected.Should().Contain("Request rejected.");
        missingCode.Should().Be(1);
        missing.Should().Contain("No pending access request for 'nobody@example.com'");
    }

    [Fact]
    public async Task UnknownRequestOrUser_FailsWithExitCodeOne()
    {
        var (rejectCode, _, rejectError) = await RunAsync("requests", "reject", "deadbeef");
        var (resetCode, _, resetError) = await RunAsync("users", "reset-password", "ghost@example.com");

        rejectCode.Should().Be(1);
        rejectError.Should().Contain("access_request.not_found");
        resetCode.Should().Be(1);
        resetError.Should().Contain("user.not_found");
    }

    [Fact]
    public async Task AuditList_ShowsSecurityEvents()
    {
        await RunAsync("users", "create", "learner@example.com");
        await RunAsync("users", "disable", "learner@example.com");

        var (code, output, _) = await RunAsync("audit", "list", "--take", "10");

        code.Should().Be(0);
        output.Should().Contain("UserCreated").And.Contain("UserDisabled");
    }

    private async Task<(int Code, string Output, string Error)> RunAsync(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await AdminCli.RunAsync(args, _services, output, error);
        return (code, output.ToString(), error.ToString());
    }
}
