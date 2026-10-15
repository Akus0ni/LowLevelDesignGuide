using FluentAssertions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

public sealed class ReadingTimeTests
{
    private const int WordsPerMinute = 10;

    [Fact]
    public void CountsProseHeadingsTablesAndInlineCode_ButNotCodeBlocks()
    {
        var markdown = """
            # Two words

            Five words in `this` paragraph.

            | Left | Right |
            |---|---|
            | one | two |

            ```csharp
            var these = "words never count";
            ```

            ```mermaid
            classDiagram A --> B
            ```
            """;

        var document = MarkdownParsing.Parse(markdown);

        ReadingTime.Minutes(document, WordsPerMinute).Should().Be(2); // 2 + 5 + 4 = 11 words → 2 minutes
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("one two three four five six seven eight nine ten", 1)]
    [InlineData("one two three four five six seven eight nine ten eleven", 2)]
    public void RoundsUp_AndIsAtLeastOneMinute(string markdown, int minutes) =>
        ReadingTime.Minutes(MarkdownParsing.Parse(markdown), WordsPerMinute).Should().Be(minutes);
}
