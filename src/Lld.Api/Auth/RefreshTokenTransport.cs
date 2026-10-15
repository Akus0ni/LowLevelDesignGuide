using Lld.Application.Auth;
using Lld.Contracts;

namespace Lld.Api.Auth;

/// <summary>
/// Strategy: how a refresh token travels between client and API.
/// The app keeps it in SecureStorage and sends it in the body; the browser must never be able to read it
/// from JavaScript, so it lives in an httpOnly cookie.
/// </summary>
public interface IRefreshTokenTransport
{
    string? Read(HttpContext context, RefreshRequest? body);

    AuthResponse Write(HttpContext context, SignInResult session);

    void Clear(HttpContext context);
}

internal sealed class BodyRefreshTokenTransport : IRefreshTokenTransport
{
    public string? Read(HttpContext context, RefreshRequest? body) => body?.RefreshToken;

    public AuthResponse Write(HttpContext context, SignInResult session) =>
        AuthMapping.ToResponse(session, includeRefreshToken: true);

    public void Clear(HttpContext context)
    {
        // Nothing stored server-side for the app; it deletes its own copy.
    }
}

internal sealed class CookieRefreshTokenTransport : IRefreshTokenTransport
{
    public const string CookieName = "lld_rt";
    public const string CookiePath = "/api/auth";

    public string? Read(HttpContext context, RefreshRequest? body) => context.Request.Cookies[CookieName];

    public AuthResponse Write(HttpContext context, SignInResult session)
    {
        context.Response.Cookies.Append(CookieName, session.RefreshToken, Options(session.RefreshTokenExpiresAt));
        return AuthMapping.ToResponse(session, includeRefreshToken: false);
    }

    public void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(CookieName, Options(DateTimeOffset.UnixEpoch));

    private static CookieOptions Options(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expires,
        IsEssential = true,
    };
}

/// <summary>Picks the transport per request from the client header.</summary>
public static class RefreshTokenTransports
{
    private static readonly IRefreshTokenTransport Body = new BodyRefreshTokenTransport();
    private static readonly IRefreshTokenTransport Cookie = new CookieRefreshTokenTransport();

    public static IRefreshTokenTransport For(HttpContext context) =>
        string.Equals(context.Request.Headers[ClientHeaders.Client], ClientHeaders.WebClient, StringComparison.OrdinalIgnoreCase)
            ? Cookie
            : Body;
}
