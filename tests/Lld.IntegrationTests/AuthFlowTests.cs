using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Lld.Application;
using Lld.Application.Access;
using Lld.Contracts;
using Lld.Domain.Access;
using Lld.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.IntegrationTests;

/// <summary>
/// Spec 001 A1, end to end through the real AppHost: request access over HTTP, approve it the way the admin
/// console does (application layer on the same database), sign in with the temporary password, change it, and save
/// progress and notes as the signed-in learner (spec 005 A6, spec 007 A12).
/// </summary>
public sealed class AuthFlowTests : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), "lld-it", $"{Guid.NewGuid():N}.db");
    private DistributedApplication? _app;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Lld_AppHost>(
            ["--Lld:ApiOnly=true", $"--Lld:DatabasePath={_databasePath}"], Ct);

        _app = await builder.BuildAsync(Ct);
        await _app.StartAsync(Ct);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", Ct).WaitAsync(StartupTimeout, Ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task RequestApproveLoginChangePassword_ThenKeepProgressAndNotes()
    {
        using var client = _app!.CreateHttpClient("api", "http");
        const string email = "it-learner@example.com";
        const string newPassword = "integration-password-123";

        var requested = await client.PostAsJsonAsync("/api/access-requests", new AccessRequestRequest(email, "IT Learner", null), Ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var temporaryPassword = await ApproveAsAdminAsync(email);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, temporaryPassword), Ct);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var temporary = (await login.Content.ReadFromJsonAsync<AuthResponse>(Ct))!;
        temporary.User.MustChangePassword.Should().BeTrue();

        using var change = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(temporaryPassword, newPassword)),
        };
        change.Headers.Authorization = new AuthenticationHeaderValue("Bearer", temporary.AccessToken);
        var changed = await client.SendAsync(change, Ct);
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await changed.Content.ReadFromJsonAsync<AuthResponse>(Ct))!.User.MustChangePassword.Should().BeFalse();

        var relogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword), Ct);
        relogin.StatusCode.Should().Be(HttpStatusCode.OK);

        // Spec 005 A6: the signed-in learner keeps progress on the server.
        var session = (await relogin.Content.ReadFromJsonAsync<AuthResponse>(Ct))!;
        var opened = new LessonProgressDto("srp", ProgressStatuses.InProgress, null, null, ["rule"], null,
            DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow, "it-device");
        (await SendAsync(client, HttpMethod.Put, "/api/progress/lessons", session.AccessToken, new[] { opened })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        var progress = await SendAsync(client, HttpMethod.Get, "/api/progress", session.AccessToken);
        (await progress.Content.ReadFromJsonAsync<ProgressSnapshotDto>(Ct))!.Lessons.Should().ContainSingle(l => l.LessonId == "srp");

        // Spec 007 A12: and notes on the server, through the real AppHost and its migrated database.
        var note = new NoteDto(Guid.NewGuid().ToString(), "srp", "the-rule", "My example #work", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, "it-device");
        (await SendAsync(client, HttpMethod.Put, "/api/notes", session.AccessToken, new[] { note })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        var notes = await SendAsync(client, HttpMethod.Get, "/api/notes", session.AccessToken);
        (await notes.Content.ReadFromJsonAsync<NotesSnapshotDto>(Ct))!.Notes.Should().ContainSingle(n => n.Body == note.Body);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request, Ct);
    }

    /// <summary>What <c>Lld.Admin requests approve</c> does: use cases straight on the shared SQLite file.</summary>
    private async Task<string> ApproveAsAdminAsync(string email)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Path"] = _databasePath })
            .Build();
        var services = new ServiceCollection().AddLogging().AddLldApplication().AddLldInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var pending = await scope.ServiceProvider.GetRequiredService<ListAccessRequests>().ExecuteAsync(AccessRequestStatus.Pending, Ct);
        var request = pending.Single(r => r.Email.Value == email);
        var credentials = await scope.ServiceProvider.GetRequiredService<ApproveAccessRequest>().ExecuteAsync(request.Id, Ct);
        return credentials.Value.TemporaryPassword;
    }
}
