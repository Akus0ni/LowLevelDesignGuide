using System.Globalization;
using System.Xml.Linq;

namespace Lld.Samples.Patterns.Structural.Adapter.After;

/// <summary>The target: the interface our app wants, in our words (rupees in, a plain result out).</summary>
public interface IPaymentGateway
{
    PaymentResult Charge(decimal rupees, string cardToken);
}

/// <summary>The adapter: implements our interface by translating every call to and from the old library.</summary>
public sealed class LegacyPayAdapter(LegacyPayGateway legacy) : IPaymentGateway
{
    public PaymentResult Charge(decimal rupees, string cardToken)
    {
        var paise = ((long)(rupees * 100)).ToString(CultureInfo.InvariantCulture);
        var result = XElement.Parse(legacy.Charge(paise, cardToken));
        return (string?)result.Attribute("status") == "ok"
            ? new PaymentResult(true, (string?)result.Attribute("ref"))
            : new PaymentResult(false, null);
    }
}

/// <summary>The client only knows the target interface; it never sees paise or XML.</summary>
public sealed class Checkout(IPaymentGateway gateway)
{
    public PaymentResult Pay(decimal rupees, string cardToken) => gateway.Charge(rupees, cardToken);
}
