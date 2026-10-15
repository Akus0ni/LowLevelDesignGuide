using Lld.Application.Abstractions;
using Lld.Domain.Access;
using Lld.Domain.Accounts;
using Lld.Domain.Audit;
using Lld.Domain.Common;

namespace Lld.Application.Audit;

/// <summary>
/// Observes domain events and appends an audit entry for each security-relevant one.
/// Runs inside the unit-of-work commit, so the audit row is saved in the same transaction as the change.
/// </summary>
public sealed class AuditTrail(IAuditLog auditLog) : IDomainEventObserver
{
    public void Handle(IDomainEvent domainEvent)
    {
        var entry = ToAuditEntry(domainEvent);
        if (entry is not null)
        {
            auditLog.Append(entry);
        }
    }

    private static AuditEntry? ToAuditEntry(IDomainEvent e) => e switch
    {
        AccessRequestSubmitted x => AuditEntry.Record(AuditEventType.AccessRequested, x.OccurredAt, x.Email, details: $"request {x.RequestId}"),
        AccessRequestMerged x => AuditEntry.Record(AuditEventType.AccessRequestMerged, x.OccurredAt, x.Email, details: $"request {x.RequestId}"),
        AccessRequestApproved x => AuditEntry.Record(AuditEventType.AccessApproved, x.OccurredAt, x.Email, details: $"request {x.RequestId}"),
        AccessRequestRejected x => AuditEntry.Record(AuditEventType.AccessRejected, x.OccurredAt, x.Email, details: $"request {x.RequestId}"),
        UserCreated x => AuditEntry.Record(AuditEventType.UserCreated, x.OccurredAt, x.Email, x.UserId, $"role {x.Role}"),
        UserDisabled x => AuditEntry.Record(AuditEventType.UserDisabled, x.OccurredAt, x.Email, x.UserId),
        LoginSucceeded x => AuditEntry.Record(AuditEventType.LoginSucceeded, x.OccurredAt, x.Email, x.UserId),
        LoginFailed x => AuditEntry.Record(AuditEventType.LoginFailed, x.OccurredAt, x.Email, details: x.Reason),
        UserLockedOut x => AuditEntry.Record(AuditEventType.LockedOut, x.OccurredAt, x.Email, x.UserId),
        PasswordChanged x => AuditEntry.Record(AuditEventType.PasswordChanged, x.OccurredAt, x.Email, x.UserId),
        PasswordReset x => AuditEntry.Record(AuditEventType.PasswordReset, x.OccurredAt, x.Email, x.UserId),
        SignedOut x => AuditEntry.Record(AuditEventType.SignedOut, x.OccurredAt, userId: x.UserId, details: $"family {x.FamilyId}"),
        SignedOutEverywhere x => AuditEntry.Record(AuditEventType.SignedOutEverywhere, x.OccurredAt, userId: x.UserId, details: $"{x.SessionsRevoked} session(s)"),
        RefreshTokenReuseDetected x => AuditEntry.Record(AuditEventType.RefreshTokenReuseDetected, x.OccurredAt, userId: x.UserId, details: $"family {x.FamilyId} revoked"),
        _ => null,
    };
}
