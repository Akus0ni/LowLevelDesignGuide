using Microsoft.Extensions.DependencyInjection;
using AbstractFactory = Lld.Samples.Patterns.Creational.AbstractFactory;
using Builder = Lld.Samples.Patterns.Creational.Builder;
using FactoryMethod = Lld.Samples.Patterns.Creational.FactoryMethod;
using Prototype = Lld.Samples.Patterns.Creational.Prototype;
using Singleton = Lld.Samples.Patterns.Creational.Singleton;

namespace Lld.Samples.Tests.Patterns;

/// <summary>What the Creational pattern lessons (spec 006 batch 1) claim about each design.</summary>
public sealed class CreationalTests
{
    private static readonly string[] Rows = ["pen", "ink"];

    [Fact]
    public void Singleton_Before_EveryScreenReadsTheFileAndKeepsItsOwnTheme()
    {
        var reads = Singleton.Before.SettingsFile.Reads;
        var home = new Singleton.Before.AppSettings();
        var profile = new Singleton.Before.AppSettings();

        home.Theme = "dark";

        profile.Theme.Should().Be("light", "each screen has its own copy");
        Singleton.Before.SettingsFile.Reads.Should().Be(reads + 2);
    }

    [Fact]
    public void Singleton_After_OneInstance_ReadOnce_EvenFromManyThreads()
    {
        var instances = Enumerable.Range(0, 16).AsParallel().Select(_ => Singleton.After.AppSettings.Instance).ToList();

        instances.Distinct().Should().ContainSingle();
        Singleton.After.SettingsFile.Reads.Should().Be(1);
    }

    [Fact]
    public void Singleton_Modern_TheContainerHandsOutOneInstance_AndTestsCanSwapTheFile()
    {
        using var services = Singleton.Modern.SettingsRegistration.AddSettings<DarkSettingsFile>(new ServiceCollection()).BuildServiceProvider();

        services.GetRequiredService<Singleton.Modern.AppSettings>()
            .Should().BeSameAs(services.GetRequiredService<Singleton.Modern.AppSettings>());
        services.GetRequiredService<Singleton.Modern.ThemePage>().Render().Should().Be("theme: dark");
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("markdown")]
    public void FactoryMethod_EveryVersionExportsTheSameReport(string format)
    {
        var before = FactoryMethod.Before.ReportExporter.Export(format, Rows);
        FactoryMethod.After.ReportExporter after = format == "csv" ? new FactoryMethod.After.CsvExporter() : new FactoryMethod.After.MarkdownExporter();
        Func<FactoryMethod.IReportWriter> make = format == "csv" ? () => new FactoryMethod.CsvWriter() : () => new FactoryMethod.MarkdownWriter();

        after.Export(Rows).Should().Be(before);
        new FactoryMethod.Modern.ReportExporter(make).Export(Rows).Should().Be(before);
    }

    [Fact]
    public void FactoryMethod_After_ANewFormatIsANewSubclass() =>
        new UpperCaseExporter().Export(Rows).Should().Be("ITEM\nPEN\nINK");

    [Fact]
    public void AbstractFactory_Before_OneMissedCheckMixesThemes() =>
        AbstractFactory.Before.Screen.Render("Dark").Should().Be("[light button] (light badge)", "'Dark' isn't 'dark', so every check falls through");

    [Fact]
    public void AbstractFactory_After_AFamilyAlwaysMatches()
    {
        new AbstractFactory.After.Screen(new AbstractFactory.After.DarkTheme()).Render().Should().Be(AbstractFactory.Before.Screen.Render("dark"));
        new AbstractFactory.After.Screen(new AbstractFactory.After.LightTheme()).Render().Should().Be("[light button] (light badge)");
    }

    [Fact]
    public void AbstractFactory_Modern_TheFamilyIsChosenOnceAtStartUp()
    {
        using var services = AbstractFactory.Modern.ThemeRegistration.AddTheme(new ServiceCollection(), dark: true).BuildServiceProvider();

        services.GetRequiredService<AbstractFactory.After.Screen>().Render().Should().Be("[dark button] (dark badge)");
    }

    [Fact]
    public void Builder_EveryVersionBuildsTheSameWelcome()
    {
        var before = Builder.Before.Emails.Welcome("asha@example.com");
        var after = Builder.After.Emails.Welcome("asha@example.com");
        var modern = Builder.Modern.Emails.Welcome("asha@example.com");

        (after.To, after.Subject, after.Body, after.IsHtml).Should().Be((before.To, before.Subject, before.Body, before.IsHtml));
        (modern.To, modern.Subject, modern.Body, modern.IsHtml).Should().Be((before.To, before.Subject, before.Body, before.IsHtml));
    }

    [Fact]
    public void Builder_After_PartsComeInAnyOrder_AndBuildChecksTheWhole()
    {
        var email = new Builder.After.EmailBuilder().Attach("a.pdf").HighPriority().Subject("Bill").To("ravi@example.com").Build();

        email.Attachments.Should().Equal("a.pdf");
        email.HighPriority.Should().BeTrue();
        new Builder.After.EmailBuilder().Subject("No one").Invoking(b => b.Build()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Prototype_Before_AHandMadeCopySharesTheTemplatesLines()
    {
        var template = new Prototype.Before.Quote { Title = "Monthly", Lines = ["Support"] };

        Prototype.Before.Quotes.CopyOf(template).Lines.Add("Extra hours");

        template.Lines.Should().Equal(["Support", "Extra hours"], "the copy and the template share one list");
    }

    [Fact]
    public void Prototype_After_ACloneIsIndependent()
    {
        var templates = new Prototype.After.QuoteTemplates();
        templates.Add("monthly", new Prototype.After.Quote { Title = "Monthly", Lines = ["Support"] });

        var march = templates.New("monthly");
        march.Lines.Add("Extra hours");

        templates.New("monthly").Lines.Should().Equal("Support");
        march.Title.Should().Be("Monthly");
    }

    [Fact]
    public void Prototype_Modern_WithCopiesAndLeavesTheTemplateAlone()
    {
        var template = new Prototype.Modern.Quote("Monthly", "INR", ["Support"]);

        var march = Prototype.Modern.Quotes.ForMonth(template, "March");

        march.Should().Be(template with { Title = "Monthly: March" });
        template.Title.Should().Be("Monthly");
    }

    private sealed class DarkSettingsFile : Singleton.Modern.ISettingsFile
    {
        public string ReadTheme() => "dark";
    }

    /// <summary>Added later: a new format is a new subclass; the exporter is not opened.</summary>
    private sealed class UpperCaseExporter : FactoryMethod.After.ReportExporter
    {
        protected override FactoryMethod.IReportWriter CreateWriter() => new UpperCaseWriter();
    }

    private sealed class UpperCaseWriter : FactoryMethod.IReportWriter
    {
        public string Write(IReadOnlyList<string> rows) => new FactoryMethod.CsvWriter().Write(rows).ToUpperInvariant();
    }
}
