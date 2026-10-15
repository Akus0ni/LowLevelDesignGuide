using Lld.Admin;
using Lld.Application;
using Lld.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Admin console: runs on the owner's PC against the same SQLite file as the API.
// There are deliberately no admin HTTP endpoints. Usage: lld-admin --help
var builder = Host.CreateApplicationBuilder();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddLldApplication();
builder.Services.AddLldInfrastructure(builder.Configuration);

using var host = builder.Build();
await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

return await AdminCli.RunAsync(args, host.Services, Console.Out, Console.Error);
