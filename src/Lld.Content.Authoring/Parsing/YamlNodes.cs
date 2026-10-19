using YamlDotNet.RepresentationModel;

namespace Lld.Content.Authoring.Parsing;

/// <summary>Small lookups over YamlDotNet maps, for the nested <c>module.yml</c> values (picker, look-alikes).</summary>
internal static class YamlNodes
{
    public static YamlNode? Child(YamlMappingNode map, string key) =>
        map.Children.FirstOrDefault(pair => pair.Key is YamlScalarNode { Value: { } name } && name == key).Value;

    /// <summary>The trimmed text of a scalar child, or <c>null</c> when missing, empty or not a scalar.</summary>
    public static string? Scalar(YamlMappingNode map, string key) =>
        Child(map, key) is YamlScalarNode { Value: { } value } && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    /// <summary>A list of scalars, or a single scalar as a one-item list; empty when missing.</summary>
    public static IReadOnlyList<string> Items(YamlMappingNode map, string key) => Child(map, key) switch
    {
        YamlSequenceNode list => [.. list.Children.OfType<YamlScalarNode>().Select(i => i.Value?.Trim() ?? string.Empty)],
        YamlScalarNode { Value: { } value } when !string.IsNullOrWhiteSpace(value) => [value.Trim()],
        _ => [],
    };

    public static IEnumerable<string> Keys(YamlMappingNode map) =>
        map.Children.Keys.OfType<YamlScalarNode>().Select(k => k.Value ?? string.Empty);
}
