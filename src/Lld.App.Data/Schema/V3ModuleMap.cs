namespace Lld.App.Data.Schema;

/// <summary>Step 3 (spec 004 §3.6): a module's map. Existing rows get none until the next pack loads.</summary>
public sealed class V3ModuleMap : ISchemaMigration
{
    public int Version => 3;

    public string Name => "Module map";

    public string Sql => "ALTER TABLE Modules ADD COLUMN Map TEXT NULL;";
}
