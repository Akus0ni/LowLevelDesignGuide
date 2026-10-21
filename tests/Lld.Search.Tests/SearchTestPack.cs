using Lld.Content;

namespace Lld.Search.Tests;

/// <summary>A small course with known words in known places, for the index contract tests.</summary>
public static class SearchTestPack
{
    public const string Patterns = "patterns";
    public const string Solid = "solid";
    public const string Decorator = "decorator-id";
    public const string Observer = "observer-id";
    public const string Srp = "srp-id";

    public static ContentPack Create(string decoratorTitle = "Decorator") => new(ContentPack.CurrentFormat, $"v-search-{decoratorTitle}", [
        new PackedModule(Solid, "OOP and SOLID", "", 1, [
            Lesson(Srp, "srp", "Single Responsibility", 1, hook: "One reason to change",
                [
                    new SearchSection("", "", "A class should have one reason to change."),
                    new SearchSection("the-idea", "The idea", "Split a class that does billing and printing. Inheritance is not the tool here."),
                    new SearchSection("smells", "Smells", "A god class with many reasons to change."),
                ],
                [new SearchSection("rule", "Rule", "One class, one job.")]),
        ]),
        new PackedModule(Patterns, "Design patterns", "", 2, [
            Lesson(Decorator, "decorator", decoratorTitle, 1, hook: "Wrap to add behaviour",
                [
                    new SearchSection("", "", "Wraps an object to add behaviour at run time."),
                    new SearchSection("the-idea", "The idea", "A wrapper with the same interface; it inherits nothing from the wrapped object."),
                    new SearchSection("seen-in-net", "Seen in .NET", "BufferedStream wraps another Stream."),
                ],
                [new SearchSection("shape", "Shape", "Same interface, wraps an inner object.")]),
            Lesson(Observer, "observer", "Observer", 2, hook: "Tell me when it changes",
                [
                    new SearchSection("", "", ""),
                    new SearchSection("the-idea", "The idea", "Subjects notify observers. The open closed principle holds: new observers need no change."),
                ],
                [new SearchSection("shape", "Shape", "Subject keeps a list of observers.")]),
        ]),
    ], new SearchVocabulary(
        new Dictionary<string, IReadOnlyList<string>> { ["srp"] = ["single responsibility"] },
        ["SOLID", "Observer"]));

    private static PackedLesson Lesson(
        string id, string slug, string title, int order, string hook, IReadOnlyList<SearchSection> sections, IReadOnlyList<SearchSection> sheet) =>
        new(id, slug, title, "", order, [], null, 1, "", [], new PackedCheatSheet(title, "", sheet), [], Hook: hook, Search: sections);
}
