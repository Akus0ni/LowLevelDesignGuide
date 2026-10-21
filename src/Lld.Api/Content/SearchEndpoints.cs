using System.Security.Claims;
using Lld.Api.Auth;
using Lld.Application.Notes;
using Lld.Contracts;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lld.Api.Content;

/// <summary>
/// Search for browser dev mode (spec 009 §3.8). Mapped only in Development, next to the content API: the apps search
/// on the device and never call it, so it costs no tunnel budget. Anonymous like content; a signed-in caller's notes
/// are searched too, a guest's (kept in the browser) are not (spec Q7).
/// </summary>
internal static class SearchEndpoints
{
    public const string BadKindCode = "validation.search.kind";

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/search", SearchAsync);
        return routes;
    }

    private static async Task<IResult> SearchAsync(
        string? q, string? kind, string? module, string? lessonId, int? limit, ClaimsPrincipal user,
        DevContentCatalog content, DevSearchIndex index, GetNotes getNotes, CancellationToken cancellationToken)
    {
        if (kind is not null && !SearchKinds.All.Contains(kind, StringComparer.Ordinal))
        {
            return Problem(StatusCodes.Status400BadRequest, BadKindCode, $"Unknown kind '{kind}'.");
        }

        var state = await content.GetAsync(cancellationToken);
        if (state.Pack is null)
        {
            return Problem(StatusCodes.Status500InternalServerError, ContentErrorCodes.Invalid, "content/ has problems; fix them and reload.");
        }

        var signedIn = user.Identity?.IsAuthenticated == true;
        var owner = signedIn ? ProgressOwner.User(user.UserId()) : ProgressOwner.Guest;
        IReadOnlyList<Note> notes = signedIn ? (await getNotes.ExecuteAsync(user.UserId(), cancellationToken)).Notes : [];
        var request = new SearchRequestDto(q ?? string.Empty, kind, module, lessonId, limit ?? Lld.Search.Sqlite.SqliteSearchIndex.MaxLimit);
        return TypedResults.Ok(await index.SearchAsync(state.Pack, owner.Key, notes, request, cancellationToken));
    }

    private static ProblemHttpResult Problem(int status, string code, string title) =>
        TypedResults.Problem(title: title, statusCode: status, extensions: new Dictionary<string, object?> { ["code"] = code });
}
