using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Parsing;
using Lld.Contracts;

namespace Lld.Content.Tests.Authoring;

public sealed class EntityTermsTests
{
    [Fact]
    public void EntitiesTable_GivesTermRoleAndTheHeadingItSitsUnder()
    {
        var terms = TermsOf("""
            # Title

            ## 3. Core entities

            | Entity | Responsibility | Key fields |
            |---|---|---|
            | `EmailAddress` (value object) | Normalise + validate the username once | `Value` |
            | `Result` / `Error` | Outcome of every operation | `IsSuccess` |
            | Plain text, no code | Ignored | — |
            """);

        terms.Should().Equal(
            new LessonTerm("EmailAddress", "Normalise + validate the username once", "3-core-entities"),
            new LessonTerm("Result", "Outcome of every operation", "3-core-entities"));
    }

    [Theory]
    [InlineData("Class")]
    [InlineData("type")]
    [InlineData("INTERFACE")]
    public void OtherHeaderNames_Count(string header)
    {
        var terms = TermsOf($"## Types\n\n| {header} | Role |\n|---|---|\n| `IClock` | Tells the time |\n");

        terms.Should().Equal(new LessonTerm("IClock", "Tells the time", "types"));
    }

    [Fact]
    public void OtherTables_AreIgnored_AndTheFirstDefinitionWins()
    {
        var terms = TermsOf("""
            ## Patterns

            | Pattern | Where |
            |---|---|
            | `State` | AccessRequest |

            ## Entities

            | Entity | Role |
            |---|---|
            | `Token` | First role |

            | Entity | Role |
            |---|---|
            | `Token` | Second role |
            """);

        terms.Should().Equal(new LessonTerm("Token", "First role", "entities"));
    }

    [Fact]
    public void RealAuthLesson_HasItsEntitiesAsTerms()
    {
        var pack = new ContentPipeline(new PhysicalContentFiles(RepositoryContentTests.RepositoryRoot()), new ContentOptions()).Build().Pack!;

        var auth = pack.Modules.SelectMany(m => m.Lessons).Single(l => l.Id == "case-study-auth");

        auth.Terms.Select(t => t.Term).Should().Contain(["AccessRequest", "RefreshToken", "Result"]);
        auth.Terms.Should().OnlyContain(t => t.Anchor == "3-core-entities" && t.Role.Length > 0);
    }

    private static IReadOnlyList<LessonTerm> TermsOf(string markdown)
    {
        var (document, _) = SourceDocument.Parse("content/m/l/lesson.md", markdown);
        return EntityTerms.Of(document, DocumentHeadings.Of(document));
    }
}
