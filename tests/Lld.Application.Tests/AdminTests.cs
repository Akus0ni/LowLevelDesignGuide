using FluentAssertions;
using Lld.Application.Accounts;
using Lld.Application.Auth;
using Lld.Domain.Audit;

namespace Lld.Application.Tests;

public sealed class AdminTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateUser_AsAdmin_UsesEmailAsDefaultName()
    {
        using var app = new AppHarness();

        var created = await app.Get<CreateUser>().ExecuteAsync(new CreateUserCommand("owner@example.com", null, "Admin"), Ct);

        created.Value.Email.Should().Be("owner@example.com");
        var user = (await app.Get<ListUsers>().ExecuteAsync(Ct)).Single();
        user.Roles.Should().Equal("Admin");
        user.DisplayName.Should().Be("owner@example.com");
    }

    [Fact]
    public async Task CreateUser_RejectsUnknownRoleAndDuplicates()
    {
        using var app = new AppHarness();
        var create = app.Get<CreateUser>();

        (await create.ExecuteAsync(new CreateUserCommand("a@example.com", null, "Root"), Ct)).Error!.Code
            .Should().Be("validation.role.unknown");
        await create.ExecuteAsync(new CreateUserCommand("a@example.com", null, "Learner"), Ct);
        (await create.ExecuteAsync(new CreateUserCommand("A@example.com", null, "Learner"), Ct)).Error!.Code
            .Should().Be("user.already_exists");
    }

    [Fact]
    public async Task ResetPassword_IssuesTemporaryPassword_AndEndsSessions()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        var session = (await app.LoginAsync()).Value;

        var reset = await app.Get<ResetPassword>().ExecuteAsync(AppHarness.LearnerEmail, Ct);

        reset.Value.TemporaryPassword.Should().Be(FixedTemporaryPasswords.Value);
        (await app.Get<RefreshSession>().ExecuteAsync(session.RefreshToken, Ct)).IsFailure.Should().BeTrue();
        (await app.LoginAsync(FixedTemporaryPasswords.Value)).Value.User.MustChangePassword.Should().BeTrue();
        app.Audit.Types.Should().Contain(AuditEventType.PasswordReset);
    }

    [Fact]
    public async Task DisableUser_BlocksSignInAndRefresh()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        var session = (await app.LoginAsync()).Value;

        (await app.Get<DisableUser>().ExecuteAsync(AppHarness.LearnerEmail, Ct)).IsSuccess.Should().BeTrue();

        (await app.LoginAsync()).Error!.Code.Should().Be("auth.invalid_credentials");
        (await app.Get<RefreshSession>().ExecuteAsync(session.RefreshToken, Ct)).IsFailure.Should().BeTrue();
        app.Audit.Types.Should().Contain(AuditEventType.UserDisabled);
    }

    [Fact]
    public async Task ResetPassword_UnknownEmail_FailsWithNotFound()
    {
        using var app = new AppHarness();

        (await app.Get<ResetPassword>().ExecuteAsync("ghost@example.com", Ct)).Error!.Code.Should().Be("user.not_found");
    }

    [Fact]
    public async Task ListAuditEntries_ReturnsNewestFirst()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        app.Time.Advance(TimeSpan.FromMinutes(1));
        await app.LoginAsync();

        var entries = await app.Get<ListAuditEntries>().ExecuteAsync(1, Ct);

        entries.Should().ContainSingle().Which.Type.Should().Be(AuditEventType.LoginSucceeded);
    }
}
