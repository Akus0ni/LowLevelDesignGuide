using Lld.Api.Auth;
using Lld.Api.Content;
using Lld.Api.Endpoints;
using Lld.Api.Hosting;
using Lld.Application;
using Lld.Infrastructure;
using Lld.Infrastructure.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

// A Windows service starts in C:\Windows\System32: read appsettings*.json from the executable's folder instead.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
});

// Hosted on the owner's PC: secrets (JWT key, status key, ntfy topic) come from DPAPI-protected files.
builder.Configuration.AddProtectedSecrets();

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddLldApplication();
builder.Services.AddLldInfrastructure(builder.Configuration);
builder.Services.AddLldAuth(builder.Configuration);
builder.Services.AddLldSelfHosting(builder.Configuration);
builder.Services.AddLldDevContent(builder.Configuration, builder.Environment);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

app.UseLldSelfHosting();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var api = app.MapGroup("/api");
api.MapInfoEndpoints();
api.MapAccessRequestEndpoints();
api.MapAuthEndpoints();
api.MapProgressEndpoints();
if (app.Environment.IsDevelopment())
{
    // Browser dev mode only (spec 003): devices read their bundled copy; the hosted API serves no content.
    api.MapContentEndpoints();
}

app.MapDefaultEndpoints();

await app.RunAsync();

// Exposed for WebApplicationFactory in Lld.Api.Tests.
public partial class Program;
