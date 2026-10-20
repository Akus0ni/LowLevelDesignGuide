using System.Collections.Frozen;
using Lld.Samples.Patterns.Structural.Flyweight.After;

namespace Lld.Samples.Patterns.Structural.Flyweight.Modern;

/// <summary>
/// Modern .NET: when every shared part is known up front, build them once into a <see cref="FrozenDictionary{TKey, TValue}"/>,
/// which is read-only, thread-safe and fast to look up. .NET does the same for strings (<c>string.Intern</c>) and
/// buffers (<c>ArrayPool&lt;T&gt;.Shared</c>).
/// </summary>
public static class TreeKinds
{
    private static readonly FrozenDictionary<string, TreeType> All = new Dictionary<string, TreeType>(StringComparer.Ordinal)
    {
        ["oak"] = new("oak", "green"),
        ["pine"] = new("pine", "dark green"),
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static TreeType Of(string kind) => All[kind];
}
