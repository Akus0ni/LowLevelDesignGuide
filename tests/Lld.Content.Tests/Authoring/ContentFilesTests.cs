using FluentAssertions;
using Lld.Content.Authoring.Files;

namespace Lld.Content.Tests.Authoring;

public sealed class ContentFilesTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("lld-content-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Physical_ReadsWithForwardSlashPaths_AndListsFoldersInOrdinalOrder()
    {
        Directory.CreateDirectory(Path.Combine(_root, "content", "b-mod"));
        Directory.CreateDirectory(Path.Combine(_root, "content", "a-mod"));
        Directory.CreateDirectory(Path.Combine(_root, "content", "B-upper"));
        File.WriteAllText(Path.Combine(_root, "content", "a-mod", "module.yml"), "title: A");
        var files = new PhysicalContentFiles(_root);

        files.GetDirectories("content").Should().Equal("B-upper", "a-mod", "b-mod");
        files.FileExists("content/a-mod/module.yml").Should().BeTrue();
        files.ReadAllText("content/a-mod/module.yml").Should().Be("title: A");
        files.DirectoryExists("content/a-mod").Should().BeTrue();
        files.GetDirectories("content/missing").Should().BeEmpty();
    }

    [Fact]
    public void Physical_AcceptsARootWithATrailingSeparator()
    {
        Directory.CreateDirectory(Path.Combine(_root, "content"));
        var files = new PhysicalContentFiles(_root + Path.DirectorySeparatorChar);

        files.DirectoryExists("content").Should().BeTrue();
    }

    [Fact]
    public void Physical_RefusesPathsOutsideTheRepository()
    {
        var files = new PhysicalContentFiles(_root);

        var read = () => files.ReadAllText("../outside.txt");

        read.Should().Throw<ArgumentException>().WithMessage("*outside the repository*");
    }

    [Fact]
    public void InMemory_ImpliesFoldersFromFilePaths()
    {
        var files = new InMemoryContentFiles()
            .Add("content/b/x/lesson.md", "b")
            .Add("content/a/module.yml", "a")
            .Add("content/top.md", "top");

        files.GetDirectories("content").Should().Equal("a", "b");
        files.GetDirectories("content/b").Should().Equal("x");
        files.DirectoryExists("content/a").Should().BeTrue();
        files.DirectoryExists("content/c").Should().BeFalse();
        files.FileExists("content/top.md").Should().BeTrue();
    }
}
