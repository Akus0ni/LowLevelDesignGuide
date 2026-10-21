using System.Text.Json;
using Lld.Content;
using Lld.Contracts;
using Microsoft.Data.Sqlite;

namespace Lld.Search.Sqlite;

/// <summary>
/// Puts a content pack's search text and vocabulary into the search tables, inside the caller's transaction (the device
/// loads content in one transaction, spec 003). Notes are re-indexed afterwards, so they pick up renamed lessons and
/// sections.
/// </summary>
public static class SearchContentWriter
{
    private const string InsertUnitSql = """
        INSERT INTO SearchUnits (Kind, ModuleSlug, ModuleOrder, LessonId, LessonSlug, LessonTitle, LessonOrder, SectionOrder,
                                 Anchor, Section, Title, Heading, Text)
        VALUES ($kind, $module, $moduleOrder, $lesson, $lessonSlug, $lessonTitle, $lessonOrder, $sectionOrder,
                $anchor, $section, $title, $heading, $text);
        """;

    public static async Task ReplaceContentAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ContentPack pack, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pack);
        await ExecuteAsync(connection, transaction, "DELETE FROM SearchUnits; DELETE FROM SearchSynonyms; DELETE FROM SearchSuggestions;", cancellationToken)
            .ConfigureAwait(false);
        await WriteVocabularyAsync(connection, transaction, pack.Vocabulary ?? SearchVocabulary.Empty, cancellationToken).ConfigureAwait(false);
        await using var insert = Command(connection, transaction, InsertUnitSql);
        for (var moduleIndex = 0; moduleIndex < pack.Modules.Count; moduleIndex++)
        {
            var module = pack.Modules[moduleIndex];
            for (var lessonIndex = 0; lessonIndex < module.Lessons.Count; lessonIndex++)
            {
                var lesson = module.Lessons[lessonIndex];
                var place = new Place(module.Slug, moduleIndex, lesson, lessonIndex);
                await InsertSectionsAsync(insert, place, SearchKinds.Lesson, LessonUnits(lesson), cancellationToken).ConfigureAwait(false);
                await InsertSectionsAsync(insert, place, SearchKinds.CheatSheet, lesson.CheatSheet.Search ?? [], cancellationToken).ConfigureAwait(false);
            }
        }

        await ExecuteAsync(connection, transaction, SearchSchema.ReindexNotesSql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// A lesson always has an intro unit (anchor <c>""</c>): notes look the lesson up through it, and it carries the hook
    /// (in <c>Heading</c>, so hook words don't count as title words).
    /// </summary>
    private static IEnumerable<SearchSection> LessonUnits(PackedLesson lesson)
    {
        var sections = lesson.Search ?? [];
        var intro = sections.FirstOrDefault(s => s.Anchor.Length == 0);
        yield return new SearchSection(string.Empty, lesson.Hook ?? string.Empty, intro?.Text ?? string.Empty);
        foreach (var section in sections.Where(s => s.Anchor.Length > 0))
        {
            yield return section;
        }
    }

    private static async Task InsertSectionsAsync(
        SqliteCommand insert, Place place, string kind, IEnumerable<SearchSection> sections, CancellationToken cancellationToken)
    {
        var order = 0;
        foreach (var section in sections)
        {
            var isIntro = section.Anchor.Length == 0;
            Bind(insert,
                ("$kind", kind), ("$module", place.ModuleSlug), ("$moduleOrder", place.ModuleOrder), ("$lesson", place.Lesson.Id),
                ("$lessonSlug", place.Lesson.Slug), ("$lessonTitle", place.Lesson.Title), ("$lessonOrder", place.LessonOrder),
                ("$sectionOrder", order++), ("$anchor", section.Anchor), ("$section", isIntro ? string.Empty : section.Heading),
                ("$title", place.Lesson.Title), ("$heading", section.Heading), ("$text", section.Text));
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteVocabularyAsync(
        SqliteConnection connection, SqliteTransaction? transaction, SearchVocabulary vocabulary, CancellationToken cancellationToken)
    {
        await using var synonym = Command(connection, transaction, "INSERT INTO SearchSynonyms (Word, AlternativesJson) VALUES ($word, $json);");
        foreach (var (word, alternatives) in vocabulary.Synonyms)
        {
            Bind(synonym, ("$word", word), ("$json", JsonSerializer.Serialize(alternatives, SearchJsonContext.Default.IReadOnlyListString)));
            await synonym.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var suggestion = Command(connection, transaction, "INSERT INTO SearchSuggestions (Position, Text) VALUES ($position, $text);");
        for (var i = 0; i < vocabulary.Suggestions.Count; i++)
        {
            Bind(suggestion, ("$position", i), ("$text", vocabulary.Suggestions[i]));
            await suggestion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
#pragma warning disable CA2100 // SQL comes from this assembly's constants; every value is a parameter.
        command.CommandText = sql;
#pragma warning restore CA2100
        return command;
    }

    internal static void Bind(SqliteCommand command, params (string Name, object? Value)[] parameters)
    {
        command.Parameters.Clear();
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private static async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, sql);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Where a lesson sits in the course, for ordering ties.</summary>
    private sealed record Place(string ModuleSlug, int ModuleOrder, PackedLesson Lesson, int LessonOrder);
}
