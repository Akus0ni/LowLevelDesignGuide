using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Lld_Api>("api")
    // Health over http so checks pass without a trusted dev certificate (local + Linux CI).
    .WithHttpHealthCheck("/health", endpointName: "http");

// Lld:ApiOnly=true starts just the API (integration tests, Linux CI): no npm, no MAUI device.
if (!builder.Configuration.GetValue<bool>("Lld:ApiOnly"))
{
    builder.AddViteApp("web", "../../web")
        .WithReference(api)
        .WaitFor(api);

    var app = builder.AddMauiProject("app", "../Lld.App/Lld.App.csproj");

    app.AddWindowsDevice("app-windows");

    // The app does not call the API until spec 001, so only telemetry needs a Dev Tunnel for now.
    app.AddAndroidEmulator("app-android", builder.Configuration["Lld:AndroidEmulatorId"])
        .WithOtlpDevTunnel();
}

await builder.Build().RunAsync();
