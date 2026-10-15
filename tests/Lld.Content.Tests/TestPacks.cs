using Lld.Contracts;

namespace Lld.Content.Tests;

/// <summary>
/// A small pack whose modules and lessons are deliberately out of order, so catalogs must sort by <c>order</c>.
/// Display order: start (0) → oop-solid (1) → case-studies (2).
/// </summary>
internal static class TestPacks
{
    public static ContentPack Sample() => new(ContentPack.CurrentFormat, "v-sample", [
        Module("case-studies", "Case studies", order: 2,
            Lesson("case-study-parking", "parking-lot", "Parking lot", order: 2),
            Lesson("case-study-auth", "auth", "Authentication", order: 1, source: "self-referential"),
            Lesson("case-study-lru", "lru-cache", "LRU cache", order: 3)),
        Module("start", "Start here", order: 0,
            Lesson("how-to-use", "how-to-use", "How to use LLD Guide", order: 1)),
        Module("oop-solid", "OOP & SOLID", order: 1,
            Lesson("solid-srp", "srp", "Single responsibility", order: 1),
            Lesson("solid-ocp", "ocp", "Open/closed", order: 2)),
    ]);

    private static PackedModule Module(string slug, string title, int order, params PackedLesson[] lessons) =>
        new(slug, title, $"{title} summary.", order, lessons);

    private static PackedLesson Lesson(string id, string slug, string title, int order, string? source = null) =>
        new(
            id,
            slug,
            title,
            $"{title} in one sentence.",
            order,
            ["tag-b", "tag-a"],
            source,
            ReadingMinutes: order + 2,
            BodyMarkdown: $"# {title}\n\n## Overview\n\nBody of {id}.",
            Outline: [new OutlineEntry(2, "Overview", "overview")],
            new PackedCheatSheet($"Cheat sheet: {title}", $"# Cheat sheet: {title}\n\n- point"));
}
