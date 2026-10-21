using Lld.Search.Sqlite;

namespace Lld.App.Data.Schema;

/// <summary>
/// Step 7 (spec 009 §3.8): the search tables, two FTS5 indexes and the triggers that keep each owner's notes indexed in
/// the same transaction as the note. Existing notes are indexed here; content units arrive with the next pack load,
/// which the pack format 4 version change forces on the first launch after the upgrade.
/// </summary>
public sealed class V7Search : ISchemaMigration
{
    public int Version => 7;

    public string Name => "Search";

    public string Sql { get; } = SearchSchema.TablesSql + SearchSchema.NoteTriggersSql + SearchSchema.ReindexNotesSql;
}
