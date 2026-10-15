using System.Text.Json;
using FluentAssertions;
using Lld.Content.Authoring.Validation;
using Lld.Content.Cli;
using Lld.Content.Tests.Authoring;

namespace Lld.Content.Tests.Cli;

/// <summary>Runs <c>lld-content</c> commands against a temporary repository on disk.</summary>
public sealed class ContentCliTests : IDisposable
{
    private readonly string _repo = Directory.CreateTempSubdirectory("lld-content-repo-").FullName;
    private readonly string _out = Directory.CreateTempSubdirectory("lld-content-out-").FullName;
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public void Dispose()
    {
        Directory.Delete(_repo, recursive: true);
        Directory.Delete(_out, recursive: true);
        _output.Dispose();
        _error.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string PackPath => Path.Combine(_out, ContentPackFiles.PackFileName);

    private string VersionPath => Path.Combine(_out, ContentPackFiles.VersionFileName);

    [Fact]
    public async Task Validate_ValidContent_Succeeds()
    {
        new ValidRepo().WriteTo(_repo);

        var exit = await RunAsync("validate");

        exit.Should().Be(ContentCli.Success);
        _output.ToString().Should().StartWith("Content is valid: 2 module(s), 3 lesson(s), version ");
        _error.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_BrokenContent_PrintsIssuesAndFails()
    {
        new ValidRepo().Remove(ValidRepo.ParkingSheet).WriteTo(_repo);

        var exit = await RunAsync("validate");

        exit.Should().Be(ContentCli.Failure);
        _error.ToString().Should().Contain($"{ValidRepo.ParkingSheet}: {IssueCodes.CheatSheetMissing}: ")
            .And.Contain("1 content issue(s); nothing was written.");
    }

    [Fact]
    public async Task MSBuildFormat_UsesFullPathsAndTheErrorKeyword()
    {
        new ValidRepo().Replace(ValidRepo.Parking, "order: 2", "order: x").WriteTo(_repo);

        await RunAsync("validate", "--format", "MSBuild");

        var full = Path.Combine(_repo, "content", "case-studies", "parking-lot", "lesson.md");
        _error.ToString().Should().Contain($"{full}(4): error {IssueCodes.FrontMatterWrongType}: 'order' must be a whole number");
    }

    [Fact]
    public async Task Pack_WritesAReadablePackAndItsVersion()
    {
        new ValidRepo().WriteTo(_repo);

        var exit = await RunAsync("pack", "--out", _out);

        exit.Should().Be(ContentCli.Success);
        await using var stream = File.OpenRead(PackPath);
        var pack = await ContentPackJson.ReadAsync(stream, Ct);
        pack.Modules.Should().HaveCount(2);
        (await File.ReadAllTextAsync(VersionPath, Ct)).Should().Be(pack.Version);
    }

    [Fact]
    public async Task Pack_SameContent_LeavesTheFilesAlone_ChangedContent_RewritesThem()
    {
        new ValidRepo().WriteTo(_repo);
        await RunAsync("pack", "--out", _out);
        var firstWrite = File.GetLastWriteTimeUtc(PackPath);
        var firstVersion = await File.ReadAllTextAsync(VersionPath, Ct);

        await RunAsync("pack", "--out", _out);

        File.GetLastWriteTimeUtc(PackPath).Should().Be(firstWrite);
        _output.ToString().Should().Contain("up to date");

        new ValidRepo().Replace(ValidRepo.AuthSheet, "Rotate", "Always rotate").WriteTo(_repo);
        await RunAsync("pack", "--out", _out);

        (await File.ReadAllTextAsync(VersionPath, Ct)).Should().NotBe(firstVersion);
    }

    [Fact]
    public async Task Pack_BrokenContent_WritesNothing()
    {
        new ValidRepo().Remove(ValidRepo.StartModule).WriteTo(_repo);

        var exit = await RunAsync("pack", "--out", _out);

        exit.Should().Be(ContentCli.Failure);
        File.Exists(PackPath).Should().BeFalse();
        File.Exists(VersionPath).Should().BeFalse();
    }

    [Fact]
    public async Task Snapshot_WritesEveryApiResponse_InCamelCase_AndDropsStaleFiles()
    {
        new ValidRepo().WriteTo(_repo);
        await File.WriteAllTextAsync(Path.Combine(_out, "stale.json"), "{}", Ct);

        var exit = await RunAsync("snapshot", "--out", _out);

        exit.Should().Be(ContentCli.Success);
        Directory.EnumerateFiles(_out, "*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_out, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .Should().Equal(
                "cheatsheets-by-module/case-studies.json",
                "cheatsheets-by-module/start.json",
                "cheatsheets.json",
                "cheatsheets/case-study-auth.json",
                "cheatsheets/case-study-parking.json",
                "cheatsheets/how-to-use.json",
                "lessons/case-study-auth.json",
                "lessons/case-study-parking.json",
                "lessons/how-to-use.json",
                "modules.json",
                "modules/case-studies.json",
                "modules/start.json");
        using var lesson = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(_out, "lessons", "case-study-auth.json"), Ct));
        lesson.RootElement.GetProperty("moduleSlug").GetString().Should().Be("case-studies");
        lesson.RootElement.GetProperty("next").GetProperty("id").GetString().Should().Be("case-study-parking");
        lesson.RootElement.GetProperty("bodyMarkdown").GetString().Should().Contain("/learn/start/how-to-use");
    }

    private Task<int> RunAsync(params string[] args) =>
        ContentCli.RunAsync([.. args, "--repo", _repo], _output, _error);
}
