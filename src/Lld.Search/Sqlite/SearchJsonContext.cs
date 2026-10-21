using System.Text.Json.Serialization;

namespace Lld.Search.Sqlite;

/// <summary>Source-generated JSON for the synonym lists stored in SQLite, so it survives Android trimming.</summary>
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<long>))]
internal sealed partial class SearchJsonContext : JsonSerializerContext;
