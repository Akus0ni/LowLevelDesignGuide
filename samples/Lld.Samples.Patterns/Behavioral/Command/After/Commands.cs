namespace Lld.Samples.Patterns.Behavioral.Command.After;

/// <summary>The command: a request turned into an object that can run and be reversed.</summary>
public interface ICommand
{
    void Execute();

    void Undo();
}

public sealed class TypeCommand(Editor editor, string text) : ICommand
{
    public void Execute() => editor.Insert(text);

    public void Undo() => editor.RemoveLast(text.Length);
}

/// <summary>Remembers what it deleted, so it can put it back.</summary>
public sealed class DeleteCommand(Editor editor, int count) : ICommand
{
    private string _removed = string.Empty;

    public void Execute() => _removed = editor.RemoveLast(count);

    public void Undo() => editor.Insert(_removed);
}

/// <summary>The invoker: runs commands and keeps them, so any number of steps can be undone.</summary>
public sealed class CommandHistory
{
    private readonly Stack<ICommand> _done = new();

    public int Count => _done.Count;

    public void Run(ICommand command)
    {
        command.Execute();
        _done.Push(command);
    }

    public void Undo()
    {
        if (_done.TryPop(out var command))
        {
            command.Undo();
        }
    }
}
