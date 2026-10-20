using System.Security.Claims;
using Lld.Api.Auth;
using Lld.Application.Notes;
using Lld.Contracts;
using Lld.Wire;
using Microsoft.AspNetCore.Mvc;

namespace Lld.Api.Endpoints;

/// <summary>
/// The signed-in user's notes and bookmarks (spec 007 §3.9). Browser mode reads and writes them here; the app keeps its
/// own copy on the device until spec 010 syncs the two. A delete is a write with <c>deletedAt</c>, so there is no DELETE.
/// </summary>
internal static class NotesEndpoints
{
    /// <summary>The client splits its queue into batches below this (plan §2.4).</summary>
    public const long MaxRequestBytes = 1_048_576;

    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder routes)
    {
        var notes = routes.MapGroup("/").RequireAuthorization().WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes));
        notes.MapGet("/notes", GetAsync);
        notes.MapPut("/notes", SaveNotesAsync);
        notes.MapPut("/bookmarks", SaveBookmarksAsync);
        notes.MapPost("/notes/adopt", AdoptAsync);
        return routes;
    }

    private static async Task<IResult> GetAsync(ClaimsPrincipal user, GetNotes useCase, CancellationToken cancellationToken) =>
        TypedResults.Ok(NotesWire.ToDto(await useCase.ExecuteAsync(user.UserId(), cancellationToken)));

    private static async Task<IResult> SaveNotesAsync(
        IReadOnlyList<NoteDto> request, ClaimsPrincipal user, SaveNotes useCase, CancellationToken cancellationToken)
    {
        var notes = NotesWire.ToNotes(request);
        if (notes.IsFailure)
        {
            return notes.Error!.ToProblem();
        }

        var result = await useCase.ExecuteAsync(user.UserId(), [.. notes.Value], cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error!.ToProblem();
    }

    private static async Task<IResult> SaveBookmarksAsync(
        IReadOnlyList<BookmarkDto> request, ClaimsPrincipal user, SaveBookmarks useCase, CancellationToken cancellationToken)
    {
        var bookmarks = NotesWire.ToBookmarks(request);
        if (bookmarks.IsFailure)
        {
            return bookmarks.Error!.ToProblem();
        }

        var result = await useCase.ExecuteAsync(user.UserId(), [.. bookmarks.Value], cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error!.ToProblem();
    }

    private static async Task<IResult> AdoptAsync(
        NotesSnapshotDto request, ClaimsPrincipal user, AdoptGuestNotes useCase, CancellationToken cancellationToken)
    {
        var guest = NotesWire.ToSnapshot(request);
        if (guest.IsFailure)
        {
            return guest.Error!.ToProblem();
        }

        var result = await useCase.ExecuteAsync(user.UserId(), guest.Value, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(NotesWire.ToDto(result.Value)) : result.Error!.ToProblem();
    }
}
