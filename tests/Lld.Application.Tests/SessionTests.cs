using FluentAssertions;
using Lld.Application.Accounts;
using Lld.Application.Auth;
using Lld.Domain.Audit;
using Lld.Domain.Sessions;

namespace Lld.Application.Tests;

public sealed class SessionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_WithCorrectPassword_StartsSession()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();

        var result = await app.LoginAsync();

        result.Value.AccessToken.Value.Should().NotBeEmpty();
        result.Value.RefreshToken.Should().NotBeEmpty();
        app.Tokens.Items.Should().ContainSingle(t => t.IsActive(app.Time.GetUtcNow()));
        app.Audit.Types.Should().Contain(AuditEventType.LoginSucceeded);
    }

    [Theory]
    [InlineData(AppHarness.LearnerEmail, "wrong-password-123")]
    [InlineData("nobody@example.com", AppHarness.NewPassword)]
    [InlineData("not-an-email", AppHarness.NewPassword)]
    public async Task Login_Failures_AreGeneric(string email, string password)
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();

        var result = await app.LoginAsync(password, email);

        result.Error!.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Login_FiveFailures_LockForFifteenMinutes()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();

        for (var attempt = 1; attempt < 5; attempt++)
        {
            (await app.LoginAsync("wrong-password-123")).Error!.Code.Should().Be("auth.invalid_credentials");
        }

        (await app.LoginAsync("wrong-password-123")).Error!.Code.Should().Be("auth.locked_out");
        (await app.LoginAsync()).Error!.Code.Should().Be("auth.locked_out");
        app.Audit.Types.Should().Contain(AuditEventType.LockedOut);

        app.Time.Advance(TimeSpan.FromMinutes(15));
        (await app.LoginAsync()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_RotatesTheToken()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        var first = (await app.LoginAsync()).Value;

        var second = await app.Get<RefreshSession>().ExecuteAsync(first.RefreshToken, Ct);

        second.Value.RefreshToken.Should().NotBe(first.RefreshToken);
        var tokens = app.Tokens.Items;
        tokens.Should().HaveCount(2);
        tokens[0].RevokedReason.Should().Be(RevocationReason.Rotated);
        tokens[1].FamilyId.Should().Be(tokens[0].FamilyId);
    }

    [Fact]
    public async Task Refresh_ReplayingARotatedToken_RevokesTheWholeFamily()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        var stolen = (await app.LoginAsync()).Value.RefreshToken;
        var legitimate = (await app.Get<RefreshSession>().ExecuteAsync(stolen, Ct)).Value.RefreshToken;

        var replay = await app.Get<RefreshSession>().ExecuteAsync(stolen, Ct);

        replay.Error!.Code.Should().Be("auth.refresh_token_reused");
        (await app.Get<RefreshSession>().ExecuteAsync(legitimate, Ct)).IsFailure.Should().BeTrue();
        app.Audit.Types.Should().Contain(AuditEventType.RefreshTokenReuseDetected);
    }

    [Fact]
    public async Task Refresh_AfterExpiry_Fails()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        var session = (await app.LoginAsync()).Value;

        app.Time.Advance(TimeSpan.FromDays(30));

        (await app.Get<RefreshSession>().ExecuteAsync(session.RefreshToken, Ct)).Error!.Code
            .Should().Be("auth.session_invalid");
    }

    [Fact]
    public async Task Logout_EndsOnlyThatSession()
    {
        using var app = new AppHarness();
        await app.CreateActiveLearnerAsync();
        var phone = (await app.LoginAsync()).Value;
        var laptop = (await app.LoginAsync()).Value;

        (await app.Get<Logout>().ExecuteAsync(phone.RefreshToken, Ct)).IsSuccess.Should().BeTrue();

        (await app.Get<RefreshSession>().ExecuteAsync(phone.RefreshToken, Ct)).IsFailure.Should().BeTrue();
        (await app.Get<RefreshSession>().ExecuteAsync(laptop.RefreshToken, Ct)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WithUnknownToken_StillSucceeds()
    {
        using var app = new AppHarness();

        (await app.Get<Logout>().ExecuteAsync("garbage", Ct)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAllDevices_EndsEverySession()
    {
        using var app = new AppHarness();
        var user = await app.CreateActiveLearnerAsync();
        var phone = (await app.LoginAsync()).Value;
        var laptop = (await app.LoginAsync()).Value;

        var revoked = await app.Get<LogoutAllDevices>().ExecuteAsync(user.Id, Ct);

        revoked.Value.Should().Be(2);
        (await app.Get<RefreshSession>().ExecuteAsync(phone.RefreshToken, Ct)).IsFailure.Should().BeTrue();
        (await app.Get<RefreshSession>().ExecuteAsync(laptop.RefreshToken, Ct)).IsFailure.Should().BeTrue();
        app.Audit.Types.Should().Contain(AuditEventType.SignedOutEverywhere);
    }

    [Fact]
    public async Task ChangePassword_ClearsMustChange_EndsOtherSessions_AndStartsANewOne()
    {
        using var app = new AppHarness();
        await app.Get<CreateUser>().ExecuteAsync(new CreateUserCommand(AppHarness.LearnerEmail, "Ada", "Learner"), Ct);
        var other = (await app.LoginAsync(FixedTemporaryPasswords.Value)).Value;
        var current = (await app.LoginAsync(FixedTemporaryPasswords.Value)).Value;

        var changed = await app.Get<ChangePassword>().ExecuteAsync(
            new ChangePasswordCommand(current.User.Id, FixedTemporaryPasswords.Value, AppHarness.NewPassword), Ct);

        changed.Value.User.MustChangePassword.Should().BeFalse();
        (await app.Get<RefreshSession>().ExecuteAsync(other.RefreshToken, Ct)).IsFailure.Should().BeTrue();
        (await app.Get<RefreshSession>().ExecuteAsync(changed.Value.RefreshToken, Ct)).IsSuccess.Should().BeTrue();
        app.Audit.Types.Should().Contain(AuditEventType.PasswordChanged);
    }

    [Fact]
    public async Task ChangePassword_EnforcesThePolicyAndTheCurrentPassword()
    {
        using var app = new AppHarness();
        var user = await app.CreateActiveLearnerAsync();
        var change = app.Get<ChangePassword>();

        (await change.ExecuteAsync(new(user.Id, AppHarness.NewPassword, "short"), Ct)).Error!.Code
            .Should().Be("validation.password.too_short");
        (await change.ExecuteAsync(new(user.Id, "not-the-current-one", "another-long-password"), Ct)).Error!.Code
            .Should().Be("auth.current_password_incorrect");
    }
}
