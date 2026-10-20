namespace Lld.Samples.Patterns.Behavioral.Memento.Before;

/// <summary>Every field is publicly settable, only so that undo can put it back.</summary>
public sealed class Editor
{
    public string Text { get; set; } = string.Empty;

    public int Cursor { get; set; }

    public void Type(string text)
    {
        Text = Text.Insert(Cursor, text);
        Cursor += text.Length;
    }
}

/// <summary>
/// The history reads and writes the editor's insides. Add a field (a selection) and undo silently forgets it until
/// someone remembers to update this class too.
/// </summary>
public sealed class History(Editor editor)
{
    private readonly Stack<(string Text, int Cursor)> _saved = new();

    public void Save() => _saved.Push((editor.Text, editor.Cursor));

    public void Undo()
    {
        if (_saved.TryPop(out var saved))
        {
            editor.Text = saved.Text;
            editor.Cursor = saved.Cursor;
        }
    }
}
