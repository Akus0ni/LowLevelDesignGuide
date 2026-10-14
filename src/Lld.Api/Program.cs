using Lld.Api.Auth;
using Lld.Api.Endpoints;
using Lld.Application;
using Lld.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddLldApplication();
builder.Services.AddLldInfrastructure(builder.Configuration);
builder.Services.AddLldAuth(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

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
