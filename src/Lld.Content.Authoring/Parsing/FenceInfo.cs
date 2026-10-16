using System.Text;

namespace Lld.Content.Authoring.Parsing;

/// <summary>One <c>key="value"</c> pair of a code fence's info string.</summary>
public sealed record FenceParameter(string Name, string Value);

/// <summary>
/// A code fence's info string (spec 004 §3.3–3.4): the language, then flags and <c>key="value"</c> attributes, e.g.
/// <c>csharp before title="Before" snippet="samples/X.cs#Type"</c>. Parsing never throws: syntax problems are listed in
/// <see cref="Problems"/> for the validation rules to report. Which flags and attributes are allowed is the rules' call.
/// </summary>
public sealed class FenceInfo
{
    public const string BeforeFlag = "before";
    public const string AfterFlag = "after";
    public const string SnippetAttribute = "snippet";
    public const string TitleAttribute = "title";
    public const string MarkAttribute = "mark";

    private const char Quote = '"';
    private const char Equal = '=';

    private FenceInfo(string language, IReadOnlyList<string> flags, IReadOnlyList<FenceParameter> attributes, IReadOnlyList<string> problems)
    {
        Language = language;
        Flags = flags;
        Attributes = attributes;
        Problems = problems;
    }

    /// <summary>The first word, e.g. <c>csharp</c>; empty when the fence has no info string.</summary>
    public string Language { get; }

    public IReadOnlyList<string> Flags { get; }

    /// <summary>In the order written.</summary>
    public IReadOnlyList<FenceParameter> Attributes { get; }

    public IReadOnlyList<string> Problems { get; }

    public bool IsBefore => HasFlag(BeforeFlag);

    public bool IsAfter => HasFlag(AfterFlag);

    /// <summary>The value of an attribute, or <c>null</c> when it isn't there.</summary>
    public string? this[string name] => Attributes.FirstOrDefault(a => a.Name == name)?.Value;

    public bool HasFlag(string flag) => Flags.Contains(flag, StringComparer.Ordinal);

    public FenceInfo Without(string attribute) =>
        new(Language, Flags, [.. Attributes.Where(a => a.Name != attribute)], Problems);

    /// <summary>Sets an attribute, replacing it where it was written or adding it at the end.</summary>
    public FenceInfo With(string attribute, string value)
    {
        var replaced = Attributes.Any(a => a.Name == attribute)
            ? Attributes.Select(a => a.Name == attribute ? a with { Value = value } : a)
            : Attributes.Append(new FenceParameter(attribute, value));
        return new(Language, Flags, [.. replaced], Problems);
    }

    /// <summary>The info string again: language, flags, then attributes, separated by single spaces.</summary>
    public override string ToString() =>
        string.Join(' ', new[] { Language }
            .Concat(Flags)
            .Concat(Attributes.Select(a => $"{a.Name}{Equal}{Quote}{a.Value}{Quote}"))
            .Where(part => part.Length > 0));

    public static FenceInfo Parse(string? info)
    {
        var flags = new List<string>();
        var attributes = new List<FenceParameter>();
        var problems = new List<string>();
        var language = string.Empty;
        var reader = new InfoReader(info ?? string.Empty);
        var first = true;
        while (reader.NextToken() is { } token)
        {
            if (token.Problem is not null)
            {
                problems.Add(token.Problem);
            }
            else if (token.Value is null && first)
            {
                language = token.Name;
            }
            else if (token.Value is null)
            {
                flags.Add(token.Name);
            }
            else
            {
                attributes.Add(new FenceParameter(token.Name, token.Value));
            }

            first = false;
        }

        return new FenceInfo(language, flags, attributes, problems);
    }

    private sealed record Token(string Name, string? Value, string? Problem);

    /// <summary>Splits on spaces, except inside <c>"..."</c>.</summary>
    private sealed class InfoReader(string text)
    {
        private int _index;

        public Token? NextToken()
        {
            while (_index < text.Length && char.IsWhiteSpace(text[_index]))
            {
                _index++;
            }

            if (_index >= text.Length)
            {
                return null;
            }

            var name = ReadWhile(c => !char.IsWhiteSpace(c) && c != Equal);
            if (_index >= text.Length || text[_index] != Equal)
            {
                return new Token(name, null, null);
            }

            _index++;
            return _index < text.Length && text[_index] == Quote ? Quoted(name) : new Token(name, ReadWhile(c => !char.IsWhiteSpace(c)), null);
        }

        private Token Quoted(string name)
        {
            var close = text.IndexOf(Quote, _index + 1);
            if (close < 0)
            {
                _index = text.Length;
                return new Token(name, null, $"'{name}' has no closing quote.");
            }

            var value = text[(_index + 1)..close];
            _index = close + 1;
            return new Token(name, value, null);
        }

        private string ReadWhile(Func<char, bool> accept)
        {
            var value = new StringBuilder();
            while (_index < text.Length && accept(text[_index]))
            {
                value.Append(text[_index++]);
            }

            return value.ToString();
        }
    }
}
