namespace Lld.Contracts;

// Wire contracts shared by the API and the MAUI app (the React app mirrors them in TypeScript).

public sealed record AccessRequestRequest(string Email, string Name, string? Note);

public sealed record LoginRequest(string Email, string Password);

/// <summary>Body for refresh/logout from the app. The browser sends nothing: its token is in an httpOnly cookie.</summary>
public sealed record RefreshRequest(string? RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserSummary(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword);

/// <summary>
/// Result of login, refresh and password change. <see cref="RefreshToken"/> is only filled for the app;
/// browser clients receive it as a cookie instead.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummary User);

public sealed record AcceptedResponse(string Message);

public sealed record SignedOutEverywhereResponse(int SessionsRevoked);

public static class ClientHeaders
{
    /// <summary>Header the browser app sends so the API uses the cookie transport for refresh tokens.</summary>
    public const string Client = "X-Lld-Client";

    public const string WebClient = "web";
}
