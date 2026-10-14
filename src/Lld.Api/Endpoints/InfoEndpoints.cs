using System.Reflection;
using Lld.Contracts;

namespace Lld.Api.Endpoints;

internal static class InfoEndpoints
{
    private const string ApplicationName = "LLD Guide API";

    public static IEndpointRouteBuilder MapInfoEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/info", GetInfo).WithName("GetInfo");
        return routes;
    }

    private static AppInfo GetInfo(IHostEnvironment environment) =>
        new(ApplicationName, ResolveVersion(), environment.EnvironmentName);

    private static string ResolveVersion()
    {
        var informational = typeof(InfoEndpoints).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        // Strip the "+<commit sha>" suffix the SDK appends to informational versions.
        return informational?.Split('+')[0] ?? "0.0.0";
    }
}
