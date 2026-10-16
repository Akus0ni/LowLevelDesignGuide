using Lld.Contracts;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lld.Content.Authoring.Parsing;

/// <summary>
/// Code-callout terms (spec 003 §3.8) from a lesson's entities table: a table whose first header is Entity, Class, Type
/// or Interface. The first <c>code</c> in a row's first cell is the term, the second cell is its role, and the heading
/// the table sits under is where "Read more" goes. The first definition of a term wins.
/// </summary>
public static class EntityTerms
{
    private static readonly HashSet<string> TableHeaders = new(StringComparer.OrdinalIgnoreCase) { "Entity", "Class", "Type", "Interface" };

    public static IReadOnlyList<LessonTerm> Of(SourceDocument source, IReadOnlyList<DocumentHeading> headings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(headings);
        var terms = new List<LessonTerm>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in source.Document.Descendants<Table>())
        {
            var rows = table.OfType<TableRow>().ToList();
            if (rows.Count < 2 || !rows[0].IsHeader || !TableHeaders.Contains(CellText(rows[0], 0)))
            {
                continue;
            }

            var anchor = AnchorAbove(headings, source.LineAt(table.Span.Start));
            foreach (var row in rows.Skip(1).Where(r => !r.IsHeader))
            {
                var term = FirstCode(row, 0);
                if (term is null || !seen.Add(term))
                {
                    continue;
                }

                terms.Add(new LessonTerm(term, CellText(row, 1), anchor));
            }
        }

        return terms;
    }

    private static string AnchorAbove(IReadOnlyList<DocumentHeading> headings, int line) =>
        headings.LastOrDefault(h => h.Line < line)?.Anchor ?? string.Empty;

    private static string? FirstCode(TableRow row, int column) =>
        Cell(row, column)?.Descendants<CodeInline>().FirstOrDefault()?.Content;

    private static string CellText(TableRow row, int column) =>
        Cell(row, column) is { } cell
            ? string.Join(' ', cell.Descendants<LeafBlock>().Select(block => DocumentHeadings.PlainText(block.Inline))).Trim()
            : string.Empty;

    private static TableCell? Cell(TableRow row, int column) =>
        column < row.Count ? row[column] as TableCell : null;
}
