using Lld.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGroup("/api").MapInfoEndpoints();
app.MapDefaultEndpoints();

await app.RunAsync();

// Exposed for WebApplicationFactory in Lld.Api.Tests.
public partial class Program;
