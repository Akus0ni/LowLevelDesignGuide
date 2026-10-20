namespace Lld.Samples.Patterns.Behavioral.State.After;

/// <summary>The state: what each action does while the document is in that state. Refuses by default.</summary>
public abstract class DocumentState
{
    public abstract string Name { get; }

    public virtual DocumentState Edit(Document document, string text) => throw Refuse("edit");

    public virtual DocumentState Submit() => throw Refuse("submit");

    public virtual DocumentState Approve() => throw Refuse("approve");

    public virtual DocumentState Reject() => throw Refuse("reject");

    private InvalidOperationException Refuse(string action) => new($"Can't {action} a document that is {Name}.");
}

public sealed class Draft : DocumentState
{
    public override string Name => "Draft";

    public override DocumentState Edit(Document document, string text)
    {
        document.Text = text;
        return this;
    }

    public override DocumentState Submit() => new InReview();
}

public sealed class InReview : DocumentState
{
    public override string Name => "InReview";

    public override DocumentState Approve() => new Published();

    public override DocumentState Reject() => new Draft();
}

public sealed class Published : DocumentState
{
    public override string Name => "Published";
}

/// <summary>The context: hands every action to its current state and takes the state it gets back.</summary>
public sealed class Document
{
    private DocumentState _state = new Draft();

    public string Status => _state.Name;

    public string Text { get; internal set; } = string.Empty;

    public void Edit(string text) => _state = _state.Edit(this, text);

    public void Submit() => _state = _state.Submit();

    public void Approve() => _state = _state.Approve();

    public void Reject() => _state = _state.Reject();
}
