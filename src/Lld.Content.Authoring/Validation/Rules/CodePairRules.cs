using Lld.Content.Authoring.Uml;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>
/// A Code ↔ UML pair (spec 008 §3.4) names code that exists, pairs a class, state or sequence diagram, and quotes at
/// least one type (a lone member has nothing a diagram can draw). Any template may use pairs.
/// </summary>
public sealed class CodePairSnippetRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from pair in CodePairs.Of(document, context.Snippets)
        let problem = Problem(pair)
        where problem is not null
        select new ContentIssue(document.Path, pair.Diagram.Line, problem.Value.Code, problem.Value.Message);

    private static (string Code, string Message)? Problem(CodePair pair)
    {
        if (pair.Problem is not null)
        {
            return (IssueCodes.CodePairUnresolved, $"code=\"{pair.Reference}\": {pair.Problem}");
        }

        if (pair.Type == DiagramType.Other)
        {
            return (IssueCodes.CodePairNotSupported, "code= pairs a classDiagram, stateDiagram-v2 or sequenceDiagram with its C#.");
        }

        return SnippetCodeModel.Of(pair.Code!).Types.Count == 0
            ? (IssueCodes.CodePairNoTypes, $"code=\"{pair.Reference}\" quotes no type: name types (or the whole file), not members.")
            : null;
    }
}

/// <summary>
/// The diagram of a Code ↔ UML pair matches its code (spec 008 §3.4, <see cref="DiagramCodeLinks"/>): one issue per
/// mismatch, at the diagram line that draws it.
/// </summary>
public sealed class DiagramCodeMatchRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from pair in CodePairs.Of(document, context.Snippets)
        where pair.Code is not null && pair.Type != DiagramType.Other
        let code = SnippetCodeModel.Of(pair.Code!)
        where code.Types.Count > 0 // A snippet with no types is reported once, by CodePairSnippetRule.
        from mismatch in DiagramCodeLinks.For(pair.Diagram.Code, code).Mismatches
        select new ContentIssue(document.Path, pair.DocumentLine(mismatch.Line), IssueCodes.CodePairMismatch, mismatch.Message);
}
