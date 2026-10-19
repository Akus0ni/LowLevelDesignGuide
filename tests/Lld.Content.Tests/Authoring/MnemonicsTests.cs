using System.Text.Json;
using FluentAssertions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

/// <summary>Checks <see cref="Mnemonics.Letters"/> against the vector the web's <c>mnemonicTiles.ts</c> also reads.</summary>
public sealed class MnemonicsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static TheoryData<string> CaseNames() => [.. LoadCases().Select(c => c.Name)];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Letters_MatchTheSharedVector(string caseName)
    {
        var fixture = LoadCases().Single(c => c.Name == caseName);

        Mnemonics.Letters(fixture.Word).Select(c => c.ToString()).Should().Equal(fixture.Letters);
    }

    [Fact]
    public void Parse_CountsOneLessonPerWordOfAPhrase()
    {
        var parsed = Mnemonics.Parse("Smart Builders Always Follow Plans: singleton, builder, abstract-factory, factory-method, prototype");

        parsed.Problem.Should().BeNull();
        parsed.Word.Should().Be("Smart Builders Always Follow Plans");
        parsed.Lessons.Should().HaveCount(5);
    }

    [Fact]
    public void Parse_PhraseWithTooFewLessons_SaysHowManyLettersItHas() =>
        Mnemonics.Parse("Fly Far: flyweight").Problem.Should().Be("'Fly Far' has 2 letters but 1 lessons.");

    private static IReadOnlyList<MnemonicCase> LoadCases()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "mnemonics.json"));
        return JsonSerializer.Deserialize<MnemonicFixture>(json, Json)!.Cases;
    }

    private sealed record MnemonicFixture(IReadOnlyList<MnemonicCase> Cases);

    private sealed record MnemonicCase(string Name, string Word, IReadOnlyList<string> Letters);
}
