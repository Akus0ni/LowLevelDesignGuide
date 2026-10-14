using FluentAssertions;
using Lld.Application.Access;
using Lld.Application.Auth;
using Lld.Domain.Access;
using Lld.Domain.Audit;

namespace Lld.Application.Tests;

public sealed class AccessRequestFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Submit_CreatesPendingRequest()
    {
        using var app = new AppHarness();

        var result = await app.Get<SubmitAccessRequest>().ExecuteAsync(new("Learner@Example.com", "Ada", "hi"), Ct);

        result.IsSuccess.Should().BeTrue();
        app.Requests.Items.Should().ContainSingle().Which.Email.Value.Should().Be("learner@example.com");
        app.Audit.Types.Should().Contain(AuditEventType.AccessRequested);
    }

    [Fact]
    public async Task Submit_Twice_MergesIntoOnePendingRequest()
    {
        using var app = new AppHarness();
        var submit = app.Get<SubmitAccessRequest>();

        await submit.ExecuteAsync(new("learner@example.com", "Ada", null), Ct);
        await submit.ExecuteAsync(new("learner@example.com", "Ada Lovelace", "again"), Ct);

        app.Requests.Items.Should().ContainSingle().Which.Name.Should().Be("Ada Lovelace");
        app.Audit.Types.Should().Contain(AuditEventType.AccessRequestMerged);
    }

    [Fact]
    public async Task Submit_ForExistingAccount_SucceedsWithoutCreatingARequest()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();

        var result = await app.Get<SubmitAccessRequest>().ExecuteAsync(new(AppHarness.LearnerEmail, "Ada", null), Ct);

        result.IsSuccess.Should().BeTrue();
        app.Requests.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_RejectsInvalidEmail()
    {
        using var app = new AppHarness();

        var result = await app.Get<SubmitAccessRequest>().ExecuteAsync(new("nope", "Ada", null), Ct);

        result.Error!.Code.Should().Be("validation.email.invalid");
    }

    [Fact]
    public async Task Approve_CreatesAccountWithTemporaryPasswordThatMustBeChanged()
    {
        using var app = new AppHarness();
        await app.Get<SubmitAccessRequest>().ExecuteAsync(new(AppHarness.LearnerEmail, "Ada", null), Ct);
        var request = app.Requests.Items.Single();

        var credentials = await app.Get<ApproveAccessRequest>().ExecuteAsync(request.Id, Ct);

        credentials.Value.TemporaryPassword.Should().Be(FixedTemporaryPasswords.Value);
        request.Status.Should().Be(AccessRequestStatus.Approved);
        var login = await app.LoginAsync(FixedTemporaryPasswords.Value);
        login.Value.User.MustChangePassword.Should().BeTrue();
        app.Audit.Types.Should().Contain([AuditEventType.AccessApproved, AuditEventType.UserCreated]);
    }

    [Fact]
    public async Task Approve_Twice_FailsWithNotPending()
    {
        using var app = new AppHarness();
        await app.Get<SubmitAccessRequest>().ExecuteAsync(new(AppHarness.LearnerEmail, "Ada", null), Ct);
        var id = app.Requests.Items.Single().Id;
        var approve = app.Get<ApproveAccessRequest>();
        await approve.ExecuteAsync(id, Ct);

        var second = await approve.ExecuteAsync(id, Ct);

        second.Error!.Code.Should().Be("access_request.not_pending");
    }

    [Fact]
    public async Task Reject_ThenApprove_Fails()
    {
        using var app = new AppHarness();
        await app.Get<SubmitAccessRequest>().ExecuteAsync(new(AppHarness.LearnerEmail, "Ada", null), Ct);
        var id = app.Requests.Items.Single().Id;

        (await app.Get<RejectAccessRequest>().ExecuteAsync(id, Ct)).IsSuccess.Should().BeTrue();
        var approve = await app.Get<ApproveAccessRequest>().ExecuteAsync(id, Ct);

        approve.Error!.Code.Should().Be("access_request.not_pending");
        (await app.LoginAsync(FixedTemporaryPasswords.Value)).Error!.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Approve_UnknownRequest_FailsWithNotFound()
    {
        using var app = new AppHarness();

        var result = await app.Get<ApproveAccessRequest>().ExecuteAsync(Guid.NewGuid(), Ct);

        result.Error!.Code.Should().Be("access_request.not_found");
    }
}
