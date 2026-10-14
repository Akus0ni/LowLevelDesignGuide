using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lld.Contracts;

namespace Lld.Api.Tests;

public sealed class InfoEndpointTests(LldApiFactory factory) : IClassFixture<LldApiFactory>
{
    [Fact]
    public async Task GetInfo_ReturnsApiIdentity()
    {
        using var client = factory.CreateClient();

        var info = await client.GetFromJsonAsync<AppInfo>("/api/info", TestContext.Current.CancellationToken);

        info.Should().NotBeNull();
        info!.Name.Should().Be("LLD Guide API");
        info.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+");
        info.Environment.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFound()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
