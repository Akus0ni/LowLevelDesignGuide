using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lld.Application.Access;
using Lld.Application.Accounts;
using Lld.Contracts;
using Lld.Domain.Access;
using Lld.Domain.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Api.Tests;

public sealed class AuthApiTests(LldApiFactory factory) : IClassFixture<LldApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RequestAccess_Approve_TemporaryLogin_ForcedChange_ThenFullAccess()
    {
        using var client = factory.CreateAppClient();
        const string email = "newcomer@example.com";

        var requested = await client.PostAsJsonAsync("/api/access-requests", new AccessRequestRequest(email, "New Comer", "hello"), Ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var credentials = await ApproveOnlyPendingRequestAsync(email);
        var temporary = await LoginAsync(client, email, credentials.TemporaryPassword);
        temporary.User.MustChangePassword.Should().BeTrue();

        (await SendAsync(client, HttpMethod.Post, "/api/auth/logout-all", temporary.AccessToken)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden, "everything except me/change-password is blocked until the password is changed");
        (await SendAsync(client, HttpMethod.Get, "/api/auth/me", temporary.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var changed = await SendAsync(client, HttpMethod.Post, "/api/auth/change-password", temporary.AccessToken,
            new ChangePasswordRequest(credentials.TemporaryPassword, LldApiFactory.Password));
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await changed.Content.ReadFromJsonAsync<AuthResponse>(Ct))!;
        session.User.MustChangePassword.Should().BeFalse();

        (await SendAsync(client, HttpMethod.Post, "/api/auth/logout-all", session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestAccess_Twice_MergesIntoOnePendingRequest()
    {
        using var client = factory.CreateAppClient();
        const string email = "twice@example.com";

        await client.PostAsJsonAsync("/api/access-requests", new AccessRequestRequest(email, "First", null), Ct);
        await client.PostAsJsonAsync("/api/access-requests", new AccessRequestRequest(email, "Second", null), Ct);

        var pending = await ListPendingAsync();
        pending.Where(r => r.Email.Value == email).Should().ContainSingle().Which.Name.Should().Be("Second");
    }

    [Fact]
    public async Task RequestAccess_InvalidEmail_Returns400WithCode()
    {
        using var client = factory.CreateAppClient();

        var response = await client.PostAsJsonAsync("/api/access-requests", new AccessRequestRequest("nope", "X", null), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("validation.email.invalid");
    }

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount()
    {
        using var client = factory.CreateAppClient();
        const string email = "locked@example.com";
        await factory.CreateActiveUserAsync(client, email);

        HttpResponseMessage last = null!;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password-1"), Ct);
        }

        last.StatusCode.Should().Be((HttpStatusCode)StatusCodes.Locked);
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, LldApiFactory.Password), Ct)).StatusCode
            .Should().Be((HttpStatusCode)StatusCodes.Locked);
        (await AuditTypesAsync()).Should().Contain(AuditEventType.LockedOut);
    }

    [Fact]
    public async Task Refresh_Rotates_AndReplayRevokesTheFamily()
    {
        using var client = factory.CreateAppClient();
        var first = await factory.CreateActiveUserAsync(client, "rotate@example.com");

        var second = await RefreshAsync(client, first.RefreshToken);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = (await second.Content.ReadFromJsonAsync<AuthResponse>(Ct))!;
        rotated.RefreshToken.Should().NotBe(first.RefreshToken);

        var replay = await RefreshAsync(client, first.RefreshToken);
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCodeAsync(replay)).Should().Be("auth.refresh_token_reused");
        (await RefreshAsync(client, rotated.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await AuditTypesAsync()).Should().Contain(AuditEventType.RefreshTokenReuseDetected);
    }

    [Fact]
    public async Task Browser_GetsRefreshTokenOnlyAsAHardenedCookie()
    {
        using var web = factory.CreateWebClient();
        using var app = factory.CreateAppClient();
        const string email = "browser@example.com";
        await factory.CreateActiveUserAsync(app, email);

        var login = await web.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, LldApiFactory.Password), Ct);

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await login.Content.ReadFromJsonAsync<AuthResponse>(Ct))!.RefreshToken.Should().BeNull();
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("lld_rt=", StringComparison.Ordinal)).ToLowerInvariant();
        cookie.Should().Contain("httponly").And.Contain("secure").And.Contain("samesite=strict").And.Contain("path=/api/auth");

        var refreshed = await web.PostAsync("/api/auth/refresh", null, Ct);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK, "the cookie is sent back automatically");
        (await web.PostAsync("/api/auth/logout", null, Ct)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await web.PostAsync("/api/auth/refresh", null, Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAll_EndsEverySession()
    {
        using var client = factory.CreateAppClient();
        const string email = "everywhere@example.com";
        await factory.CreateActiveUserAsync(client, email);
        var phone = await LoginAsync(client, email, LldApiFactory.Password);
        var laptop = await LoginAsync(client, email, LldApiFactory.Password);

        var response = await SendAsync(client, HttpMethod.Post, "/api/auth/logout-all", laptop.AccessToken);

        (await response.Content.ReadFromJsonAsync<SignedOutEverywhereResponse>(Ct))!.SessionsRevoked.Should().BeGreaterThanOrEqualTo(2);
        (await RefreshAsync(client, phone.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(client, laptop.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_EnforcesPolicy()
    {
        using var client = factory.CreateAppClient();
        var session = await factory.CreateActiveUserAsync(client, "policy@example.com");

        var response = await SendAsync(client, HttpMethod.Post, "/api/auth/change-password", session.AccessToken,
            new ChangePasswordRequest(LldApiFactory.Password, "short"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("validation.password.too_short");
    }

    [Fact]
    public async Task ProtectedEndpoints_RequireAToken()
    {
        using var client = factory.CreateAppClient();

        (await client.GetAsync("/api/auth/me", Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/auth/logout-all", null, Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WrongPassword_IsGenericAndAudited()
    {
        using var client = factory.CreateAppClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ghost@example.com", "whatever-123"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCodeAsync(response)).Should().Be("auth.invalid_credentials");
        (await AuditTypesAsync()).Should().Contain(AuditEventType.LoginFailed);
    }

    private static Task<AuthResponse> LoginAsync(HttpClient client, string email, string password) =>
        LldApiFactory.LoginAsync(client, email, password);

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string? refreshToken) =>
        client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(refreshToken), Ct);

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, Ct);
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private async Task<IssuedCredentials> ApproveOnlyPendingRequestAsync(string email)
    {
        var request = (await ListPendingAsync()).Single(r => r.Email.Value == email);
        await using var scope = factory.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<ApproveAccessRequest>().ExecuteAsync(request.Id, Ct)).Value;
    }

    private async Task<IReadOnlyList<AccessRequest>> ListPendingAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ListAccessRequests>().ExecuteAsync(AccessRequestStatus.Pending, Ct);
    }

    private async Task<IEnumerable<AuditEventType>> AuditTypesAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<ListAuditEntries>().ExecuteAsync(1000, Ct)).Select(e => e.Type);
    }

    private static class StatusCodes
    {
        public const int Locked = 423;
    }
}
