using System.Globalization;
using System.Text;

namespace Lld.Search.Query;

/// <summary>
/// Turns whatever the learner typed into words and phrases (spec §3.2). A word is a run of letters and digits, as the
/// FTS5 <c>unicode61</c> tokenizer sees it; everything else separates words, so operators such as <c>AND</c>,
/// <c>NEAR</c>, <c>*</c>, <c>(</c> or <c>:</c> can only ever be searched as text. Double quotes group a phrase; an
/// unclosed quote runs to the end.
/// </summary>
public static class QueryParser
{
    /// <summary>Longer input is cut here (spec §3.2).</summary>
    public const int MaxLength = 100;

    /// <summary>Terms after this many are ignored, which keeps every query cheap.</summary>
    public const int MaxTerms = 8;

    private const char Quote = '"';

    public static SearchQuery Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return SearchQuery.Empty;
        }

        var input = text.Length > MaxLength ? text[..MaxLength] : text;
        var scanner = new Scanner();
        foreach (var rune in input.EnumerateRunes())
        {
            scanner.Read(rune);
        }

        return scanner.Finish();
    }

    /// <summary>Letters, digits and combining marks, as <c>unicode61</c> treats them.</summary>
    private static bool IsWordRune(Rune rune) =>
        Rune.GetUnicodeCategory(rune) switch
        {
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter
                or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber
                or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark => true,
            _ => false,
        };

    /// <summary>One left-to-right pass: collects words, groups quoted ones, remembers whether the input ends mid-word.</summary>
    private sealed class Scanner
    {
        private readonly List<QueryTerm> _terms = [];
        private readonly List<string> _phrase = [];
        private readonly StringBuilder _word = new();
        private bool _inPhrase;
        private bool _endsMidWord;

        public void Read(Rune rune)
        {
            if (IsWordRune(rune))
            {
                _word.Append(rune.ToString());
                _endsMidWord = !_inPhrase;
                return;
            }

            EndWord();
            _endsMidWord = false;
            if (rune.Value == Quote)
            {
                if (_inPhrase)
                {
                    EndPhrase();
                }

                _inPhrase = !_inPhrase;
            }
        }

        public SearchQuery Finish()
        {
            EndWord();
            EndPhrase();
            if (_terms.Count == 0)
            {
                return SearchQuery.Empty;
            }

            var cut = _terms.Count > MaxTerms;
            List<QueryTerm> kept = cut ? _terms[..MaxTerms] : _terms;
            return new SearchQuery(kept, LastIsPrefix: _endsMidWord && !cut && !kept[^1].IsPhrase);
        }

        private void EndWord()
        {
            if (_word.Length == 0)
            {
                return;
            }

            var word = _word.ToString().ToLower(CultureInfo.InvariantCulture);
            _word.Clear();
            if (_inPhrase)
            {
                _phrase.Add(word);
            }
            else
            {
                _terms.Add(QueryTerm.Word(word));
            }
        }

        private void EndPhrase()
        {
            if (_phrase.Count == 1)
            {
                _terms.Add(QueryTerm.Word(_phrase[0]));
            }
            else if (_phrase.Count > 1)
            {
                _terms.Add(QueryTerm.Phrase([.. _phrase]));
            }

            _phrase.Clear();
        }
    }
}
