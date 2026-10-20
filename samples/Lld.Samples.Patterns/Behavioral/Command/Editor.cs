namespace Lld.Samples.Patterns.Behavioral.Command;

/// <summary>The receiver: a tiny text editor that knows how to change its text. Shared by every version.</summary>
public sealed class Editor
{
    public string Text { get; set; } = string.Empty;

    public void Insert(string text) => Text += text;

    /// <summary>Removes the last <paramref name="count"/> characters and returns them.</summary>
    public string RemoveLast(int count)
    {
        var removed = Text[^Math.Min(count, Text.Length)..];
        Text = Text[..^removed.Length];
        return removed;
    }
}
