using FluentAssertions;
using Lld.Content.Authoring.Parsing;
using Lld.Contracts;

namespace Lld.Content.Tests.Authoring;

public sealed class DocumentHeadingsTests
{
    [Fact]
    public void Headings_KeepReadableText_AndNumberRepeatsAcrossLevels()
    {
        var source = Parse("""
            ---
            title: T
            ---
            # Overview

            ## The `Login` *use* case

            ### See [the docs](https://x.y) ![logo](logo.png)

            > ## Overview

            #### Deep &amp; low
            """);

        var headings = DocumentHeadings.Of(source);

        headings.Should().Equal(
            new DocumentHeading(1, "Overview", "overview", 4),
            new DocumentHeading(2, "The Login use case", "the-login-use-case", 6),
            new DocumentHeading(3, "See the docs", "see-the-docs", 8),
            new DocumentHeading(2, "Overview", "overview-1", 10),
            new DocumentHeading(4, "Deep & low", "deep--low", 12));
    }

    [Fact]
    public void Outline_HasOnlyLevelsTwoAndThree()
    {
        var headings = DocumentHeadings.Of(Parse("# A\n\n## B\n\n### C\n\n#### D\n\n## E"));

        DocumentHeadings.Outline(headings).Should().Equal(
            new OutlineEntry(2, "B", "b"),
            new OutlineEntry(3, "C", "c"),
            new OutlineEntry(2, "E", "e"));
    }

    private static SourceDocument Parse(string text) => SourceDocument.Parse("content/m/l/lesson.md", text).Document;
}
