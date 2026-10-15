using FluentAssertions;
using Lld.Application;
using Lld.Application.Hosting;
using Lld.Infrastructure;
using Lld.Infrastructure.Hosting;
using Lld.ServerStatus;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Admin.Tests;

/// <summary>Self-hosting commands against a throw-away database, backup folder and secrets folder.</summary>
public sealed class HostingCommandsTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lld-admin-tests", Guid.NewGuid().ToString("N"));
    private readonly RecordingAnnouncer _announcer = new();
    private ServiceProvider _services = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string DatabasePath => Path.Combine(_root, "lld.db");

    private string BackupPath => Path.Combine(_root, "backups");

    private string SecretsPath => Path.Combine(_root, "secrets");

    public async ValueTask InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Path"] = DatabasePath,
                ["Backup:Path"] = BackupPath,
                ["Backup:KeepDays"] = "14",
            })
            .Build();
        _services = new ServiceCollection()
            .AddLogging()
            .AddLldApplication()
            .AddLldInfrastructure(configuration)
            .AddLldHosting(configuration)
            .AddSingleton<IStatusAnnouncer>(_announcer)
            .BuildServiceProvider();
        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task HostingInit_CreatesMissingSecretsOnce_AndPrintsTheAppSettings()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Protected secrets use Windows DPAPI.");

        var (code, first, _) = await RunAsync("hosting", "init", "--secrets-path", SecretsPath, "--public-url", "https://demo.ngrok-free.dev/");
        var (_, second, _) = await RunAsync("hosting", "init", "--secrets-path", SecretsPath);

        code.Should().Be(0);
        first.Should().Contain("created  Jwt:SigningKey").And.Contain("created  ServerStatus key pair")
            .And.Contain("Api:BaseUrl            = https://demo.ngrok-free.dev");
        second.Should().Contain("kept     Jwt:SigningKey").And.Contain("kept     ServerStatus key pair");
        first.Should().MatchRegex(@"ServerStatus:Topic\s+= lld-[0-9a-f]{32}");

        var secrets = new ProtectedSecretStore(SecretsPath).ReadAll();
        secrets[ServerStatusOptions.TopicKey].Should().StartWith("lld-");
        File.ReadAllText(Directory.GetFiles(SecretsPath, "Jwt__SigningKey.secret").Single())
            .Should().NotContain(secrets["Jwt:SigningKey"]!, "the value is encrypted at rest");
    }

    [Fact]
    public async Task StatusKeygen_RefusesToReplaceAKeyWithoutForce()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Protected secrets use Windows DPAPI.");
        await RunAsync("hosting", "init", "--secrets-path", SecretsPath);
        var before = new ProtectedSecretStore(SecretsPath).ReadAll()["ServerStatus:PublicKey"];

        var (refused, _, error) = await RunAsync("status", "keygen", "--secrets-path", SecretsPath);
        var (forced, _, _) = await RunAsync("status", "keygen", "--secrets-path", SecretsPath, "--force");

        refused.Should().Be(1);
        error.Should().Contain("--force");
        forced.Should().Be(0);
        new ProtectedSecretStore(SecretsPath).ReadAll()["ServerStatus:PublicKey"].Should().NotBe(before);
    }

    [Fact]
    public async Task HostingInit_WithoutASecretsPath_ExplainsWhatToDo()
    {
        var (code, _, error) = await RunAsync("hosting", "init");

        code.Should().Be(1);
        error.Should().Contain("Hosting:SecretsPath is not set");
    }

    [Fact]
    public async Task StatusPublish_SendsASignedManualMessage()
    {
        var (code, output, _) = await RunAsync("status", "publish", "offline");

        code.Should().Be(0);
        output.Should().Contain("Published: offline.");
        _announcer.Messages.Should().ContainSingle()
            .Which.Should().Match<StatusMessage>(m => m.Status == ServerState.Offline && m.Reason == StatusReasons.Manual);
    }

    [Fact]
    public async Task StatusBudget_ShowsUsageAgainstTheLimits()
    {
        var window = BudgetWindow.Containing(DateTimeOffset.UtcNow, 1);
        await using (var scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IUsageCounterStore>().AddAsync(window.Key, 19_000, 200L << 20, Ct);
        }

        var (code, output, _) = await RunAsync("status", "budget");

        code.Should().Be(0);
        output.Should().Contain($"Window: {window.Key}").And.Contain("19,000").And.Contain("20,000")
            .And.Contain("95.0").And.Contain("200.0 MB").And.Contain("Near the limit");
    }

    [Fact]
    public async Task BackupRun_WritesACopy_PrunesOldOnes_AndListsThem()
    {
        Directory.CreateDirectory(BackupPath);
        var old = Path.Combine(BackupPath, "lld-20200101-0000.db");
        await File.WriteAllTextAsync(old, "old", Ct);

        var (code, output, _) = await RunAsync("backup", "run");
        var (_, list, _) = await RunAsync("backup", "list");

        code.Should().Be(0);
        output.Should().Contain("Backup written:").And.Contain("Pruned:");
        File.Exists(old).Should().BeFalse();
        var backup = Directory.GetFiles(BackupPath, "lld-*.db").Should().ContainSingle().Subject;
        list.Should().Contain(Path.GetFileName(backup));

        await using var copy = new SqliteConnection($"Data Source={backup};Mode=ReadOnly;Pooling=False");
        await copy.OpenAsync(Ct);
        await using var command = copy.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";";
        ((long)(await command.ExecuteScalarAsync(Ct))!).Should().BeGreaterThan(0, "the backup is a complete, readable database");
    }

    private async Task<(int Code, string Output, string Error)> RunAsync(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await AdminCli.RunAsync(args, _services, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private sealed class RecordingAnnouncer : IStatusAnnouncer
    {
        public List<StatusMessage> Messages { get; } = [];

        public bool IsConfigured => true;

        public Task AnnounceAsync(StatusMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
