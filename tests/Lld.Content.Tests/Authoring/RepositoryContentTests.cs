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

    /// <summary>Spec 004 A2: concept lessons are deep (prose alone takes at least 15 minutes; code comes on top).</summary>
    [Fact]
    public void OopSolidLessons_AreDeep()
    {
        const int MinimumMinutes = 15;
        var pack = new ContentPipeline(new PhysicalContentFiles(RepositoryRoot()), new ContentOptions()).Build().Pack!;

        var lessons = pack.Modules.Single(m => m.Slug == "oop-solid").Lessons;

        lessons.Should().NotBeEmpty();
        lessons.Should().AllSatisfy(lesson =>
            lesson.ReadingMinutes.Should().BeGreaterThanOrEqualTo(MinimumMinutes, $"{lesson.Id} is a deep lesson"));
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
