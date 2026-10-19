using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lld.Contracts;

namespace Lld.Api.Tests;

public sealed class ProgressApiTests(LldApiFactory factory) : IClassFixture<LldApiFactory>
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/progress")]
    [InlineData("PUT", "/api/progress/lessons")]
    [InlineData("POST", "/api/progress/activity")]
    [InlineData("POST", "/api/progress/adopt")]
    public async Task EveryEndpoint_RequiresSignIn(string method, string path)
    {
        using var client = factory.CreateAppClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SavedLessons_AreReadBack()
    {
        var ada = await SignInAsync("progress-roundtrip@example.com");
        var done = Done("srp", minutes: 1) with { ReadSections = ["rule", "smells"], Position = new ReadingPositionDto("smells", 80) };

        (await SendAsync(ada, HttpMethod.Put, "/api/progress/lessons", new[] { done, Opened("ocp") })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await GetAsync(ada)).Lessons.Should().BeEquivalentTo([Opened("ocp"), done], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task Users_OnlyEverSeeTheirOwnProgress()
    {
        var ada = await SignInAsync("progress-ada@example.com");
        var grace = await SignInAsync("progress-grace@example.com");

        await SendAsync(ada, HttpMethod.Put, "/api/progress/lessons", new[] { Done("srp") });
        await SendAsync(grace, HttpMethod.Put, "/api/progress/lessons", new[] { Opened("srp", minutes: 9) });

        (await GetAsync(ada)).Lessons.Single().Status.Should().Be(ProgressStatuses.Done);
        (await GetAsync(grace)).Lessons.Single().Status.Should().Be(ProgressStatuses.InProgress);
    }

    [Fact]
    public async Task AnOlderEdit_DoesNotOverwriteANewerOne_ButAResetDoes()
    {
        var ada = await SignInAsync("progress-lww@example.com");
        await SendAsync(ada, HttpMethod.Put, "/api/progress/lessons", new[] { Done("srp", minutes: 5) });

        await SendAsync(ada, HttpMethod.Put, "/api/progress/lessons", new[] { Opened("srp", minutes: 1) });
        var afterOlder = (await GetAsync(ada)).Lessons.Single().Status;
        await SendAsync(ada, HttpMethod.Put, "/api/progress/lessons", new[] { Reset("srp", minutes: 9) });

        afterOlder.Should().Be(ProgressStatuses.Done);
        (await GetAsync(ada)).Lessons.Single().Status.Should().Be(ProgressStatuses.NotStarted);
    }

    [Theory]
    [InlineData("""[{"lessonId":"srp","status":"finished","readSections":[],"updatedAt":"2026-10-19T09:00:00Z","deviceId":"d"}]""", "validation.progress.status")]
    [InlineData("""[{"lessonId":"srp","status":"done","readSections":[],"updatedAt":"2026-10-19T09:00:00Z","deviceId":"d"}]""", "validation.progress.status")]
    [InlineData("""[{"lessonId":"","status":"inProgress","readSections":[],"updatedAt":"2026-10-19T09:00:00Z","deviceId":"d"}]""", "validation.progress.lesson_id")]
    public async Task InvalidLessons_AreRejectedWithACode(string body, string expectedCode)
    {
        var ada = await SignInAsync($"progress-invalid-{expectedCode.Length}-{body.Length}@example.com");

        var response = await SendRawAsync(ada, HttpMethod.Put, "/api/progress/lessons", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be(expectedCode);
    }

    [Fact]
    public async Task ActivityDays_AreSavedOnce()
    {
        var ada = await SignInAsync("progress-days@example.com");

        await SendAsync(ada, HttpMethod.Post, "/api/progress/activity", new ActivityDayRequest("2026-10-18"));
        await SendAsync(ada, HttpMethod.Post, "/api/progress/activity", new ActivityDayRequest("2026-10-18"));

        (await GetAsync(ada)).ActivityDays.Should().Equal("2026-10-18");
    }

    [Theory]
    [InlineData("18/10/2026")]
    [InlineData("2999-01-01")]
    public async Task ActivityDays_MustBeARealDayNotInTheFuture(string day)
    {
        var ada = await SignInAsync($"progress-badday-{day.Length}-{day[0]}@example.com");

        var response = await SendAsync(ada, HttpMethod.Post, "/api/progress/activity", new ActivityDayRequest(day));

        (await ErrorCodeAsync(response)).Should().Be("validation.progress.day");
    }

    [Fact]
    public async Task Adopt_MergesTheBrowserGuestIntoTheUser()
    {
        var ada = await SignInAsync("progress-adopt@example.com");
        await SendAsync(ada, HttpMethod.Put, "/api/progress/lessons", new[] { Opened("srp", minutes: 1) with { ReadSections = ["rule"] } });
        var guest = new ProgressSnapshotDto([Opened("srp", minutes: 3) with { ReadSections = ["smells"] }, Done("lsp")], ["2026-10-17"]);

        var response = await SendAsync(ada, HttpMethod.Post, "/api/progress/adopt", guest);

        var merged = (await response.Content.ReadFromJsonAsync<ProgressSnapshotDto>(Ct))!;
        merged.Should().BeEquivalentTo(await GetAsync(ada));
        merged.Lessons.Single(l => l.LessonId == "srp").ReadSections.Should().Equal("rule", "smells");
        merged.Lessons.Should().HaveCount(2);
    }

    private static LessonProgressDto Opened(string lessonId, int minutes = 0) =>
        new(lessonId, ProgressStatuses.InProgress, null, null, [], null, T0.AddMinutes(minutes), null, T0.AddMinutes(minutes), "browser-a");

    private static LessonProgressDto Done(string lessonId, int minutes = 0) =>
        Opened(lessonId, minutes) with { Status = ProgressStatuses.Done, DoneBy = ProgressDoneBy.Manual, CompletedAt = T0.AddMinutes(minutes) };

    private static LessonProgressDto Reset(string lessonId, int minutes) =>
        Opened(lessonId, minutes) with { Status = ProgressStatuses.NotStarted, LastReadAt = null };

    private async Task<(HttpClient Client, string Token)> SignInAsync(string email)
    {
        var client = factory.CreateAppClient();
        var session = await factory.CreateActiveUserAsync(client, email);
        return (client, session.AccessToken);
    }

    private static async Task<ProgressSnapshotDto> GetAsync((HttpClient Client, string Token) user)
    {
        var response = await SendAsync(user, HttpMethod.Get, "/api/progress");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProgressSnapshotDto>(Ct))!;
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
