using Lld.Samples.Patterns.Structural.Bridge.After;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Samples.Patterns.Structural.Bridge.Modern;

/// <summary>
/// Modern .NET: the container builds the bridge. Channels are keyed services, and each alert asks for the one it
/// needs, so pairing a kind with a channel is configuration, not a new class.
/// </summary>
public static class AlertRegistration
{
    public static IServiceCollection AddAlerts(this IServiceCollection services) =>
        services
            .AddKeyedSingleton<IChannel, EmailChannel>("email")
            .AddKeyedSingleton<IChannel, SmsChannel>("sms")
            .AddSingleton(sp => new UrgentAlert(sp.GetRequiredKeyedService<IChannel>("sms")))
            .AddSingleton(sp => new ReminderAlert(sp.GetRequiredKeyedService<IChannel>("email")));
}
