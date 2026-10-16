using Lld.Samples.OopSolid.Inheritance.Language;
using AfterCounting = Lld.Samples.OopSolid.Inheritance.After.CountingPlaylist;
using AfterCsv = Lld.Samples.OopSolid.Inheritance.After.CsvContactImporter;
using AfterJson = Lld.Samples.OopSolid.Inheritance.After.JsonLinesContactImporter;
using BeforeCounting = Lld.Samples.OopSolid.Inheritance.Before.CountingPlaylist;
using BeforeCsv = Lld.Samples.OopSolid.Inheritance.Before.CsvContactImporter;
using BeforeJson = Lld.Samples.OopSolid.Inheritance.Before.JsonLinesContactImporter;

namespace Lld.Samples.Tests;

/// <summary>What the Inheritance lesson claims about each design.</summary>
public sealed class InheritanceTests
{
    [Fact]
    public void FragileBase_TheSubclassCountsEveryItemTwice()
    {
        var playlist = new BeforeCounting();

        playlist.AddSongs(["a", "b", "c"]);

        playlist.Songs.Should().HaveCount(3);
        playlist.AddedCount.Should().Be(6, "AddSongs counts 3, then the base calls the overridden Add 3 more times");
    }

    [Fact]
    public void BaseDesignedForExtension_TheHookRunsOncePerItem()
    {
        var playlist = new AfterCounting();

        playlist.AddSongs(["a", "b", "c"]);
        playlist.Add("d");

        playlist.AddedCount.Should().Be(4);
    }

    [Fact]
    public void CopiedAlgorithms_DriftApart()
    {
        var fromCsv = BeforeCsv.Import("Asha, ASHA@Example.com");
        var fromJson = BeforeJson.Import("{\"Name\":\"Asha\",\"Email\":\"ASHA@Example.com\"}");

        fromCsv.Contacts[0].Email.Should().Be("asha@example.com");
        fromJson.Contacts[0].Email.Should().Be("ASHA@Example.com", "the fix was made in one copy only");
    }

    [Fact]
    public void TemplateMethod_EveryFormatGetsTheSameRules()
    {
        var fromCsv = new AfterCsv().Import(" Asha , ASHA@Example.com\nbroken");
        var fromJson = new AfterJson().Import("{\"Name\":\" Asha \",\"Email\":\"ASHA@Example.com\"}\nnot json");

        fromCsv.Contacts.Should().Equal(fromJson.Contacts);
        fromCsv.Contacts.Should().ContainSingle().Which.Email.Should().Be("asha@example.com");
        fromCsv.Errors.Should().Equal("Line 2: expected 'name,email'.");
        fromJson.Errors.Should().ContainSingle().Which.Should().StartWith("Line 2:");
    }

    [Fact]
    public void New_HidesInsteadOfOverriding()
    {
        var loud = new LoudGreeter("Asha");
        Greeter asBase = loud;

        loud.Greet().Should().Be("HELLO, ASHA");
        asBase.Greet().Should().Be("Hello, Asha", "hiding is resolved by the variable's type at compile time");
    }

    [Fact]
    public void VirtualCallInAConstructor_RunsBeforeTheSubclassIsReady() =>
        new Label("Save").Description.Should().Be("label ''", "_text was still null when the base constructor ran");

    [Fact]
    public void ExtensionMembers_AddBehaviourWithoutInheritance()
    {
        "  ".IsBlank.Should().BeTrue();
        "akash kumar soni".Initials().Should().Be("AKS");
    }
}
