using System.Text;
using FluentAssertions;

namespace Lld.Content.Tests;

public sealed class ContentPackJsonTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RoundTrip_KeepsEverything()
    {
        var pack = TestPacks.Sample();

        var copy = await ContentPackJson.ReadAsync(await WriteAsync(pack), Ct);

        copy.Should().BeEquivalentTo(pack, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Write_UsesCamelCase_AndWritesMissingSourceAsNull()
    {
        using var stream = await WriteAsync(TestPacks.Sample());
        var json = Encoding.UTF8.GetString(stream.ToArray());

        json.Should().StartWith("{\"format\":1,\"version\":\"v-sample\",\"modules\":[");
        json.Should().Contain("\"source\":\"self-referential\"");
        json.Should().Contain("\"source\":null");
    }

    [Fact]
    public async Task Read_UnknownFormat_IsRejected()
    {
        using var stream = await WriteAsync(TestPacks.Sample() with { Format = ContentPack.CurrentFormat + 1 });

        var read = () => ContentPackJson.ReadAsync(stream, Ct);

        await read.Should().ThrowAsync<InvalidDataException>().WithMessage("*format 2 is not supported*");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"format\":1}")]
    public async Task Read_MalformedOrIncomplete_IsRejected(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var read = () => ContentPackJson.ReadAsync(stream, Ct);

        await read.Should().ThrowAsync<InvalidDataException>();
    }

    private static async Task<MemoryStream> WriteAsync(ContentPack pack)
    {
        var stream = new MemoryStream();
        await ContentPackJson.WriteAsync(pack, stream, Ct);
        stream.Position = 0;
        return stream;
    }
}
