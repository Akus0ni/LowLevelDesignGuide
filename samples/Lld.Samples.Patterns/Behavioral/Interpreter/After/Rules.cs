using System.Globalization;

namespace Lld.Samples.Patterns.Behavioral.Interpreter.After;

/// <summary>An expression of the little rule language; each kind of expression knows how to evaluate itself.</summary>
public interface IRule
{
    bool Interpret(Cart cart);
}

/// <summary>Terminal expressions: the words of the language.</summary>
public sealed class IsMember : IRule
{
    public bool Interpret(Cart cart) => cart.IsMember;
}

public sealed class TotalOver(decimal amount) : IRule
{
    public bool Interpret(Cart cart) => cart.Total > amount;
}

/// <summary>Non-terminal expressions: built from other expressions (a Composite).</summary>
public sealed class And(IRule left, IRule right) : IRule
{
    public bool Interpret(Cart cart) => left.Interpret(cart) && right.Interpret(cart);
}

public sealed class Or(IRule left, IRule right) : IRule
{
    public bool Interpret(Cart cart) => left.Interpret(cart) || right.Interpret(cart);
}

public sealed class Not(IRule rule) : IRule
{
    public bool Interpret(Cart cart) => !rule.Interpret(cart);
}

/// <summary>
/// Turns text into an expression tree, left to right: <c>"member and not total&gt;1000"</c>. A real grammar would add
/// precedence and brackets; the evaluation side would not change.
/// </summary>
public static class RuleParser
{
    public static IRule Parse(string text)
    {
        var words = new Queue<string>(text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var rule = Term(words);
        while (words.TryDequeue(out var word))
        {
            rule = word switch
            {
                "and" => new And(rule, Term(words)),
                "or" => new Or(rule, Term(words)),
                _ => throw new FormatException($"Expected 'and' or 'or', found '{word}'."),
            };
        }

        return rule;
    }

    private static IRule Term(Queue<string> words) => words.Dequeue() switch
    {
        "not" => new Not(Term(words)),
        "member" => new IsMember(),
        var word when word.StartsWith("total>", StringComparison.Ordinal) =>
            new TotalOver(decimal.Parse(word["total>".Length..], CultureInfo.InvariantCulture)),
        var word => throw new FormatException($"Unknown word '{word}'."),
    };
}
