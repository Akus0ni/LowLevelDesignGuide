using System.Globalization;

namespace Lld.Samples.Patterns.Structural.Adapter;

/// <summary>
/// A payment library we can't change: amounts in paise as text, answers as XML. Shared by every version.
/// </summary>
public sealed class LegacyPayGateway
{
    public int Calls { get; private set; }

    public string Charge(string amountInPaise, string cardToken)
    {
        Calls++;
        return long.Parse(amountInPaise, CultureInfo.InvariantCulture) > 0
            ? $"<result status=\"ok\" ref=\"{cardToken}-{amountInPaise}\"/>"
            : "<result status=\"declined\"/>";
    }
}

/// <summary>What the rest of the app wants back: a plain answer, no XML.</summary>
public sealed record PaymentResult(bool Succeeded, string? Reference);
