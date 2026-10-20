namespace Lld.Samples.Patterns.Behavioral.State.Modern;

public enum Status
{
    Draft,
    InReview,
    Published,
}

public enum DocumentAction
{
    Submit,
    Approve,
    Reject,
}

/// <summary>
/// Modern C#: a small machine fits in one transition table, a <c>switch</c> on (state, action). Every legal move is on
/// one screen. Move to state classes when each state carries lots of its own behaviour.
/// </summary>
public static class DocumentFlow
{
    public static Status Next(Status status, DocumentAction action) => (status, action) switch
    {
        (Status.Draft, DocumentAction.Submit) => Status.InReview,
        (Status.InReview, DocumentAction.Approve) => Status.Published,
        (Status.InReview, DocumentAction.Reject) => Status.Draft,
        _ => throw new InvalidOperationException($"Can't {action} a document that is {status}."),
    };
}
