using System.Security.Claims;
using Lld.Application.Abstractions;
using Lld.Application.Auth;
using Lld.Contracts;
using Lld.Domain.Common;
using Lld.Infrastructure.Security;

namespace Lld.Api.Auth;

internal static class AuthMapping
{
    public static UserSummary ToSummary(UserAccount user) =>
        new(user.Id, user.Email.Value, user.DisplayName, user.Roles, user.MustChangePassword);

    public static AuthResponse ToResponse(SignInResult session, bool includeRefreshToken) =>
        new(
            session.AccessToken.Value,
            session.AccessToken.ExpiresAt,
            includeRefreshToken ? session.RefreshToken : null,
            session.RefreshTokenExpiresAt,
            ToSummary(session.User));

    public static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(LldClaims.Subject) ?? throw new InvalidOperationException("Authenticated user has no subject claim.");
}

/// <summary>Turns <see cref="Error"/> codes into HTTP problem responses, in one place.</summary>
internal static class ResultHttpMapping
{
    public static IResult ToProblem(this Error error)
    {
        var status = StatusFor(error.Code);
        return TypedResults.Problem(
            title: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static int StatusFor(string code) => code switch
    {
        _ when code.StartsWith("validation.", StringComparison.Ordinal) => StatusCodes.Status400BadRequest,
        "auth.invalid_credentials" or "auth.session_invalid" or "auth.refresh_token_reused" => StatusCodes.Status401Unauthorized,
        "auth.locked_out" => StatusCodes.Status423Locked,
        _ when code.EndsWith(".not_found", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
        _ when code.EndsWith(".not_pending", StringComparison.Ordinal) || code.EndsWith(".already_exists", StringComparison.Ordinal)
            => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}
