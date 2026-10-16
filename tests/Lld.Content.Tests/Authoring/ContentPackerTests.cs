using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Contracts;

namespace Lld.Content.Tests.Authoring;

public sealed class ContentPackerTests
{
    private static readonly ContentOptions Options = new() { RepositoryFileUrl = new Uri("https://github.com/o/r/blob/main/") };

    [Fact]
    public void Pack_OrdersModulesAndLessons_AndCarriesTheFrontMatter()
    {
        var pack = Pack(new ValidRepo());

        pack.Format.Should().Be(ContentPack.CurrentFormat);
        pack.Modules.Select(m => (m.Slug, m.Title, m.Order)).Should().Equal(("start", "Start here", 0), ("case-studies", "Case studies", 2));
        pack.Modules[0].Summary.Should().Be("How the guide works.");

        var caseStudies = pack.Modules[1];
        caseStudies.Lessons.Select(l => l.Id).Should().Equal("case-study-auth", "case-study-parking");
        var auth = caseStudies.Lessons[0];
        auth.Slug.Should().Be("auth");
        auth.Title.Should().Be("Case Study 1: Authentication");
        auth.Summary.Should().Be("The app's own sign-in, class by class.");
        auth.Order.Should().Be(1);
        auth.Tags.Should().Equal("state", "strategy");
        auth.Source.Should().Be("self-referential");
        auth.ReadingMinutes.Should().Be(1);
        auth.Outline.Should().Equal(new OutlineEntry(2, "Token rotation", "token-rotation"));
        caseStudies.Lessons[1].Source.Should().Be("classic");
        caseStudies.Lessons[1].Tags.Should().BeEmpty();
    }

    [Fact]
    public void Pack_DropsFrontMatter_AndRewritesLinks()
    {
        var pack = Pack(new ValidRepo());
        var auth = pack.Modules[1].Lessons[0];
        var howToUse = pack.Modules[0].Lessons[0];

        auth.BodyMarkdown.Should().StartWith("# Case Study 1: Authentication\n");
        auth.BodyMarkdown.Should().Contain("[the guide](/learn/start/how-to-use)");
        auth.BodyMarkdown.Should().Contain("[request]: https://github.com/o/r/blob/main/src/Lld.Domain/Access/AccessRequest.cs");
        howToUse.BodyMarkdown.Should().Contain("(/learn/case-studies/auth?at=token-rotation)");
        howToUse.BodyMarkdown.Should().Contain("[cheat sheet](/revise/case-studies/auth)");
        howToUse.BodyMarkdown.Should().Contain("[modules](?at=modules)");
        auth.CheatSheet.Title.Should().Be("Cheat sheet: Authentication");
        auth.CheatSheet.BodyMarkdown.Should().Be("# Auth\n\n## Rules\n\n- Rotate refresh tokens.\n");
    }

    [Fact]
    public void Version_IsAStableHash_ThatIgnoresLineEndings()
    {
        var first = Pack(new ValidRepo());
        var again = Pack(new ValidRepo());
        var windows = Pack(new ValidRepo().Set(ValidRepo.Auth, Read(new ValidRepo(), ValidRepo.Auth).ReplaceLineEndings("\r\n")));

        first.Version.Should().MatchRegex("^[0-9a-f]{64}$");
        again.Version.Should().Be(first.Version);
        windows.Version.Should().Be(first.Version);
        windows.Modules[1].Lessons[0].BodyMarkdown.Should().Be(first.Modules[1].Lessons[0].BodyMarkdown);
    }

    [Fact]
    public void Version_ChangesWhenAnythingChanges()
    {
        var original = Pack(new ValidRepo()).Version;

        Pack(new ValidRepo().Replace(ValidRepo.AuthSheet, "Rotate", "Always rotate")).Version.Should().NotBe(original);
        Pack(new ValidRepo().Replace(ValidRepo.CaseStudiesModule, "order: 2", "order: 3")).Version.Should().NotBe(original);
    }

    private static ContentPack Pack(ValidRepo repo)
    {
        var result = new ContentPipeline(repo.Files(), Options).Build();
        result.Issues.Should().BeEmpty();
        return result.Pack!;
    }

    private static string Read(ValidRepo repo, string path) => repo.Files().ReadAllText(path);
}
