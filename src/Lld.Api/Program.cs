using Lld.Api.Auth;
using Lld.Api.Endpoints;
using Lld.Api.Hosting;
using Lld.Application;
using Lld.Infrastructure;
using Lld.Infrastructure.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Hosted on the owner's PC: secrets (JWT key, status key, ntfy topic) come from DPAPI-protected files.
builder.Configuration.AddProtectedSecrets();

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddLldApplication();
builder.Services.AddLldInfrastructure(builder.Configuration);
builder.Services.AddLldAuth(builder.Configuration);
builder.Services.AddLldSelfHosting(builder.Configuration);

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
app.MapDefaultEndpoints();

await app.RunAsync();

// Exposed for WebApplicationFactory in Lld.Api.Tests.
public partial class Program;
