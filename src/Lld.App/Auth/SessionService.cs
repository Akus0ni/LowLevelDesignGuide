using Lld.Contracts;

namespace Lld.App.Auth;

/// <summary>Where the refresh token lives on the device (Strategy: SecureStorage in the app, memory in tests).</summary>
public interface ITokenStore
{
    Task<string?> GetRefreshTokenAsync();

    Task SetRefreshTokenAsync(string? refreshToken);
}

/// <summary>Keychain (iOS/macOS), Keystore-backed storage (Android), DPAPI (Windows).</summary>
public sealed class SecureTokenStore(ISecureStorage storage) : ITokenStore
{
    private const string RefreshTokenKey = "lld.refresh_token";

    public Task<string?> GetRefreshTokenAsync() => storage.GetAsync(RefreshTokenKey);

    public Task SetRefreshTokenAsync(string? refreshToken)
    {
        if (refreshToken is null)
        {
            storage.Remove(RefreshTokenKey);
            return Task.CompletedTask;
        }

        return storage.SetAsync(RefreshTokenKey, refreshToken);
    }
}

/// <summary>
/// Owns the app's session: the access token stays in memory, the refresh token in <see cref="ITokenStore"/>.
/// Refreshes are single-flight (one at a time) so rotation never presents the same token twice.
/// </summary>
public sealed class SessionService(ApiClient api, ITokenStore tokens) : IDisposable
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;

    public async Task<ApiResult<UserSummary>> LoginAsync(string email, string password, CancellationToken ct) =>
        await AcceptAsync(await api.LoginAsync(new LoginRequest(email, password), ct));

    /// <summary>Restores the session from the stored refresh token; a null value means signed out.</summary>
    public async Task<ApiResult<UserSummary?>> RestoreAsync(CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var refreshToken = await tokens.GetRefreshTokenAsync();
            if (refreshToken is null)
            {
                return new ApiResult<UserSummary?>(null, null);
            }

            var refreshed = await api.RefreshAsync(refreshToken, ct);
            if (refreshed.Status == 401)
            {
                await ClearAsync();
                return new ApiResult<UserSummary?>(null, null);
            }

            var accepted = await AcceptAsync(refreshed);
            return new ApiResult<UserSummary?>(accepted.Value, accepted.Error);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public Task<ApiResult<UserSummary>> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct) =>
        WithAccessTokenAsync(
            token => api.ChangePasswordAsync(new ChangePasswordRequest(currentPassword, newPassword), token, ct),
            AcceptAsync,
            ct);

    public async Task<ApiResult<bool>> LogoutAsync(CancellationToken ct)
    {
        var refreshToken = await tokens.GetRefreshTokenAsync();
        if (refreshToken is not null)
        {
            // Best effort: even if the server is unreachable, this device forgets the session.
            await api.LogoutAsync(refreshToken, ct);
        }

        await ClearAsync();
        return new ApiResult<bool>(true, null);
    }

    public async Task<ApiResult<bool>> LogoutAllAsync(CancellationToken ct)
    {
        var result = await WithAccessTokenAsync(
            token => api.LogoutAllAsync(token, ct),
            r => Task.FromResult(new ApiResult<bool>(r.Ok, r.Error, r.Status)),
            ct);
        if (result.Ok)
        {
            await ClearAsync();
        }

        return result;
    }

    public void Dispose() => _refreshLock.Dispose();

    /// <summary>Calls the API with the access token; on 401 refreshes once and retries.</summary>
    private async Task<ApiResult<TOut>> WithAccessTokenAsync<TIn, TOut>(
        Func<string, Task<ApiResult<TIn>>> call, Func<ApiResult<TIn>, Task<ApiResult<TOut>>> map, CancellationToken ct)
    {
        if (_accessToken is null && (await RestoreAsync(ct)).Value is null)
        {
            return new ApiResult<TOut>(default, new BridgeError("auth.session_invalid", "Please sign in again."), 401);
        }

        var result = await call(_accessToken!);
        if (result.Status == 401 && (await RestoreAsync(ct)).Value is not null)
        {
            result = await call(_accessToken!);
        }

        return await map(result);
    }

    private async Task<ApiResult<UserSummary>> AcceptAsync(ApiResult<AuthResponse> result)
    {
        if (!result.Ok || result.Value is null)
        {
            return new ApiResult<UserSummary>(default, result.Error, result.Status);
        }

        _accessToken = result.Value.AccessToken;
        await tokens.SetRefreshTokenAsync(result.Value.RefreshToken);
        return new ApiResult<UserSummary>(result.Value.User, null, result.Status);
    }

    private async Task ClearAsync()
    {
        _accessToken = null;
        await tokens.SetRefreshTokenAsync(null);
    }
}
