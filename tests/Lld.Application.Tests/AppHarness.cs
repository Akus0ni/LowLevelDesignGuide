using Lld.Application.Abstractions;
using Lld.Application.Accounts;
using Lld.Application.Auth;
using Lld.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Lld.Application.Tests;

/// <summary>The real use cases wired to in-memory fakes, one scope per test.</summary>
internal sealed class AppHarness : IDisposable
{
    public const string LearnerEmail = "learner@example.com";
    public const string NewPassword = "a-long-new-password";

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public AppHarness()
    {
        Time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 14, 9, 0, 0, TimeSpan.Zero));
        Accounts = new FakeUserAccounts(Time);

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Time);
        services.AddLldApplication();
        services.AddSingleton(Requests);
        services.AddSingleton<IAccessRequestRepository>(Requests);
        services.AddSingleton<IRefreshTokenRepository>(Tokens);
        services.AddSingleton<IAuditLog>(Audit);
        services.AddSingleton<IUserAccounts>(Accounts);
        services.AddSingleton<ITokenIssuer, FakeTokenIssuer>();
        services.AddSingleton<IRefreshTokenCodec, FakeRefreshTokenCodec>();
        services.AddSingleton<ITemporaryPasswordGenerator, FixedTemporaryPasswords>();
        services.AddSingleton<IPasswordPolicy, MinLengthPolicy>();
        services.AddScoped<IUnitOfWork, FakeUnitOfWork>();

        _provider = services.BuildServiceProvider(validateScopes: true);
        _scope = _provider.CreateScope();
    }

    public FakeTimeProvider Time { get; }

    public FakeUserAccounts Accounts { get; }

    public InMemoryAccessRequests Requests { get; } = new();

    public InMemoryRefreshTokens Tokens { get; } = new();

    public InMemoryAuditLog Audit { get; } = new();

    public T Get<T>()
        where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    /// <summary>Creates a learner who has already replaced the temporary password.</summary>
    public async Task<UserAccount> CreateActiveLearnerAsync(string email = LearnerEmail)
    {
        await Get<CreateUser>().ExecuteAsync(new CreateUserCommand(email, "Learner", Roles.Learner), default);
        var user = (await Accounts.FindByEmailAsync(Domain.Identity.EmailAddress.Create(email).Value, default))!;
        await Accounts.ChangePasswordAsync(user.Id, FixedTemporaryPasswords.Value, NewPassword, default);
        return (await Accounts.FindByIdAsync(user.Id, default))!;
    }

    public Task<Domain.Common.Result<SignInResult>> LoginAsync(string password = NewPassword, string email = LearnerEmail) =>
        Get<Login>().ExecuteAsync(new LoginCommand(email, password), default);

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
