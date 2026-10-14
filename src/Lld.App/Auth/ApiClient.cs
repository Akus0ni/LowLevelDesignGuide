using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lld.Contracts;

namespace Lld.App.Auth;

/// <summary>An error the React side can show: a stable code plus a human-readable message.</summary>
public sealed record BridgeError(string Code, string Message)
{
    public static readonly BridgeError Network =
        new("network.unavailable", "Can't reach the server. Check your connection and try again.");
}

/// <summary>Outcome of an API call: a value or a <see cref="BridgeError"/>; never an exception for expected failures.</summary>
public sealed record ApiResult<T>(T? Value, BridgeError? Error, int? Status = null)
{
    public bool Ok => Error is null;
}

/// <summary>
/// Typed HTTP client for the LLD Guide API. The base address comes from Aspire service discovery
/// (<c>http+https://api</c>: plain http preferred in dev, so no trusted dev certificate is needed).
/// </summary>
public sealed class ApiClient(HttpClient http)
{
    public const string ServiceAddress = "http+https://api";

    public Task<ApiResult<AcceptedResponse>> RequestAccessAsync(AccessRequestRequest request, CancellationToken ct) =>
        SendAsync<AcceptedResponse>(HttpMethod.Post, "/api/access-requests", request, accessToken: null, ct);

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "/api/auth/login", request, accessToken: null, ct);

    public Task<ApiResult<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "/api/auth/refresh", new RefreshRequest(refreshToken), accessToken: null, ct);

    public Task<ApiResult<object>> LogoutAsync(string refreshToken, CancellationToken ct) =>
        SendAsync<object>(HttpMethod.Post, "/api/auth/logout", new RefreshRequest(refreshToken), accessToken: null, ct);

    public Task<ApiResult<SignedOutEverywhereResponse>> LogoutAllAsync(string accessToken, CancellationToken ct) =>
        SendAsync<SignedOutEverywhereResponse>(HttpMethod.Post, "/api/auth/logout-all", body: null, accessToken, ct);

    public Task<ApiResult<AuthResponse>> ChangePasswordAsync(ChangePasswordRequest request, string accessToken, CancellationToken ct) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "/api/auth/change-password", request, accessToken, ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var value = response.Content.Headers.ContentLength == 0
                    ? default
                    : await response.Content.ReadFromJsonAsync<T>(ct);
                return new ApiResult<T>(value, null, (int)response.StatusCode);
            }

            return new ApiResult<T>(default, await ReadErrorAsync(response, ct), (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>(default, BridgeError.Network);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ApiResult<T>(default, BridgeError.Network);
        }
    }

    private static async Task<BridgeError> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var code = problem.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;
            var title = problem.RootElement.TryGetProperty("title", out var t) ? t.GetString() : null;
            return new BridgeError(code ?? $"http.{(int)response.StatusCode}", title ?? "The request failed.");
        }
        catch (JsonException)
        {
            return new BridgeError($"http.{(int)response.StatusCode}", "The request failed.");
        }
    }
}
