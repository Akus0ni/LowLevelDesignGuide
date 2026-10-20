namespace Lld.Samples.Patterns.Behavioral.Memento.Modern;

/// <summary>
/// Modern C#: make the state an immutable record. Every change returns a new state, so the old one already is a
/// snapshot; undo is just keeping the previous values (the Redux / MVU idea).
/// </summary>
public sealed record EditorState(string Text = "", int Cursor = 0)
{
    public EditorState Type(string text) => this with { Text = Text.Insert(Cursor, text), Cursor = Cursor + text.Length };
}

public sealed class Editor
{
    private readonly Stack<EditorState> _past = new();

    public EditorState State { get; private set; } = new();

    public void Type(string text)
    {
        _past.Push(State);
        State = State.Type(text);
    }

    public void Undo()
    {
        if (_past.TryPop(out var previous))
        {
            State = previous;
        }
    }
}
