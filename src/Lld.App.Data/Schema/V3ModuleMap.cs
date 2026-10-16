namespace Lld.App.Data.Schema;

/// <summary>
/// Step 3 (spec 004 §3.6, §3.8): a module's map and mnemonics, a lesson's icon and hook. Existing rows get none until
/// the next pack loads (a new pack version always comes with an app that has this step).
/// </summary>
public sealed class V3ModuleMap : ISchemaMigration
{
    public int Version => 3;

    public string Name => "Module map and memory aids";

    public string Sql => """
        ALTER TABLE Modules ADD COLUMN MapJson TEXT NULL;
        ALTER TABLE Modules ADD COLUMN MnemonicsJson TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE Lessons ADD COLUMN Icon TEXT NULL;
        ALTER TABLE Lessons ADD COLUMN Hook TEXT NULL;
        """;
}
