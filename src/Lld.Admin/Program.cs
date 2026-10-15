using Lld.Admin;
using Lld.Application;
using Lld.Infrastructure;
using Lld.Infrastructure.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Admin console: runs on the owner's PC against the same SQLite file as the API.
// There are deliberately no admin HTTP endpoints. Usage: lld-admin --help
// `dotnet run` uses the Development profile (dev database); the installed copy runs as Production and reads
// appsettings.Production.json + the protected secrets, so it manages the hosted API's database.
var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddProtectedSecrets();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddLldApplication();
builder.Services.AddLldInfrastructure(builder.Configuration);
builder.Services.AddLldHosting(builder.Configuration);

using var host = builder.Build();
await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

return await AdminCli.RunAsync(args, host.Services, Console.Out, Console.Error);
