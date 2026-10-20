using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>Shared by the <c>content/search.yml</c> rules (spec 009 §3.3): the file, read once per rule.</summary>
internal static class SearchFile
{
    public static (SourceSearchFile File, ParsedSearchVocabulary Vocabulary)? Read(ContentContext context) =>
        context.Set.Search is { Map: { } map } file ? (file, SearchVocabularyFile.Parse(map)) : null;
}

/// <summary><c>search.yml</c> has only <c>synonyms:</c> (a map of word → list) and <c>suggestions:</c> (a list).</summary>
public sealed class SearchVocabularyShapeRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        SearchFile.Read(context) is (var file, var vocabulary)
            ? vocabulary.Problems.Select(p => new ContentIssue(file.Path, p.Line, IssueCodes.SearchShape, p.Message))
            : [];
}

/// <summary>A synonym key is one word: search expands single query words only (spec §3.2).</summary>
public sealed class SearchKeyFormatRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        if (SearchFile.Read(context) is not (var file, var vocabulary))
        {
            yield break;
        }

        foreach (var synonym in vocabulary.Synonyms)
        {
            var words = SearchableContent.Words(synonym.Key);
            if (words.Length != 1 || !string.Equals(words[0], synonym.Key.ToLowerInvariant(), StringComparison.Ordinal))
            {
                yield return new(file.Path, synonym.Line, IssueCodes.SearchKeyFormat,
                    $"Synonym key '{synonym.Key}' must be one word of letters and digits.");
            }
        }
    }
}

/// <summary>Keys are matched without regard to case, so <c>SRP</c> and <c>srp</c> may not both be keys.</summary>
public sealed class SearchKeyUniqueRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        if (SearchFile.Read(context) is not (var file, var vocabulary))
        {
            yield break;
        }

        foreach (var repeat in vocabulary.Synonyms.GroupBy(s => s.Key, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            foreach (var later in repeat.Skip(1))
            {
                yield return new(file.Path, later.Line, IssueCodes.SearchKeyDuplicate,
                    $"Synonym '{later.Key}' is already a key on line {repeat.First().Line} (keys ignore case).");
            }
        }
    }
}

/// <summary>Every alternative is written somewhere in the content; an alternative that finds nothing only adds noise.</summary>
public sealed class SearchAlternativeInContentRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        if (SearchFile.Read(context) is not (var file, var vocabulary))
        {
            yield break;
        }

        var content = context.Searchable;
        foreach (var synonym in vocabulary.Synonyms)
        {
            foreach (var alternative in synonym.Alternatives.Where(a => !content.ContainsPhrase(SearchableContent.Words(a))))
            {
                yield return new(file.Path, synonym.Line, IssueCodes.SearchAlternativeNotInContent,
                    $"'{alternative}' (for {synonym.Key}) isn't written in any lesson or cheat sheet.");
            }
        }
    }
}

/// <summary>A suggestion is offered with one tap, so it must find something.</summary>
public sealed class SearchSuggestionHasResultRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        if (SearchFile.Read(context) is not (var file, var vocabulary))
        {
            yield break;
        }

        var content = context.Searchable;
        foreach (var suggestion in vocabulary.Suggestions.Where(s => !content.FindsAll(SearchableContent.Words(s.Text))))
        {
            yield return new(file.Path, suggestion.Line, IssueCodes.SearchSuggestionNoResult,
                $"Suggestion '{suggestion.Text}' finds nothing: no lesson or cheat sheet section has all its words.");
        }
    }
}
