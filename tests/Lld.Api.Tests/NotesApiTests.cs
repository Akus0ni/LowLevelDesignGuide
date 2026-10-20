using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lld.Contracts;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Api.Tests;

public sealed class NotesApiTests(LldApiFactory factory) : IClassFixture<LldApiFactory>
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/notes")]
    [InlineData("PUT", "/api/notes")]
    [InlineData("PUT", "/api/bookmarks")]
    [InlineData("POST", "/api/notes/adopt")]
    public async Task EveryEndpoint_RequiresSignIn(string method, string path)
    {
        using var client = factory.CreateAppClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SavedNotesAndBookmarks_AreReadBack_DeletesIncluded()
    {
        var ada = await SignInAsync("notes-roundtrip@example.com");
        var section = Note("On the rule #weak", anchor: "the-rule");
        var deleted = Note("gone", minutes: 1) with { DeletedAt = T0.AddMinutes(1) };
        var bookmark = Bookmark(BookmarkKinds.Section, anchor: "the-rule");

        (await SendAsync(ada, HttpMethod.Put, "/api/notes", new[] { section, deleted })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(ada, HttpMethod.Put, "/api/bookmarks", new[] { bookmark })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await GetAsync(ada);
        stored.Notes.Should().BeEquivalentTo([section, deleted]);
        stored.Bookmarks.Should().BeEquivalentTo([bookmark]);
    }

    [Fact]
    public async Task Users_OnlyEverSeeTheirOwnNotes_EvenWithTheSameId()
    {
        var ada = await SignInAsync("notes-ada@example.com");
        var grace = await SignInAsync("notes-grace@example.com");
        var note = Note("Ada's");

        await SendAsync(ada, HttpMethod.Put, "/api/notes", new[] { note });
        await SendAsync(grace, HttpMethod.Put, "/api/notes", new[] { note with { Body = "Grace's", UpdatedAt = T0.AddMinutes(9) } });

        (await GetAsync(ada)).Notes.Single().Body.Should().Be("Ada's");
        (await GetAsync(grace)).Notes.Single().Body.Should().Be("Grace's");
    }

    [Fact]
    public async Task AnOlderEdit_DoesNotOverwriteANewerOne()
    {
        var ada = await SignInAsync("notes-lww@example.com");
        var note = Note("newer", minutes: 5);
        await SendAsync(ada, HttpMethod.Put, "/api/notes", new[] { note });

        await SendAsync(ada, HttpMethod.Put, "/api/notes", new[] { note with { Body = "older", UpdatedAt = T0.AddMinutes(1) } });

        (await GetAsync(ada)).Notes.Single().Body.Should().Be("newer");
    }

    [Theory]
    [InlineData("/api/notes", """[{"id":"x","lessonId":"srp","anchor":"","body":"b","createdAt":"2026-10-20T09:00:00Z","updatedAt":"2026-10-20T09:00:00Z","deviceId":"d"}]""", "validation.notes.id")]
    [InlineData("/api/notes", """[{"id":"6f1c3d0e-0000-4000-8000-000000000001","lessonId":"srp","anchor":"","body":" ","createdAt":"2026-10-20T09:00:00Z","updatedAt":"2026-10-20T09:00:00Z","deviceId":"d"}]""", "validation.notes.body")]
    [InlineData("/api/bookmarks", """[{"kind":"chapter","lessonId":"srp","anchor":"","createdAt":"2026-10-20T09:00:00Z","updatedAt":"2026-10-20T09:00:00Z","deviceId":"d"}]""", "validation.bookmarks.kind")]
    [InlineData("/api/bookmarks", """[{"kind":"section","lessonId":"srp","anchor":"","createdAt":"2026-10-20T09:00:00Z","updatedAt":"2026-10-20T09:00:00Z","deviceId":"d"}]""", "validation.bookmarks.anchor")]
    public async Task InvalidRecords_AreRejectedWithACode(string path, string body, string expectedCode)
    {
        var ada = await SignInAsync($"notes-invalid-{expectedCode.Length}-{body.Length}@example.com");

        var response = await SendRawAsync(ada, HttpMethod.Put, path, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be(expectedCode);
    }

    [Fact]
    public async Task OverALessonsLimit_NothingIsSaved()
    {
        var ada = await SignInAsync("notes-limit@example.com");
        var tooMany = Enumerable.Range(0, 101).Select(i => Note($"note {i}")).ToArray();

        var response = await SendAsync(ada, HttpMethod.Put, "/api/notes", tooMany);

        (await ErrorCodeAsync(response)).Should().Be("validation.notes.too_many");
        (await GetAsync(ada)).Notes.Should().BeEmpty();
    }

    /// <summary>Kestrel enforces the limit (the test server doesn't), so this checks every notes endpoint declares it.</summary>
    [Fact]
    public void EveryEndpoint_LimitsTheBodyToOneMegabyte()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is "/api/notes" or "/api/bookmarks" or "/api/notes/adopt")
            .ToList();

        endpoints.Should().HaveCount(4);
        endpoints.Should().OnlyContain(e => e.Metadata.GetMetadata<IRequestSizeLimitMetadata>()!.MaxRequestBodySize == 1_048_576);
    }

    [Fact]
    public async Task Adopt_MergesTheBrowserGuestIntoTheUser()
    {
        var ada = await SignInAsync("notes-adopt@example.com");
        var shared = Note("user's", minutes: 1);
        await SendAsync(ada, HttpMethod.Put, "/api/notes", new[] { shared });
        await SendAsync(ada, HttpMethod.Put, "/api/bookmarks", new[] { Bookmark(BookmarkKinds.Lesson) });
        var guest = new NotesSnapshotDto(
            [shared with { Body = "guest's later edit", UpdatedAt = T0.AddMinutes(4) }, Note("only guest's")],
            [Bookmark(BookmarkKinds.CheatSheet)]);

        var response = await SendAsync(ada, HttpMethod.Post, "/api/notes/adopt", guest);

        var merged = (await response.Content.ReadFromJsonAsync<NotesSnapshotDto>(Ct))!;
        merged.Should().BeEquivalentTo(await GetAsync(ada));
        merged.Notes.Select(n => n.Body).Should().BeEquivalentTo(["guest's later edit", "only guest's"]);
        merged.Bookmarks.Should().HaveCount(2);
    }

    private static NoteDto Note(string body, string anchor = "", int minutes = 0) =>
        new(Guid.NewGuid().ToString(), "srp", anchor, body, T0, T0.AddMinutes(minutes), null, "browser-a");

    private static BookmarkDto Bookmark(string kind, string anchor = "") => new(kind, "srp", anchor, T0, T0, null, "browser-a");

    private async Task<(HttpClient Client, string Token)> SignInAsync(string email)
    {
        var client = factory.CreateAppClient();
        var session = await factory.CreateActiveUserAsync(client, email);
        return (client, session.AccessToken);
    }

    private static async Task<NotesSnapshotDto> GetAsync((HttpClient Client, string Token) user)
    {
        var response = await SendAsync(user, HttpMethod.Get, "/api/notes");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotesSnapshotDto>(Ct))!;
    }

    private static Task<HttpResponseMessage> SendAsync((HttpClient Client, string Token) user, HttpMethod method, string path, object? body = null) =>
        SendRawAsync(user, method, path, body is null ? null : JsonSerializer.Serialize(body, JsonSerializerOptions.Web));

    private static Task<HttpResponseMessage> SendRawAsync((HttpClient Client, string Token) user, HttpMethod method, string path, string? json)
    {
        var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return user.Client.SendAsync(request, Ct);
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return problem.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
