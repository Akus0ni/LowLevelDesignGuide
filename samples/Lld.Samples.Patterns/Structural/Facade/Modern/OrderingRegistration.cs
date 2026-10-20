using Lld.Samples.Patterns.Structural.Facade.After;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Samples.Patterns.Structural.Facade.Modern;

public static class OrderingRegistration
{
    /// <summary>
    /// A facade for start-up too: one <c>AddOrdering()</c> call registers the whole subsystem, the way
    /// <c>AddDbContext</c> or <c>AddAuthentication</c> hide dozens of registrations.
    /// </summary>
    public static IServiceCollection AddOrdering(this IServiceCollection services) =>
        services
            .AddSingleton<Inventory>()
            .AddSingleton<Payments>()
            .AddSingleton<Shipping>()
            .AddSingleton<Mailer>()
            .AddSingleton<OrderFacade>();
}
