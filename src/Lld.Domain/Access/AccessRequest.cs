using Lld.Domain.Common;
using Lld.Domain.Identity;

namespace Lld.Domain.Access;

public enum AccessRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>
/// A person asking to use the app. Its lifecycle is a small state machine
/// (<c>Pending → Approved | Rejected</c>) implemented with the State pattern:
/// every operation is delegated to the object for the current <see cref="Status"/>,
/// so the rules for "what is allowed now" live in one class per state.
/// </summary>
public sealed class AccessRequest : AggregateRoot
{
    public const int MaxNameLength = 100;
    public const int MaxNoteLength = 500;

    private AccessRequest(Guid id, EmailAddress email, string name, string? note, DateTimeOffset now)
    {
        Id = id;
        Email = email;
        Name = name;
        Note = note;
        Status = AccessRequestStatus.Pending;
        CreatedAt = now;
        UpdatedAt = now;
    }

    // Required by EF Core.
    private AccessRequest()
    {
        Email = null!;
        Name = null!;
    }

    public Guid Id { get; private set; }

    public EmailAddress Email { get; private set; }

    public string Name { get; private set; }

    public string? Note { get; private set; }

    public AccessRequestStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    private IAccessRequestState State => AccessRequestStates.For(Status);

    public static Result<AccessRequest> Submit(EmailAddress email, string? name, string? note, DateTimeOffset now)
    {
        var details = ValidateDetails(name, note);
        if (details.IsFailure)
        {
            return details.Error!;
        }

        var request = new AccessRequest(Guid.NewGuid(), email, details.Value.Name, details.Value.Note, now);
        request.Raise(new AccessRequestSubmitted(request.Id, email.Value, now));
        return request;
    }

    /// <summary>A repeat request from the same email updates the pending one instead of creating a duplicate.</summary>
    public Result Merge(string? name, string? note, DateTimeOffset now)
    {
        var details = ValidateDetails(name, note);
        return details.IsFailure ? details.Error! : State.Merge(this, details.Value, now);
    }

    public Result Approve(DateTimeOffset now) => State.Approve(this, now);

    public Result Reject(DateTimeOffset now) => State.Reject(this, now);

    // Mutators used only by the state objects.
    internal void ApplyMerge(RequestDetails details, DateTimeOffset now)
    {
        Name = details.Name;
        Note = details.Note;
        UpdatedAt = now;
        Raise(new AccessRequestMerged(Id, Email.Value, now));
    }

    internal void ApplyDecision(AccessRequestStatus decision, DateTimeOffset now)
    {
        Status = decision;
        DecidedAt = now;
        UpdatedAt = now;
        Raise(decision == AccessRequestStatus.Approved
            ? new AccessRequestApproved(Id, Email.Value, now)
            : new AccessRequestRejected(Id, Email.Value, now));
    }

    private static Result<RequestDetails> ValidateDetails(string? name, string? note)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (trimmedName.Length == 0)
        {
            return Error.Validation("name.required", "Name is required.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            return Error.Validation("name.too_long", $"Name must be at most {MaxNameLength} characters.");
        }

        if (trimmedNote?.Length > MaxNoteLength)
        {
            return Error.Validation("note.too_long", $"Note must be at most {MaxNoteLength} characters.");
        }

        return new RequestDetails(trimmedName, trimmedNote);
    }
}

internal readonly record struct RequestDetails(string Name, string? Note);

public sealed record AccessRequestSubmitted(Guid RequestId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record AccessRequestMerged(Guid RequestId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record AccessRequestApproved(Guid RequestId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record AccessRequestRejected(Guid RequestId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;
