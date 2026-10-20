using System.Globalization;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Validation;

public enum FieldKind
{
    /// <summary>Non-empty text.</summary>
    Text,

    /// <summary>A whole number, 0 or more.</summary>
    Order,

    /// <summary>A list of text values (may be empty).</summary>
    List,

    /// <summary>One of <see cref="FieldSpec.Choices"/>.</summary>
    Choice,

    /// <summary>A nested map or list of maps; the key's own parser and rule check its shape (spec 006 picker, look-alikes).</summary>
    Structured,
}

public sealed record FieldSpec(string Name, FieldKind Kind, bool Required, IReadOnlyList<string>? Choices = null);

/// <summary>The keys a front-matter (or <c>module.yml</c>) may have, and their types. Unknown keys are errors.</summary>
public sealed class FrontMatterSchema(params FieldSpec[] fields)
{
    public IReadOnlyList<FieldSpec> Fields { get; } = fields;

    public IEnumerable<ContentIssue> Check(YamlMap map, string path)
    {
        ArgumentNullException.ThrowIfNull(map);
        foreach (var field in Fields)
        {
            if (map[field.Name] is not { } entry)
            {
                if (field.Required)
                {
                    yield return new ContentIssue(path, map.Line, IssueCodes.FrontMatterMissingKey, $"'{field.Name}' is required.");
                }

                continue;
            }

            if (TypeProblem(field, entry) is { } problem)
            {
                yield return new ContentIssue(path, entry.Line, IssueCodes.FrontMatterWrongType, $"'{field.Name}' {problem}.");
            }
        }

        var allowed = string.Join(", ", Fields.Select(f => f.Name));
        foreach (var entry in map.Entries.Where(e => Fields.All(f => f.Name != e.Key)))
        {
            yield return new ContentIssue(path, entry.Line, IssueCodes.FrontMatterUnknownKey, $"Unknown key '{entry.Key}'. Allowed: {allowed}.");
        }
    }

    public static bool TryParseOrder(string? value, out int order) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out order);

    private static string? TypeProblem(FieldSpec field, YamlEntry entry) => field.Kind switch
    {
        FieldKind.Text when entry.Kind != YamlValueKind.Scalar || string.IsNullOrWhiteSpace(entry.Scalar) => "must be non-empty text",
        FieldKind.Order when entry.Kind != YamlValueKind.Scalar || !TryParseOrder(entry.Scalar, out _) => "must be a whole number (0 or more)",
        FieldKind.List when entry.Kind != YamlValueKind.List => "must be a list, e.g. [a, b]",
        FieldKind.Choice when entry.Kind != YamlValueKind.Scalar || field.Choices?.Contains(entry.Scalar) != true =>
            $"must be one of: {string.Join(", ", field.Choices ?? [])}",
        FieldKind.Structured when entry.Kind != YamlValueKind.Other => "must be a nested map or a list of maps (see the authoring guide)",
        _ => null,
    };
}
