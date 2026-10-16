using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;

namespace Lld.Content.Tests.Authoring;

/// <summary>The real <c>content/</c> folder must always pack: CI fails here before any app build does.</summary>
public sealed class RepositoryContentTests
{
    private const string SolutionFile = "LowLevelDesignGuide.slnx";

    [Fact]
    public void RepositoryContent_IsValid()
    {
        var result = new ContentPipeline(new PhysicalContentFiles(RepositoryRoot()), new ContentOptions()).Build();

        result.Issues.Select(i => i.ToString()).Should().BeEmpty("every file under content/ must follow the authoring contract");
        result.Pack!.Modules.Select(m => m.Slug).Should().StartWith("start").And.Contain("case-studies");
        result.Pack.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).Should().Contain("case-study-auth");
    }

    /// <summary>
    /// Spec 004 A2 (amended): a concept lesson is a short visual card; the detail waits in "Go deeper", which doesn't
    /// count towards reading time.
    /// </summary>
    [Fact]
    public void OopSolidLessons_AreShortCards_WithAGoDeeperSection()
    {
        const int MaximumCardMinutes = 8;
        var pack = new ContentPipeline(new PhysicalContentFiles(RepositoryRoot()), new ContentOptions()).Build().Pack!;

        var lessons = pack.Modules.Single(m => m.Slug == "oop-solid").Lessons;

        lessons.Should().NotBeEmpty();
        lessons.Should().AllSatisfy(lesson =>
        {
            lesson.ReadingMinutes.Should().BeLessThanOrEqualTo(MaximumCardMinutes, $"{lesson.Id}'s card is short");
            lesson.Outline.Select(o => o.Text).Should().Contain("Go deeper");
        });
    }

    internal static string RepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException($"{SolutionFile} not found above {AppContext.BaseDirectory}.");
    }
}
