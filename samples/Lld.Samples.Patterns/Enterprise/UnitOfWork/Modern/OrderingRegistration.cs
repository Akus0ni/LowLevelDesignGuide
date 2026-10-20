using Lld.Samples.Patterns.Enterprise.UnitOfWork.After;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Samples.Patterns.Enterprise.UnitOfWork.Modern;

/// <summary>
/// In ASP.NET Core one unit of work per request comes from a <b>scoped</b> registration: every repository in the same
/// request gets the same one (EF Core's <c>AddDbContext</c> registers the <c>DbContext</c>, a unit of work, this way).
/// </summary>
public static class OrderingRegistration
{
    public static IServiceCollection AddOrdering(this IServiceCollection services) =>
        services
            .AddSingleton<Store>()
            .AddScoped<After.UnitOfWork>()
            .AddScoped<OrderRepository>()
            .AddScoped<StockRepository>()
            .AddScoped<PlaceOrder>();
}
