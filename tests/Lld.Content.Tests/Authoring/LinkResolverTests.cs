using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Links;

namespace Lld.Content.Tests.Authoring;

public sealed class LinkResolverTests
{
    private const string Lesson = "content/case-studies/auth/lesson.md";
    private readonly LinkResolver _resolver = new(new ContentOptions());

    [Theory]
    [InlineData("https://example.com/a#b")]
    [InlineData("http://example.com")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("//cdn.example.com/x.js")]
    public void SchemesAndProtocolRelative_AreExternal(string url) =>
        _resolver.Resolve(Lesson, url).Should().Be(new LinkTarget.External(url));

    [Fact]
    public void Fragment_IsTheSameDocument() =>
        _resolver.Resolve(Lesson, "#token-rotation").Should().Be(new LinkTarget.SameDocument("token-rotation"));

    [Theory]
    [InlineData("../../oop-solid/srp/lesson.md", "oop-solid", "srp", null)]
    [InlineData("../../oop-solid/srp/lesson.md#why-it-matters", "oop-solid", "srp", "why-it-matters")]
    [InlineData("../other/lesson.md", "case-studies", "other", null)]
    [InlineData("./../../start/./how-to-use/lesson.md", "start", "how-to-use", null)]
    [InlineData("../../start/how-to-use/lesson.md#caf%C3%A9", "start", "how-to-use", "café")]
    public void LessonFiles_AreLessons(string url, string module, string lesson, string? anchor) =>
        _resolver.Resolve(Lesson, url).Should().Be(new LinkTarget.Lesson(module, lesson, anchor));

    [Theory]
    [InlineData("cheatsheet.md", "case-studies", "auth", null)]
    [InlineData("../../oop-solid/srp/cheatsheet.md#rules", "oop-solid", "srp", "rules")]
    public void CheatSheetFiles_AreCheatSheets(string url, string module, string lesson, string? anchor) =>
        _resolver.Resolve(Lesson, url).Should().Be(new LinkTarget.CheatSheet(module, lesson, anchor));

    [Theory]
    [InlineData("../../../src/Lld.Domain/Access/AccessRequest.cs", "src/Lld.Domain/Access/AccessRequest.cs", null)]
    [InlineData("../../../src/Lld.Api/Program.cs#L10-L20", "src/Lld.Api/Program.cs", "L10-L20")]
    [InlineData("../../../docs/My%20Notes.md", "docs/My Notes.md", null)]
    public void OtherRepositoryFiles_AreRepositoryFiles(string url, string path, string? fragment) =>
        _resolver.Resolve(Lesson, url).Should().Be(new LinkTarget.RepositoryFile(path, fragment));

    [Theory]
    [InlineData("", "empty")]
    [InlineData("#", "empty")]
    [InlineData("/src/A.cs", "relative")]
    [InlineData("../../../../outside.md", "outside the repository")]
    [InlineData("../../oop-solid/srp/", "lesson.md or cheatsheet.md")]
    [InlineData("../../oop-solid/module.yml", "lesson.md or cheatsheet.md")]
    [InlineData("../../oop-solid/srp/lesson.md?x=1", "query string")]
    public void BrokenShapes_AreInvalid_WithAReason(string url, string reason) =>
        _resolver.Resolve(Lesson, url).Should().BeOfType<LinkTarget.Invalid>()
            .Which.Reason.Should().ContainEquivalentOf(reason);
}
