namespace Lld.App.Data.Schema;

/// <summary>
/// Step 5 (spec 006 §3.5–§3.7): a module's "Which pattern?" picker and look-alike groups, a lesson's periodic-table
/// symbol. Existing rows get none until the next pack loads (a new pack version always comes with this step).
/// </summary>
public sealed class V5PatternAids : ISchemaMigration
{
    public int Version => 5;

    public string Name => "Pattern picker, look-alikes and symbols";

    public string Sql => """
        ALTER TABLE Modules ADD COLUMN PickerJson TEXT NULL;
        ALTER TABLE Modules ADD COLUMN LookalikesJson TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE Lessons ADD COLUMN Symbol TEXT NULL;
        """;
}
