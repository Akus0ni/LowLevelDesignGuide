namespace Lld.Samples.Patterns.Behavioral.Command.Modern;

/// <summary>
/// Modern C#: a command can be a record of two delegates. No class per action; the history works the same. Use real
/// command classes when they must be serialised, queued to another process or logged with their data.
/// </summary>
public sealed record EditAction(string Name, Action Do, Action Undo);

public static class Edits
{
    public static EditAction Type(Editor editor, string text) =>
        new("type", () => editor.Insert(text), () => editor.RemoveLast(text.Length));
}

public sealed class History
{
    private readonly Stack<EditAction> _done = new();

    public void Run(EditAction action)
    {
        action.Do();
        _done.Push(action);
    }

    public void Undo()
    {
        if (_done.TryPop(out var action))
        {
            action.Undo();
        }
    }
}
