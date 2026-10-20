using System.Globalization;

namespace Lld.Samples.Patterns.Behavioral.Interpreter.Before;

/// <summary>
/// Marketing writes rules like <c>"member and total&gt;1000"</c>; the code picks the string apart with ifs. It only
/// understands <c>and</c>: adding <c>or</c> or <c>not</c> turns this into a tangle nobody can test.
/// </summary>
public static class Discounts
{
    public static bool Applies(string rule, Cart cart)
    {
        foreach (var part in rule.Split(" and ", StringSplitOptions.TrimEntries))
        {
            if (part == "member" && !cart.IsMember)
            {
                return false;
            }

            if (part.StartsWith("total>", StringComparison.Ordinal)
                && cart.Total <= decimal.Parse(part["total>".Length..], CultureInfo.InvariantCulture))
            {
                return false;
            }
        }

        return true;
    }
}
