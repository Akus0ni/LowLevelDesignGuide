namespace Lld.App.Data.Schema;

/// <summary>
/// One step of the device schema. Steps are numbered 1, 2, 3… and never edited once shipped: a change is a new step
/// (one class each), so devices on any older version reach the latest one in order.
/// </summary>
public interface ISchemaMigration
{
    int Version { get; }

    /// <summary>Short description for logs and errors.</summary>
    string Name { get; }

    string Sql { get; }
}
