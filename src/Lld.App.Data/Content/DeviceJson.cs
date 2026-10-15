using System.Text.Json;
using System.Text.Json.Serialization;
using Lld.Contracts;

namespace Lld.App.Data.Content;

/// <summary>Tags and outlines are stored as JSON columns; source-generated so it survives Android trimming.</summary>
internal static class DeviceJson
{
    public static string Tags(IReadOnlyList<string> tags) =>
        JsonSerializer.Serialize([.. tags], DeviceJsonContext.Default.StringArray);

    public static IReadOnlyList<string> Tags(string json) =>
        JsonSerializer.Deserialize(json, DeviceJsonContext.Default.StringArray) ?? [];

    public static string Outline(IReadOnlyList<OutlineEntry> outline) =>
        JsonSerializer.Serialize([.. outline], DeviceJsonContext.Default.OutlineEntryArray);

    public static IReadOnlyList<OutlineEntry> Outline(string json) =>
        JsonSerializer.Deserialize(json, DeviceJsonContext.Default.OutlineEntryArray) ?? [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(OutlineEntry[]))]
internal sealed partial class DeviceJsonContext : JsonSerializerContext;
