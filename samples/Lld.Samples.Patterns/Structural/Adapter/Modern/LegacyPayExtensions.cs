using Lld.Samples.Patterns.Structural.Adapter.After;

namespace Lld.Samples.Patterns.Structural.Adapter.Modern;

/// <summary>
/// C# 14 extension members give the old class a nicer method in our words, with no wrapper object. Handy for a few
/// call sites; when callers must depend on an interface (tests, swapping providers), keep the adapter class.
/// </summary>
public static class LegacyPayExtensions
{
    extension(LegacyPayGateway legacy)
    {
        public PaymentResult ChargeRupees(decimal rupees, string cardToken) =>
            new LegacyPayAdapter(legacy).Charge(rupees, cardToken);
    }
}
