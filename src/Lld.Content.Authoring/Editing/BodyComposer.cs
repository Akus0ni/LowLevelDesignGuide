using System.Text;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Editing;

/// <summary>Replace <see cref="Length"/> characters at <see cref="Start"/> (an index in the whole file) with new text.</summary>
public sealed record TextEdit(int Start, int Length, string Replacement)
{
    public int End => Start + Length;
}

/// <summary>Something that changes a document's text when it is packed: links, code fences, ...</summary>
public interface IBodyEditSource
{
    IEnumerable<TextEdit> EditsFor(SourceDocument source);
}

/// <summary>
/// Applies the edits of every <see cref="IBodyEditSource"/> to a document and returns the body without front-matter.
/// Edits are applied from the end of the file backwards, so each one's position stays valid; they may not overlap.
/// Text no edit touches stays byte-for-byte the same.
/// </summary>
public static class BodyComposer
{
    public static string Compose(SourceDocument source, IEnumerable<TextEdit> edits)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(edits);
        var text = new StringBuilder(source.Text);
        var limit = text.Length;
        foreach (var edit in edits.OrderByDescending(e => e.Start).ThenByDescending(e => e.Length))
        {
            if (edit.Start < source.BodyStart || edit.End > limit)
            {
                throw new InvalidOperationException(
                    $"{source.Path}: an edit at {edit.Start} (length {edit.Length}) overlaps another edit or the front-matter.");
            }

            text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Replacement);
            limit = edit.Start;
        }

        return text.ToString(source.BodyStart, text.Length - source.BodyStart);
    }
}
