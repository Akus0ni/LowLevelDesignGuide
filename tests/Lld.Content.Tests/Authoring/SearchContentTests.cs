using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Tests.Authoring;

/// <summary>
/// Spec 009 §3.1, §3.3: the search text the packer extracts per section, and <c>content/search.yml</c> with its rules.
/// Each rule test breaks one thing in a valid file and expects exactly that issue (path, line, code).
/// </summary>
public sealed class SearchContentTests
{
    private const string SearchFile = "content/search.yml";

    private const string ValidVocabulary = """
        synonyms:
          RT: [refresh tokens]
          rotate: [token rotation, rotation]
        suggestions:
          - Token rotation
          - revise
        """;

    [Fact]
    public void Pack_HasEachSectionsPlainTextWithoutCodeOrDiagrams()
    {
        var pack = Build(new ValidRepo()).Pack!;

        var auth = pack.Modules.Single(m => m.Slug == "case-studies").Lessons.Single(l => l.Slug == "auth");
        auth.Search.Should().BeEquivalentTo(
            [new SearchSection("", "", "Case Study 1: Authentication"), new SearchSection("token-rotation", "Token rotation", "See AccessRequest.cs and the guide.")],
            o => o.WithStrictOrdering());
        auth.CheatSheet.Search.Should().BeEquivalentTo(
            [new SearchSection("", "", "Auth"), new SearchSection("rules", "Rules", "Rotate refresh tokens.")],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void Pack_KeepsCalloutAndFoldedTextButDropsMarkersTablesMarkupAndHtml()
    {
        var repo = new ValidRepo().Replace(ValidRepo.Auth, "## Token rotation", """
            ## Token rotation

            > [!REMEMBER]
            > Rotate **every** time.

            | Token | Lives |
            |---|---|
            | `refresh` | 14 days |

            <div>hidden html</div>

            ## Go deeper

            ### Families

            A stolen token *revokes* its family.
            """);

        var auth = Build(repo).Pack!.Modules.Single(m => m.Slug == "case-studies").Lessons.Single(l => l.Slug == "auth");

        auth.Search!.Select(s => s.Text).Should().Equal(
            "Case Study 1: Authentication",
            "Rotate every time. Token Lives refresh 14 days",
            "Families A stolen token revokes its family. See AccessRequest.cs and the guide.");
    }

    [Fact]
    public void Pack_CarriesTheVocabularyWithLowerCaseKeys()
    {
        var pack = Build(new ValidRepo().Set(SearchFile, ValidVocabulary)).Pack!;

        pack.Vocabulary!.Synonyms.Keys.Should().BeEquivalentTo("rt", "rotate");
        pack.Vocabulary.Synonyms["rotate"].Should().Equal("token rotation", "rotation");
        pack.Vocabulary.Suggestions.Should().Equal("Token rotation", "revise");
    }

    [Fact]
    public void NoSearchFile_PacksAnEmptyVocabulary() =>
        Build(new ValidRepo()).Pack!.Vocabulary.Should().BeEquivalentTo(SearchVocabulary.Empty);

    [Fact]
    public void ValidFile_HasNoIssues() =>
        IssuesOf(new ValidRepo().Set(SearchFile, ValidVocabulary)).Should().BeEmpty();

    [Theory]
    [InlineData("  - Token rotation\n  - revise", "  first: Token rotation", "suggestions:")]
    [InlineData("synonyms:\n", "aliases:\n  x: [y]\nsynonyms:\n", "aliases:")]
    [InlineData("  RT: [refresh tokens]", "  RT: []", "  RT: []")]
    public void BadShape(string oldText, string newText, string lineOf)
    {
        var repo = new ValidRepo().Set(SearchFile, ValidVocabulary).Replace(SearchFile, oldText, newText);

        IssuesOf(repo).Should().Equal((SearchFile, repo.LineOf(SearchFile, lineOf), IssueCodes.SearchShape));
    }

    [Fact]
    public void KeyOfTwoWords()
    {
        var repo = new ValidRepo().Set(SearchFile, ValidVocabulary).Replace(SearchFile, "  RT:", "  \"R T\":");

        IssuesOf(repo).Should().Equal((SearchFile, repo.LineOf(SearchFile, "\"R T\""), IssueCodes.SearchKeyFormat));
    }

    [Fact]
    public void KeyRepeatedInAnotherCase()
    {
        var repo = new ValidRepo().Set(SearchFile, ValidVocabulary).Replace(SearchFile, "suggestions:", "  rt: [refresh tokens]\nsuggestions:");

        IssuesOf(repo).Should().Equal((SearchFile, repo.LineOf(SearchFile, "  rt:"), IssueCodes.SearchKeyDuplicate));
    }

    [Fact]
    public void AlternativeNotInContent()
    {
        var repo = new ValidRepo().Set(SearchFile, ValidVocabulary).Replace(SearchFile, "[refresh tokens]", "[refresh token]");

        IssuesOf(repo).Should().Equal((SearchFile, repo.LineOf(SearchFile, "  RT:"), IssueCodes.SearchAlternativeNotInContent));
    }

    [Fact]
    public void SuggestionThatFindsNothing()
    {
        var repo = new ValidRepo().Set(SearchFile, ValidVocabulary).Replace(SearchFile, "  - revise", "  - observer pattern");

        IssuesOf(repo).Should().Equal((SearchFile, repo.LineOf(SearchFile, "observer pattern"), IssueCodes.SearchSuggestionNoResult));
    }

    private static ContentBuildResult Build(ValidRepo repo) => new ContentPipeline(repo.Files(), new ContentOptions()).Build();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        Build(repo).Issues.Select(i => (i.Path, i.Line, i.Code));
}
