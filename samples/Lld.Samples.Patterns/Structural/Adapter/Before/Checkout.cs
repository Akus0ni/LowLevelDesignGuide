using System.Globalization;

namespace Lld.Samples.Patterns.Structural.Adapter.Before;

/// <summary>
/// Checkout talks to the old library directly: converts rupees to paise text and digs through XML. Every other
/// caller (refunds, subscriptions) repeats the same conversions, and swapping the library means editing all of them.
/// </summary>
public sealed class Checkout(LegacyPayGateway gateway)
{
    public PaymentResult Pay(decimal rupees, string cardToken)
    {
        var paise = ((long)(rupees * 100)).ToString(CultureInfo.InvariantCulture);
        var xml = gateway.Charge(paise, cardToken);
        if (!xml.Contains("status=\"ok\"", StringComparison.Ordinal))
        {
            return new PaymentResult(false, null);
        }

        var start = xml.IndexOf("ref=\"", StringComparison.Ordinal) + "ref=\"".Length;
        return new PaymentResult(true, xml[start..xml.IndexOf('"', start)]);
    }
}
