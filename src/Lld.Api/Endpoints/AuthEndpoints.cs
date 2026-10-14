using System.Security.Claims;
using Lld.Api.Auth;
using Lld.Application.Access;
using Lld.Application.Auth;
using Lld.Contracts;

namespace Lld.Api.Endpoints;

internal static class AuthEndpoints
{
    public const string RateLimitPolicy = "auth";

    public static IEndpointRouteBuilder MapAccessRequestEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/access-requests", SubmitAccessRequestAsync).RequireRateLimiting(RateLimitPolicy);
        return routes;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/auth");
        auth.MapPost("/login", LoginAsync).RequireRateLimiting(RateLimitPolicy);
        auth.MapPost("/refresh", RefreshAsync).RequireRateLimiting(RateLimitPolicy);
        auth.MapPost("/logout", LogoutAsync);
        auth.MapPost("/logout-all", LogoutAllAsync).RequireAuthorization();
        auth.MapPost("/change-password", ChangePasswordAsync).RequireAuthorization(AuthorizationPolicies.PasswordChangePending);
        auth.MapGet("/me", GetMeAsync).RequireAuthorization(AuthorizationPolicies.PasswordChangePending);
        return routes;
    }

    private static async Task<IResult> SubmitAccessRequestAsync(
        AccessRequestRequest request, SubmitAccessRequest useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new SubmitAccessRequestCommand(request.Email, request.Name, request.Note), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted((string?)null, new AcceptedResponse("Thanks! The admin will review your request and contact you."))
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, Login useCase, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(RefreshTokenTransports.For(context).Write(context, result.Value))
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest? request, RefreshSession useCase, HttpContext context, CancellationToken cancellationToken)
    {
        var transport = RefreshTokenTransports.For(context);
        var result = await useCase.ExecuteAsync(transport.Read(context, request), cancellationToken);
        if (result.IsFailure)
        {
            transport.Clear(context);
            return result.Error!.ToProblem();
        }

        return TypedResults.Ok(transport.Write(context, result.Value));
    }

    private static async Task<IResult> LogoutAsync(
        RefreshRequest? request, Logout useCase, HttpContext context, CancellationToken cancellationToken)
    {
        var transport = RefreshTokenTransports.For(context);
        await useCase.ExecuteAsync(transport.Read(context, request), cancellationToken);
        transport.Clear(context);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> LogoutAllAsync(
        ClaimsPrincipal user, LogoutAllDevices useCase, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(user.UserId(), cancellationToken);
        RefreshTokenTransports.For(context).Clear(context);
        return TypedResults.Ok(new SignedOutEverywhereResponse(result.Value));
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request, ClaimsPrincipal user, ChangePassword useCase, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new ChangePasswordCommand(user.UserId(), request.CurrentPassword, request.NewPassword), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(RefreshTokenTransports.For(context).Write(context, result.Value))
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> GetMeAsync(ClaimsPrincipal user, GetCurrentUser useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(user.UserId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(AuthMapping.ToSummary(result.Value)) : result.Error!.ToProblem();
    }
}
