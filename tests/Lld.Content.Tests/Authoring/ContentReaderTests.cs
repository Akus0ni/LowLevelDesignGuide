using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Tests.Authoring;

public sealed class ContentReaderTests
{
    private static readonly ContentOptions Options = new();

    [Fact]
    public void ReadsModulesAndLessons_InFolderOrder()
    {
        var files = new InMemoryContentFiles()
            .Add("content/oop-solid/module.yml", "title: OOP & SOLID\norder: 1\nsummary: Basics.\n")
            .Add("content/oop-solid/srp/lesson.md", "---\nid: solid-srp\n---\n# SRP")
            .Add("content/oop-solid/srp/cheatsheet.md", "---\ntitle: SRP\n---\n# SRP")
            .Add("content/oop-solid/ocp/lesson.md", "# no front-matter")
            .Add("content/case-studies/module.yml", "title: Case studies\n")
            .Add("content/README.md", "not a module")
            .Add("src/Lld.Domain/A.cs", "class A;");

        var set = new ContentReader(files, Options).Read();

        set.Issues.Should().BeEmpty();
        set.Modules.Select(m => m.Slug).Should().Equal("case-studies", "oop-solid");
        var oop = set.Modules[1];
        oop.Path.Should().Be("content/oop-solid");
        oop.ManifestPath.Should().Be("content/oop-solid/module.yml");
        oop.Manifest!["title"]!.Scalar.Should().Be("OOP & SOLID");
        oop.Lessons.Select(l => l.Slug).Should().Equal("ocp", "srp");

        var srp = oop.Lessons[1];
        srp.ModuleSlug.Should().Be("oop-solid");
        srp.LessonPath.Should().Be("content/oop-solid/srp/lesson.md");
        srp.Lesson!.FrontMatter!["id"]!.Scalar.Should().Be("solid-srp");
        srp.CheatSheet!.Path.Should().Be("content/oop-solid/srp/cheatsheet.md");

        var ocp = oop.Lessons[0];
        ocp.Lesson!.FrontMatter.Should().BeNull();
        ocp.CheatSheet.Should().BeNull();
        set.Modules[0].Lessons.Should().BeEmpty();
    }

    [Fact]
    public void MissingManifest_IsNull_ForTheRulesToReport()
    {
        var files = new InMemoryContentFiles().Add("content/start/how-to-use/lesson.md", "# Hi");

        var module = new ContentReader(files, Options).Read().Modules.Single();

        module.Manifest.Should().BeNull();
    }

    [Fact]
    public void FoldersStartingWithDotOrUnderscore_AreIgnored()
    {
        var files = new InMemoryContentFiles()
            .Add("content/_drafts/x/lesson.md", "# Draft")
            .Add("content/.obsidian/y/lesson.md", "# Editor")
            .Add("content/start/_wip/lesson.md", "# WIP")
            .Add("content/start/module.yml", "title: Start\n");

        var set = new ContentReader(files, Options).Read();

        set.Modules.Select(m => m.Slug).Should().Equal("start");
        set.Modules[0].Lessons.Should().BeEmpty();
    }

    [Fact]
    public void UnreadableYaml_IsCollected_AndReadingGoesOn()
    {
        var files = new InMemoryContentFiles()
            .Add("content/start/module.yml", "title: [oops\n")
            .Add("content/start/a/lesson.md", "---\nid: [oops\n---\n# A")
            .Add("content/start/b/lesson.md", "---\nid: b\n---\n# B");

        var set = new ContentReader(files, Options).Read();

        set.Issues.Select(i => (i.Path, i.Code)).Should().Equal(
            ("content/start/module.yml", YamlMapParser.InvalidCode),
            ("content/start/a/lesson.md", YamlMapParser.InvalidCode));
        set.Modules[0].Lessons[1].Lesson!.FrontMatter!["id"]!.Scalar.Should().Be("b");
    }

    [Fact]
    public void MissingContentFolder_IsAnIssue()
    {
        var set = new ContentReader(new InMemoryContentFiles().Add("src/A.cs", "x"), Options).Read();

        set.Modules.Should().BeEmpty();
        set.Issues.Should().ContainSingle().Which.Code.Should().Be(ContentReader.ContentMissingCode);
    }
}
