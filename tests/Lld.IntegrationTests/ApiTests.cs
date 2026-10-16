using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Lld.Contracts;

namespace Lld.IntegrationTests;

/// <summary>Starts the real AppHost (API only) and calls the API over HTTP.</summary>
public sealed class ApiTests : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);
    private DistributedApplication? _app;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Lld_AppHost>(
            ["--Lld:ApiOnly=true", $"--Lld:DatabasePath={Path.Combine(Path.GetTempPath(), "lld-it", $"{Guid.NewGuid():N}.db")}"],
            cancellationToken);

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var client = _app!.CreateHttpClient("api", "http");

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Info_ReturnsApiIdentity()
    {
        using var client = _app!.CreateHttpClient("api", "http");

        var info = await client.GetFromJsonAsync<AppInfo>("/api/info", TestContext.Current.CancellationToken);

        info.Should().NotBeNull();
        info!.Name.Should().Be("LLD Guide API");
    }

    [Fact]
    public async Task Content_IsServedFromTheRepository_InDevelopment()
    {
        using var client = _app!.CreateHttpClient("api", "http");

        var modules = await client.GetFromJsonAsync<List<ModuleSummary>>("/api/content/modules", TestContext.Current.CancellationToken);
        var auth = await client.GetFromJsonAsync<LessonDetail>("/api/content/lessons/case-study-auth", TestContext.Current.CancellationToken);

        modules!.Select(m => m.Slug).Should().StartWith("start").And.Contain("case-studies");
        auth!.Outline.Should().NotBeEmpty();
    }
}
