namespace Lld.Samples.Patterns.Behavioral.Command.Before;

/// <summary>
/// Buttons call the editor directly. Undo is a single saved copy that each button must remember to update, so only the
/// last action can be undone, and nothing can be queued, logged or replayed.
/// </summary>
public sealed class Toolbar(Editor editor)
{
    private string? _beforeLastAction;

    public void TypeClicked(string text)
    {
        _beforeLastAction = editor.Text;
        editor.Insert(text);
    }

    public void DeleteClicked(int count)
    {
        _beforeLastAction = editor.Text;
        editor.RemoveLast(count);
    }

    public void UndoClicked()
    {
        if (_beforeLastAction is not null)
        {
            editor.Text = _beforeLastAction;
            _beforeLastAction = null;
        }
    }
}
