using FluentAssertions;
using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Packing;
using Lld.Content.Authoring.Snippets;

namespace Lld.Content.Tests.Authoring;

public sealed class SnippetTests
{
    private const string ShapesPath = "samples/Shapes.cs";

    private const string Shapes = """
        using System;

        namespace Samples.Lsp;

        #region Shapes
        /// <summary>A rectangle.</summary>
        public class Rectangle
        {
            public Rectangle(int width) => Width = width;

            public Rectangle() : this(1)
            {
            }

            public virtual int Width { get; set; }

            // Area is width times height.
            public int Area() => Width * Height;

            public int Area(int scale) => Area() * scale;

            public int Height { get; set; }

            public sealed class Before;
        }
        #endregion

        [Obsolete("Use Rectangle.")]
        public sealed class Square : Rectangle
        {
            public override int Width { get; set; }

            public sealed class Before;
        }

        public sealed class Box<T>;

        public sealed class Box<TKey, TValue>;
        """;

    private readonly CSharpSnippetSource _source = new(Files());

    [Fact]
    public void Type_ComesWithDocCommentsAndAttributes_WithoutDirectivesOrIndentation()
    {
        Code("#Square").Should().Be("""
            [Obsolete("Use Rectangle.")]
            public sealed class Square : Rectangle
            {
                public override int Width { get; set; }

                public sealed class Before;
            }
            """.ReplaceLineEndings("\n"));
        Code("#Rectangle").Should().StartWith("/// <summary>A rectangle.</summary>\npublic class Rectangle\n{");
    }

    [Fact]
    public void Member_IsDedented_WithItsComment()
    {
        Code("#Rectangle.Height").Should().Be("public int Height { get; set; }");
        Code("#Rectangle.Area").Should().Be(
            "// Area is width times height.\npublic int Area() => Width * Height;\n\npublic int Area(int scale) => Area() * scale;");
    }

    [Fact]
    public void ConstructorsAndNestedTypes_HaveTheirOwnSelectors()
    {
        Code("#Rectangle.Rectangle").Should().StartWith("public Rectangle(int width) => Width = width;\n\npublic Rectangle() : this(1)");
        Code("#Square.Before").Should().Be("public sealed class Before;");
    }

    [Fact]
    public void GenericArity_TellsTypesApart()
    {
        Code("#Box<T>").Should().Be("public sealed class Box<T>;");
        Code("#Box<K, V>").Should().Be("public sealed class Box<TKey, TValue>;");
        Result("#Box").Should().Be(new SnippetResult.Ambiguous("Box", 2));
    }

    [Fact]
    public void SeveralSelectors_AreJoinedByABlankLine()
    {
        Code("#Rectangle.Height,Square.Before").Should().Be("public int Height { get; set; }\n\npublic sealed class Before;");
    }

    [Fact]
    public void NoSelector_IsTheWholeFile_WithoutUsingsOrNamespace()
    {
        var code = Code(string.Empty);

        code.Should().StartWith("/// <summary>A rectangle.</summary>").And.EndWith("public sealed class Box<TKey, TValue>;");
        code.Should().NotContain("using System").And.NotContain("namespace").And.NotContain("#region");
    }

    [Fact]
    public void Problems_AreResults()
    {
        Result("#Circle").Should().Be(new SnippetResult.SymbolMissing("Circle"));
        Result("#Rectangle.Perimeter").Should().Be(new SnippetResult.SymbolMissing("Rectangle.Perimeter"));
        Result("#Before").Should().Be(new SnippetResult.Ambiguous("Before", 2));
        _source.Extract(new SnippetReference("samples/Missing.cs", [])).Should().Be(new SnippetResult.FileMissing("samples/Missing.cs"));
    }

    [Fact]
    public void FilesRead_ListsOnlyFilesThatExist()
    {
        Result("#Square");
        _source.Extract(new SnippetReference("samples/Missing.cs", []));

        _source.FilesRead.Should().Equal(ShapesPath);
    }

    [Theory]
    [InlineData("samples/A.cs", "samples/A.cs", new string[0])]
    [InlineData("src/X/A.cs#A,B.C", "src/X/A.cs", new[] { "A", "B.C" })]
    [InlineData("samples/A.cs#Pair<K, V>,A", "samples/A.cs", new[] { "Pair<K, V>", "A" })]
    public void Reference_Parse(string text, string path, string[] selectors)
    {
        var (reference, problem) = SnippetReference.Parse(text);

        problem.Should().BeNull();
        reference!.Path.Should().Be(path);
        reference.Selectors.Should().Equal(selectors);
    }

    [Theory]
    [InlineData("samples/A.txt#A")]
    [InlineData("../samples/A.cs")]
    [InlineData("/samples/A.cs")]
    [InlineData("samples\\A.cs")]
    [InlineData("samples/A.cs#")]
    [InlineData("samples/A.cs#A.")]
    [InlineData("samples/A.cs#1A")]
    public void Reference_Parse_RejectsBadReferences(string text) =>
        SnippetReference.Parse(text).Problem.Should().NotBeNull();

    [Fact]
    public void Dedent_RemovesSharedIndentation_AndOuterBlankLines() =>
        Dedent.Apply("\n    a\n\n      b  \n    c\n").Should().Be("a\n\n  b\nc");

    [Fact]
    public void LineDiff_MarksRemovedAndAddedLines()
    {
        var changes = LineDiff.Compare("class A\n{\n    void Save() {}\n    void Print() {}\n}", "class A\n{\n    void Save() {}\n}\n\nclass Printer\n{\n    void Print() {}\n}");

        // Every Before line survives (Print moved into the new class); blank lines never count.
        changes.Removed.Should().BeEmpty();
        changes.Added.Should().Equal(4, 6, 7);
    }

    [Fact]
    public void LineDiff_MarksAChangedLineOnBothSides()
    {
        var changes = LineDiff.Compare("a\nold\nc", "a\nnew\nc");

        changes.Removed.Should().Equal(2);
        changes.Added.Should().Equal(2);
    }

    [Fact]
    public void LineMarks_BridgeBlankLinesInsideAChangedBlock()
    {
        var code = "a\nb\n\nc\nd\ne\nf\ng\nh";

        LineMarks.ForDisplay([2, 4], code).Should().Equal(2, 3, 4);
        LineMarks.ForDisplay([1], code).Should().Equal(1);
    }

    [Fact]
    public void LineMarks_NoneWhenMostOfTheSideChanged() =>
        LineMarks.ForDisplay([1, 2, 3, 4], "a\nb\nc\nd\ne").Should().BeEmpty();

    [Fact]
    public void LineDiff_IgnoresTrailingWhitespace() =>
        LineDiff.Compare("a  \nb", "a\nb").Should().BeEquivalentTo(new LineChanges([], []));

    private string Code(string selectors) =>
        Result(selectors).Should().BeOfType<SnippetResult.Found>().Subject.Code;

    private SnippetResult Result(string selectors) =>
        _source.Extract(SnippetReference.Parse(ShapesPath + selectors).Reference!);

    private static InMemoryContentFiles Files()
    {
        var files = new InMemoryContentFiles();
        files.Add(ShapesPath, Shapes);
        return files;
    }
}
