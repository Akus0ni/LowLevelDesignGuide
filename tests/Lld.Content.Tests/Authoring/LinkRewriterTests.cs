using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Links;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

public sealed class LinkRewriterTests
{
    private const string Path = "content/case-studies/auth/lesson.md";
    private static readonly ContentOptions Options = new() { RepositoryFileUrl = new Uri("https://github.com/o/r/blob/main/") };
    private readonly LinkRewriter _rewriter = new(new LinkResolver(Options), Options);

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void RewritesDestinationsOnly_AndDropsTheFrontMatter(string newLine)
    {
        var text = string.Join(newLine,
            "---",
            "id: case-study-auth",
            "---",
            "# Auth",
            "",
            "Read [SRP](../../oop-solid/srp/lesson.md#why) and its [sheet](../../oop-solid/srp/cheatsheet.md).",
            "Jump to [rotation](#token-rotation), see [AccessRequest.cs](../../../src/Lld.Domain/AccessRequest.cs#L5)",
            "or [MDN](https://developer.mozilla.org) and [the spec][spec] (twice: [again][spec]).",
            "",
            "[spec]: ../../../specs/001-auth/spec.md");

        var body = _rewriter.RewriteBody(Parse(text));

        body.Should().Be(string.Join(newLine,
            "# Auth",
            "",
            "Read [SRP](/learn/oop-solid/srp?at=why) and its [sheet](/revise/oop-solid/srp).",
            "Jump to [rotation](?at=token-rotation), see [AccessRequest.cs](https://github.com/o/r/blob/main/src/Lld.Domain/AccessRequest.cs#L5)",
            "or [MDN](https://developer.mozilla.org) and [the spec][spec] (twice: [again][spec]).",
            "",
            "[spec]: https://github.com/o/r/blob/main/specs/001-auth/spec.md"));
    }

    [Fact]
    public void InvalidLinks_AreLeftAsWritten()
    {
        var body = _rewriter.RewriteBody(Parse("[out](../../../../x.md) [empty]() [dir](../../oop-solid/srp/)"));

        body.Should().Be("[out](../../../../x.md) [empty]() [dir](../../oop-solid/srp/)");
    }

    [Fact]
    public void AnchorsAndSegments_AreEscapedForTheRoute()
    {
        var body = _rewriter.RewriteBody(Parse("[x](../../start/how-to-use/lesson.md#café-(1))"));

        body.Should().Be("[x](/learn/start/how-to-use?at=caf%C3%A9-%281%29)");
    }

    [Fact]
    public void Route_KeepsExternalAndInvalidAsWritten()
    {
        _rewriter.Route(new LinkTarget.External("https://x.y")).Should().BeNull();
        _rewriter.Route(new LinkTarget.Invalid("no")).Should().BeNull();
    }

    private static SourceDocument Parse(string text) => SourceDocument.Parse(Path, text).Document;
}
