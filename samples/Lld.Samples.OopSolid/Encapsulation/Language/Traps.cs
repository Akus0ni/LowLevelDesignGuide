using System.Collections.ObjectModel;

namespace Lld.Samples.OopSolid.Encapsulation.Language;

/// <summary>Looks read-only, but the caller gets the real list: <c>(List&lt;string&gt;)team.Members</c> can change it.</summary>
public sealed class LeakyTeam
{
    private readonly List<string> _members = [];

    public IReadOnlyCollection<string> Members => _members;

    public void Join(string name) => _members.Add(name);
}

/// <summary>Hands out a read-only wrapper: there is nothing to cast back to.</summary>
public sealed class Team
{
    private readonly List<string> _members = [];

    public ReadOnlyCollection<string> Members => _members.AsReadOnly();

    public void Join(string name) => _members.Add(name);
}

/// <summary>The constructor checks the value, but <c>with { Value = 150 }</c> skips the constructor.</summary>
public sealed record LeakyPercentage
{
    public LeakyPercentage(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
        Value = value;
    }

    public decimal Value { get; init; }
}

/// <summary>C# 14's <c>field</c> keyword puts the check in the accessor, so <c>with</c> and initializers go through it too.</summary>
public sealed record Percentage
{
    public Percentage(decimal value) => Value = value;

    public decimal Value
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
            field = value;
        }
    }
}
