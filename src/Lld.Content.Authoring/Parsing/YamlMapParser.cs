using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Lld.Content.Authoring.Parsing;

public sealed record YamlParseResult(YamlMap? Map, ContentIssue? Issue);

/// <summary>Parses YAML into a <see cref="YamlMap"/>; syntax errors become issues with the file line, not exceptions.</summary>
public static class YamlMapParser
{
    public const string InvalidCode = "yaml.invalid";
    public const string NotMappingCode = "yaml.not_mapping";

    /// <param name="firstLine">1-based line in the file where <paramref name="yaml"/> starts.</param>
    public static YamlParseResult Parse(string yaml, string path, int firstLine)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlException ex)
        {
            var line = firstLine + (int)ex.Start.Line - 1;
            return new(null, new ContentIssue(path, line, InvalidCode, $"YAML can't be read: {ex.Message}"));
        }
        catch (InvalidOperationException)
        {
            // YamlDotNet's scanner throws this (without a position) for some malformed input, e.g. an unclosed "[".
            return new(null, new ContentIssue(path, firstLine, InvalidCode, "YAML can't be read: check brackets and quotes."));
        }

        if (stream.Documents.Count == 0)
        {
            return new(new YamlMap([], firstLine), null);
        }

        if (stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            return new(null, new ContentIssue(path, firstLine, NotMappingCode, "Expected 'key: value' pairs."));
        }

        var entries = root.Children.Select(pair => ToEntry(pair.Key, pair.Value, firstLine)).ToList();
        return new(new YamlMap(entries, firstLine), null);
    }

    private static YamlEntry ToEntry(YamlNode key, YamlNode value, int firstLine)
    {
        var name = key is YamlScalarNode scalarKey ? scalarKey.Value ?? string.Empty : key.ToString();
        var line = firstLine + (int)key.Start.Line - 1;
        return value switch
        {
            YamlScalarNode scalar => new(name, line, YamlValueKind.Scalar, scalar.Value ?? string.Empty, []),
            YamlSequenceNode list when list.Children.All(item => item is YamlScalarNode) =>
                new(name, line, YamlValueKind.List, null, [.. list.Children.Cast<YamlScalarNode>().Select(item => item.Value ?? string.Empty)]),
            _ => new(name, line, YamlValueKind.Other, null, []),
        };
    }
}
