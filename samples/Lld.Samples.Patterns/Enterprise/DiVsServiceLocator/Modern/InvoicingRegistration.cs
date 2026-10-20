using Lld.Samples.Patterns.Enterprise.DiVsServiceLocator.After;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Samples.Patterns.Enterprise.DiVsServiceLocator.Modern;

/// <summary>
/// The container is used in one place only, the composition root (<c>Program.cs</c>): register here, resolve the
/// top-level object there, and let constructors do the rest. Asking <c>IServiceProvider</c> from inside business code
/// would be a service locator again.
/// </summary>
public static class InvoicingRegistration
{
    public static IServiceCollection AddInvoicing<TMailer>(this IServiceCollection services)
        where TMailer : class, IMailer =>
        services.AddSingleton<IMailer, TMailer>().AddTransient<InvoiceSender>();
}
