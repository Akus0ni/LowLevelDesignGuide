namespace Lld.Samples.Patterns.Behavioral.Memento.After;

/// <summary>
/// The originator: keeps its fields private and hands out a sealed snapshot of them. Only the editor knows what is
/// inside a snapshot, so a new field is saved without anyone else changing.
/// </summary>
public sealed class Editor
{
    private string _text = string.Empty;
    private int _cursor;

    public string Text => _text;

    public int Cursor => _cursor;

    public void Type(string text)
    {
        _text = _text.Insert(_cursor, text);
        _cursor += text.Length;
    }

    public void MoveCursor(int position) => _cursor = Math.Clamp(position, 0, _text.Length);

    public Snapshot Save() => new(_text, _cursor);

    public void Restore(Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        (_text, _cursor) = (snapshot.Text, snapshot.Cursor);
    }

    /// <summary>The memento: opaque to everyone but the editor (its contents are not public).</summary>
    public sealed class Snapshot
    {
        internal Snapshot(string text, int cursor) => (Text, Cursor) = (text, cursor);

        internal string Text { get; }

        internal int Cursor { get; }
    }
}

/// <summary>The caretaker: stores snapshots and hands them back, without looking inside.</summary>
public sealed class History(Editor editor)
{
    private readonly Stack<Editor.Snapshot> _saved = new();

    public void Save() => _saved.Push(editor.Save());

    public void Undo()
    {
        if (_saved.TryPop(out var snapshot))
        {
            editor.Restore(snapshot);
        }
    }
}
