using System.Security.Claims;
using Lld.Api.Auth;
using Lld.Application.Progress;
using Lld.Contracts;
using Lld.Wire;

namespace Lld.Api.Endpoints;

/// <summary>
/// The signed-in user's progress (spec 005 §3.10). Browser mode reads and writes it here; the app keeps its own copy on
/// the device until spec 010 syncs the two. A reset is a normal write of reset records, so there is no delete.
/// </summary>
internal static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder routes)
    {
        var progress = routes.MapGroup("/progress").RequireAuthorization();
        progress.MapGet("/", GetAsync);
        progress.MapPut("/lessons", SaveLessonsAsync);
        progress.MapPost("/activity", RecordActivityAsync);
        progress.MapPost("/adopt", AdoptAsync);
        return routes;
    }

    private static async Task<IResult> GetAsync(ClaimsPrincipal user, GetProgress useCase, CancellationToken cancellationToken) =>
        TypedResults.Ok(ProgressWire.ToDto(await useCase.ExecuteAsync(user.UserId(), cancellationToken)));

    private static async Task<IResult> SaveLessonsAsync(
        IReadOnlyList<LessonProgressDto> request, ClaimsPrincipal user, SaveLessonProgress useCase, CancellationToken cancellationToken)
    {
        var lessons = ProgressWire.ToLessons(request);
        if (lessons.IsFailure)
        {
            return lessons.Error!.ToProblem();
        }

        var result = await useCase.ExecuteAsync(user.UserId(), [.. lessons.Value], cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error!.ToProblem();
    }

    private static async Task<IResult> RecordActivityAsync(
        ActivityDayRequest request, ClaimsPrincipal user, RecordActivityDay useCase, CancellationToken cancellationToken)
    {
        var day = ProgressWire.ToDay(request.Day);
        if (day.IsFailure)
        {
            return day.Error!.ToProblem();
        }

        var result = await useCase.ExecuteAsync(user.UserId(), day.Value, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error!.ToProblem();
    }

    private static async Task<IResult> AdoptAsync(
        ProgressSnapshotDto request, ClaimsPrincipal user, AdoptGuestProgress useCase, CancellationToken cancellationToken)
    {
        var guest = ProgressWire.ToSnapshot(request);
        if (guest.IsFailure)
        {
            return guest.Error!.ToProblem();
        }

        var result = await useCase.ExecuteAsync(user.UserId(), guest.Value, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(ProgressWire.ToDto(result.Value)) : result.Error!.ToProblem();
    }
}
