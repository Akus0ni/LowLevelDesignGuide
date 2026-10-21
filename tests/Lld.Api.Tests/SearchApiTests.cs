using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lld.Api.Content;
using Lld.Contracts;
using Microsoft.AspNetCore.Hosting;

namespace Lld.Api.Tests;

/// <summary>Browser dev mode's search endpoint over a small repository in a temp folder (spec 009 §3.8, T7).</summary>
public sealed class SearchApiTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly TempContentRepository _repo = new();
    private readonly SearchApiFactory _factory;
    private readonly HttpClient _client;

    public SearchApiTests()
    {
        _repo.Write("start/how-to-use/lesson.md",
            "---\nid: how-to-use\ntitle: How to use\norder: 1\nsummary: Use.\ntags: []\n---\n# How to use\n\n## Revise\n\nCheat sheets help before an interview.\n");
        _factory = new SearchApiFactory(_repo.ContentFolder);
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
    public async Task FindsContentAnonymously()
    {
        var results = await _client.GetFromJsonAsync<SearchResultsDto>("/api/search?q=interv", Ct);

        results!.Hits.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Kind = SearchKinds.Lesson, LessonId = "how-to-use", Anchor = "revise", Heading = "Revise",
        });
    }

    [Fact]
    public async Task FindsTheSignedInLearnersNotesOnly()
    {
        var ada = await SignInAsync("search-ada@example.com");
        var grace = await SignInAsync("search-grace@example.com");
        var note = new NoteDto(Guid.NewGuid().ToString(), "how-to-use", "revise", "My #gotcha with recall", T0, T0, null, "browser-a");
        (await SendAsync(ada, HttpMethod.Put, "/api/notes", JsonSerializer.Serialize(new[] { note }, JsonSerializerOptions.Web)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var adaHits = await SearchAsync(ada, "gotcha");
        var graceHits = await SearchAsync(grace, "gotcha");
        var guestHits = await _client.GetFromJsonAsync<SearchResultsDto>("/api/search?q=gotcha", Ct);

        adaHits.Hits.Should().ContainSingle().Which.NoteId.Should().Be(note.Id);
        graceHits.Hits.Should().BeEmpty();
        guestHits!.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task AnUnknownKind_IsABadRequest()
    {
        var response = await _client.GetAsync("/api/search?q=revise&kind=video", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        json.RootElement.GetProperty("code").GetString().Should().Be(SearchEndpoints.BadKindCode);
    }

    [Fact]
    public async Task OutsideDevelopment_SearchIsNotMapped()
    {
        await using var hosted = new SearchApiFactory(_repo.ContentFolder, environment: "Staging");
        using var client = hosted.CreateClient();

        (await client.GetAsync("/api/search?q=revise", Ct)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(HttpClient Client, string Token)> SignInAsync(string email)
    {
        var client = _factory.CreateAppClient();
        var session = await _factory.CreateActiveUserAsync(client, email);
        return (client, session.AccessToken);
    }

    private static async Task<SearchResultsDto> SearchAsync((HttpClient Client, string Token) user, string query)
    {
        var response = await SendAsync(user, HttpMethod.Get, $"/api/search?q={Uri.EscapeDataString(query)}", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SearchResultsDto>(Ct))!;
    }

    private static Task<HttpResponseMessage> SendAsync((HttpClient Client, string Token) user, HttpMethod method, string path, string? json)
    {
        var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return user.Client.SendAsync(request, Ct);
    }

    private sealed class SearchApiFactory(string contentFolder, string environment = "Development") : LldApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(environment);
            builder.UseSetting("Content:Path", contentFolder);
        }
    }
}
