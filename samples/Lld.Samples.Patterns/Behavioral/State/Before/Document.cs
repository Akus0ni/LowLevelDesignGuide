namespace Lld.Samples.Patterns.Behavioral.State.Before;

public enum DocumentStatus
{
    Draft,
    InReview,
    Published,
}

/// <summary>
/// Every action switches on the status. A new status ("Archived") means visiting every method, and the rules for one
/// status are spread across all of them.
/// </summary>
public sealed class Document
{
    public DocumentStatus Status { get; private set; } = DocumentStatus.Draft;

    public string Text { get; private set; } = string.Empty;

    public void Edit(string text)
    {
        switch (Status)
        {
            case DocumentStatus.Draft:
                Text = text;
                break;
            default:
                throw new InvalidOperationException($"Can't edit a document that is {Status}.");
        }
    }

    public void Submit() => Status = Status == DocumentStatus.Draft
        ? DocumentStatus.InReview
        : throw new InvalidOperationException($"Can't submit a document that is {Status}.");

    public void Approve() => Status = Status == DocumentStatus.InReview
        ? DocumentStatus.Published
        : throw new InvalidOperationException($"Can't approve a document that is {Status}.");

    public void Reject() => Status = Status == DocumentStatus.InReview
        ? DocumentStatus.Draft
        : throw new InvalidOperationException($"Can't reject a document that is {Status}.");
}
