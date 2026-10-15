using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lld.Content;

/// <summary>Reads and writes <c>content-pack.json</c> with source-generated JSON, so it survives Android trimming.</summary>
public static class ContentPackJson
{
    public static async Task WriteAsync(ContentPack pack, Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pack);
        await JsonSerializer.SerializeAsync(destination, pack, ContentPackJsonContext.Default.ContentPack, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <exception cref="InvalidDataException">The file is empty, malformed or in a format this build doesn't know.</exception>
    public static async Task<ContentPack> ReadAsync(Stream source, CancellationToken cancellationToken)
    {
        ContentPack? pack;
        try
        {
            pack = await JsonSerializer.DeserializeAsync(source, ContentPackJsonContext.Default.ContentPack, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The content pack is not valid JSON.", ex);
        }

        if (pack is null)
        {
            throw new InvalidDataException("The content pack is empty.");
        }

        if (pack.Format != ContentPack.CurrentFormat)
        {
            throw new InvalidDataException(
                $"Content pack format {pack.Format} is not supported (expected {ContentPack.CurrentFormat}).");
        }

        return pack;
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(ContentPack))]
internal sealed partial class ContentPackJsonContext : JsonSerializerContext;
