using Lld.Domain.Common;

namespace Lld.Domain.Access;

/// <summary>State pattern: what an <see cref="AccessRequest"/> may do in its current status.</summary>
internal interface IAccessRequestState
{
    Result Merge(AccessRequest request, RequestDetails details, DateTimeOffset now);

    Result Approve(AccessRequest request, DateTimeOffset now);

    Result Reject(AccessRequest request, DateTimeOffset now);
}

internal static class AccessRequestStates
{
    private static readonly IAccessRequestState Pending = new PendingState();
    private static readonly IAccessRequestState Approved = new DecidedState("approved");
    private static readonly IAccessRequestState Rejected = new DecidedState("rejected");

    public static IAccessRequestState For(AccessRequestStatus status) => status switch
    {
        AccessRequestStatus.Pending => Pending,
        AccessRequestStatus.Approved => Approved,
        AccessRequestStatus.Rejected => Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown access request status."),
    };

    /// <summary>The only state that allows changes.</summary>
    private sealed class PendingState : IAccessRequestState
    {
        public Result Merge(AccessRequest request, RequestDetails details, DateTimeOffset now)
        {
            request.ApplyMerge(details, now);
            return Result.Success();
        }

        public Result Approve(AccessRequest request, DateTimeOffset now)
        {
            request.ApplyDecision(AccessRequestStatus.Approved, now);
            return Result.Success();
        }

        public Result Reject(AccessRequest request, DateTimeOffset now)
        {
            request.ApplyDecision(AccessRequestStatus.Rejected, now);
            return Result.Success();
        }
    }

    /// <summary>Approved and Rejected are final: every transition is refused with the same error.</summary>
    private sealed class DecidedState(string label) : IAccessRequestState
    {
        public Result Merge(AccessRequest request, RequestDetails details, DateTimeOffset now) => NotPending();

        public Result Approve(AccessRequest request, DateTimeOffset now) => NotPending();

        public Result Reject(AccessRequest request, DateTimeOffset now) => NotPending();

        private Error NotPending() =>
            new("access_request.not_pending", $"This request was already {label}.");
    }
}
