using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// JWT signing key: generated once, stored in this AppHost's user secrets, never committed.
var jwtSigningKey = builder.AddParameter(
    "jwt-signing-key",
    new GenerateParameterDefault { MinLength = 64, Special = false },
    secret: true,
    persist: true);

var api = builder.AddProject<Projects.Lld_Api>("api")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    // Health over http so checks pass without a trusted dev certificate (local + Linux CI).
    .WithHttpHealthCheck("/health", endpointName: "http");

// Optional override of the SQLite file (tests use a temp file); default: %LOCALAPPDATA%/LldGuide/lld.db.
var databasePath = builder.Configuration["Lld:DatabasePath"];
if (!string.IsNullOrWhiteSpace(databasePath))
{
    api.WithEnvironment("Database__Path", databasePath);
}

// Lld:ApiOnly=true starts just the API (integration tests, Linux CI): no npm, no MAUI device.
if (!builder.Configuration.GetValue<bool>("Lld:ApiOnly"))
{
    builder.AddViteApp("web", "../../web")
        .WithReference(api)
        .WaitFor(api);

    var app = builder.AddMauiProject("app", "../Lld.App/Lld.App.csproj");

    // Windows runs on this machine, so it reaches the API on localhost.
    app.AddWindowsDevice("app-windows")
        .WithReference(api);

    // The Android emulator is another "machine": it reaches the API through a public Dev Tunnel.
    // Lld:AndroidEmulatorId is an adb serial (e.g. emulator-5554), not an AVD name; empty = the running emulator.
    // Only the https endpoint is tunnelled: the tunnel's http port answered 502 (spec 001 plan §4).
    var apiTunnel = builder.AddDevTunnel("api-tunnel")
        .WithReference(api.GetEndpoint("https"), allowAnonymous: true);
    var emulatorId = builder.Configuration["Lld:AndroidEmulatorId"];
    app.AddAndroidEmulator("app-android", string.IsNullOrWhiteSpace(emulatorId) ? null : emulatorId)
        .WithReference(api, apiTunnel)
        .WithOtlpDevTunnel();
}

await builder.Build().RunAsync();
