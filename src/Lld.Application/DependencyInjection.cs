using Lld.Application.Abstractions;
using Lld.Application.Access;
using Lld.Application.Accounts;
using Lld.Application.Audit;
using Lld.Application.Auth;
using Lld.Application.Common;
using Lld.Application.Progress;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lld.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use cases. Infrastructure must register the ports.</summary>
    public static IServiceCollection AddLldApplication(this IServiceCollection services, Action<AuthOptions>? configure = null)
    {
        var options = new AuthOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<DomainEventCollector>();
        services.AddScoped<IDomainEvents>(sp => sp.GetRequiredService<DomainEventCollector>());
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<IDomainEventObserver, AuditTrail>();

        services.AddScoped<SessionManager>();
        services.AddScoped<AccountProvisioner>();

        // Use cases: one class per action.
        services.AddScoped<SubmitAccessRequest>();
        services.AddScoped<ApproveAccessRequest>();
        services.AddScoped<RejectAccessRequest>();
        services.AddScoped<ListAccessRequests>();
        services.AddScoped<CreateUser>();
        services.AddScoped<ResetPassword>();
        services.AddScoped<DisableUser>();
        services.AddScoped<ListUsers>();
        services.AddScoped<ListAuditEntries>();
        services.AddScoped<Login>();
        services.AddScoped<RefreshSession>();
        services.AddScoped<Logout>();
        services.AddScoped<LogoutAllDevices>();
        services.AddScoped<ChangePassword>();
        services.AddScoped<GetCurrentUser>();

        services.AddSingleton<ActivityDayPolicy>();
        services.AddScoped<GetProgress>();
        services.AddScoped<SaveLessonProgress>();
        services.AddScoped<RecordActivityDay>();
        services.AddScoped<AdoptGuestProgress>();
        return services;
    }
}
