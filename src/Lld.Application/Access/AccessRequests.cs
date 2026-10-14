using Lld.Application.Abstractions;
using Lld.Application.Accounts;
using Lld.Application.Common;
using Lld.Domain.Access;
using Lld.Domain.Common;
using Lld.Domain.Identity;

namespace Lld.Application.Access;

public sealed record SubmitAccessRequestCommand(string Email, string Name, string? Note);

/// <summary>
/// Records a request to use the app. The caller always gets the same answer (new, merged, or the
/// email already has an account) so the endpoint can't be used to discover who has an account.
/// </summary>
public sealed class SubmitAccessRequest(
    IAccessRequestRepository requests,
    IUserAccounts accounts,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result> ExecuteAsync(SubmitAccessRequestCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error!;
        }

        if (await accounts.FindByEmailAsync(email.Value, cancellationToken) is not null)
        {
            return Result.Success();
        }

        var now = time.GetUtcNow();
        var pending = await requests.FindPendingByEmailAsync(email.Value, cancellationToken);
        if (pending is not null)
        {
            var merged = pending.Merge(command.Name, command.Note, now);
            if (merged.IsFailure)
            {
                return merged;
            }
        }
        else
        {
            var created = AccessRequest.Submit(email.Value, command.Name, command.Note, now);
            if (created.IsFailure)
            {
                return created.Error!;
            }

            requests.Add(created.Value);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Approves a pending request and creates the learner's account with a temporary password.</summary>
public sealed class ApproveAccessRequest(
    IAccessRequestRepository requests,
    AccountProvisioner provisioner,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<Result<IssuedCredentials>> ExecuteAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await requests.FindByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return AppErrors.AccessRequestNotFound;
        }

        var approved = request.Approve(time.GetUtcNow());
        if (approved.IsFailure)
        {
            return approved.Error!;
        }

        // The account is created last: if that fails, nothing has been committed yet.
        var credentials = await provisioner.CreateAsync(request.Email, request.Name, Roles.Learner, cancellationToken);
        if (credentials.IsFailure)
        {
            return credentials;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return credentials;
    }
}

public sealed class RejectAccessRequest(IAccessRequestRepository requests, IUnitOfWork unitOfWork, TimeProvider time)
{
    public async Task<Result> ExecuteAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await requests.FindByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return AppErrors.AccessRequestNotFound;
        }

        var rejected = request.Reject(time.GetUtcNow());
        if (rejected.IsSuccess)
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return rejected;
    }
}

public sealed class ListAccessRequests(IAccessRequestRepository requests)
{
    public Task<IReadOnlyList<AccessRequest>> ExecuteAsync(AccessRequestStatus? status, CancellationToken cancellationToken) =>
        requests.ListAsync(status, cancellationToken);
}
