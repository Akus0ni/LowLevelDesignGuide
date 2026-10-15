using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lld.Contracts;
using Microsoft.AspNetCore.Hosting;

namespace Lld.Api.Tests;

/// <summary>The dev-only content API over a small repository in a temp folder (spec 003 §3.4).</summary>
public sealed class ContentApiTests : IDisposable
{
    private readonly TempContentRepository _repo = new();
    private readonly ContentApiFactory _factory;
    private readonly HttpClient _client;

    public ContentApiTests()
    {
        _factory = new ContentApiFactory(_repo.ContentFolder);
        _client = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _repo.Dispose();
    }

    [Fact]
    public async Task Modules_LessonsAndCheatSheets_AreServedAnonymously_InCamelCase()
    {
        var modules = await _client.GetFromJsonAsync<List<ModuleSummary>>("/api/content/modules", Ct);
        var module = await _client.GetFromJsonAsync<ModuleDetail>("/api/content/modules/start", Ct);
        var lesson = await _client.GetStringAsync("/api/content/lessons/how-to-use", Ct);
        var sheets = await _client.GetFromJsonAsync<List<CheatSheetSummary>>("/api/content/cheatsheets?module=start", Ct);
        var sheet = await _client.GetFromJsonAsync<CheatSheetDetail>("/api/content/cheatsheets/how-to-use", Ct);

        modules.Should().Equal(new ModuleSummary("start", "Start here", "How the guide works.", 0, 1));
        module!.Lessons.Select(l => l.Id).Should().Equal("how-to-use");
        using var json = JsonDocument.Parse(lesson);
        json.RootElement.GetProperty("moduleTitle").GetString().Should().Be("Start here");
        json.RootElement.GetProperty("bodyMarkdown").GetString().Should().StartWith("# How to use");
        sheets!.Select(s => s.LessonId).Should().Equal("how-to-use");
        sheet!.Title.Should().Be("Cheat sheet: How to use");
    }

    [Theory]
    [InlineData("/api/content/modules/nope")]
    [InlineData("/api/content/lessons/nope")]
    [InlineData("/api/content/cheatsheets/nope")]
    public async Task UnknownItems_AreNotFound_WithTheContentCode(string url)
    {
        var response = await _client.GetAsync(url, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CodeOf(response)).Should().Be(ContentErrorCodes.NotFound);
    }

    [Fact]
    public async Task AddedAndDeletedLessons_ShowUpOnTheNextRequest_WithoutARestart()
    {
        (await LessonIdsAsync()).Should().Equal("how-to-use");

        _repo.AddLesson("glossary", order: 2);
        (await LessonIdsAsync()).Should().Equal("how-to-use", "glossary");

        _repo.DeleteLesson("glossary");
        (await LessonIdsAsync()).Should().Equal("how-to-use");
    }

    [Fact]
    public async Task InvalidContent_IsAProblemListingEveryIssue()
    {
        _repo.Write("start/how-to-use/cheatsheet.md", "# no front-matter\n");

        var response = await _client.GetAsync("/api/content/modules", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        problem.RootElement.GetProperty("code").GetString().Should().Be(ContentErrorCodes.Invalid);
        problem.RootElement.GetProperty("issues").EnumerateArray().Select(i => i.GetString()).Should().Equal(
            "content/start/how-to-use/cheatsheet.md:1: frontmatter.missing: Start the file with front-matter between '---' lines.");
    }

    [Fact]
    public async Task OutsideDevelopment_TheContentApiDoesNotExist()
    {
        await using var hosted = new ContentApiFactory(_repo.ContentFolder, environment: "Staging");
        using var client = hosted.CreateClient();

        var response = await client.GetAsync("/api/content/modules", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<IEnumerable<string>> LessonIdsAsync() =>
        (await _client.GetFromJsonAsync<ModuleDetail>("/api/content/modules/start", Ct))!.Lessons.Select(l => l.Id);

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return problem.RootElement.GetProperty("code").GetString();
    }

    private sealed class ContentApiFactory(string contentFolder, string environment = "Development") : LldApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(environment);
            builder.UseSetting("Content:Path", contentFolder);
        }
    }
}

/// <summary>A one-module repository on disk that tests can change while the API runs.</summary>
internal sealed class TempContentRepository : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("lld-api-content-").FullName;

    public TempContentRepository()
    {
        Write("start/module.yml", "title: Start here\norder: 0\nsummary: How the guide works.\n");
        AddLesson("how-to-use", order: 1);
    }

    public string ContentFolder => Path.Combine(_root, "content");

    public void AddLesson(string slug, int order)
    {
        Write($"start/{slug}/lesson.md",
            $"---\nid: {slug}\ntitle: \"{slug}\"\norder: {order}\nsummary: \"About {slug}.\"\ntags: []\n---\n# How to use {slug}\n");
        Write($"start/{slug}/cheatsheet.md", "---\ntitle: \"Cheat sheet: How to use\"\n---\n# Cheat sheet\n");
    }

    public void DeleteLesson(string slug) => Directory.Delete(Path.Combine(ContentFolder, "start", slug), recursive: true);

    public void Write(string relativePath, string text)
    {
        var path = Path.Combine(ContentFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
