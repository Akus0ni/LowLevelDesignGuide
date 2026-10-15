using Lld.Content;
using Lld.Content.Authoring;
using Lld.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lld.Api.Content;

/// <summary>
/// Read-only content for browser dev mode and tests (spec 003). Mapped only in Development: the hosted API never
/// serves content (the apps carry their own copy), so it costs no tunnel budget. Anonymous: reading is open.
/// </summary>
internal static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder routes)
    {
        var content = routes.MapGroup("/content");
        content.MapGet("/modules", (DevContentCatalog source, CancellationToken ct) =>
            QueryAsync(source, (catalog, t) => catalog.GetModulesAsync(t)!, ct));
        content.MapGet("/modules/{slug}", (string slug, DevContentCatalog source, CancellationToken ct) =>
            QueryAsync(source, (catalog, t) => catalog.GetModuleAsync(slug, t), ct));
        content.MapGet("/lessons/{id}", (string id, DevContentCatalog source, CancellationToken ct) =>
            QueryAsync(source, (catalog, t) => catalog.GetLessonAsync(id, t), ct));
        content.MapGet("/cheatsheets", (string? module, DevContentCatalog source, CancellationToken ct) =>
            QueryAsync(source, (catalog, t) => catalog.GetCheatSheetsAsync(module, t)!, ct));
        content.MapGet("/cheatsheets/{lessonId}", (string lessonId, DevContentCatalog source, CancellationToken ct) =>
            QueryAsync(source, (catalog, t) => catalog.GetCheatSheetAsync(lessonId, t), ct));
        return routes;
    }

    private static async Task<IResult> QueryAsync<T>(
        DevContentCatalog source, Func<IContentCatalog, CancellationToken, Task<T?>> query, CancellationToken cancellationToken)
        where T : class
    {
        var state = await source.GetAsync(cancellationToken);
        if (state.Catalog is null)
        {
            return Invalid(state.Issues);
        }

        var value = await query(state.Catalog, cancellationToken);
        return value is null
            ? Problem(StatusCodes.Status404NotFound, ContentErrorCodes.NotFound, "Not found.")
            : TypedResults.Ok(value);
    }

    /// <summary>The author sees exactly what to fix, in the browser, as the build would print it.</summary>
    private static ProblemHttpResult Invalid(IReadOnlyList<ContentIssue> issues) =>
        Problem(
            StatusCodes.Status500InternalServerError,
            ContentErrorCodes.Invalid,
            $"content/ has {issues.Count} problem(s); fix them and reload.",
            issues.Select(i => i.ToString()).ToList());

    private static ProblemHttpResult Problem(int status, string code, string title, IReadOnlyList<string>? issues = null)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = code };
        if (issues is not null)
        {
            extensions["issues"] = issues;
        }

        return TypedResults.Problem(title: title, statusCode: status, extensions: extensions);
    }
}
