namespace Lld.Domain.Audit;

public enum AuditEventType
{
    AccessRequested = 0,
    AccessRequestMerged = 1,
    AccessApproved = 2,
    AccessRejected = 3,
    UserCreated = 4,
    UserDisabled = 5,
    LoginSucceeded = 6,
    LoginFailed = 7,
    LockedOut = 8,
    PasswordChanged = 9,
    PasswordReset = 10,
    SignedOut = 11,
    SignedOutEverywhere = 12,
    RefreshTokenReuseDetected = 13,
}

/// <summary>Append-only record of a security-relevant event. Never updated or deleted.</summary>
public sealed class AuditEntry
{
    private AuditEntry(AuditEventType type, string? email, string? userId, DateTimeOffset occurredAt, string? details)
    {
        Id = Guid.NewGuid();
        Type = type;
        Email = email;
        UserId = userId;
        OccurredAt = occurredAt;
        Details = details;
    }

    // Required by EF Core.
    private AuditEntry()
    {
    }

    public Guid Id { get; private set; }

    public AuditEventType Type { get; private set; }

    public string? Email { get; private set; }

    public string? UserId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string? Details { get; private set; }

    public static AuditEntry Record(
        AuditEventType type, DateTimeOffset occurredAt, string? email = null, string? userId = null, string? details = null) =>
        new(type, email, userId, occurredAt, details);
}
