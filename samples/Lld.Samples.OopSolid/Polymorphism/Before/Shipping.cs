namespace Lld.Samples.OopSolid.Polymorphism.Before;

public enum Carrier
{
    Post,
    Courier,
    Pickup,
}

/// <summary>
/// Every question about a carrier is a switch on the carrier. Adding a carrier means finding and editing every switch;
/// miss one and it throws at runtime.
/// </summary>
public static class Shipping
{
    public static decimal Cost(Carrier carrier, decimal weightKg) => carrier switch
    {
        Carrier.Post => 3.50m + (0.80m * weightKg),
        Carrier.Courier => 9.00m + (1.20m * weightKg),
        Carrier.Pickup => 0m,
        _ => throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Unknown carrier."),
    };

    public static int DaysToDeliver(Carrier carrier) => carrier switch
    {
        Carrier.Post => 3,
        Carrier.Courier => 1,
        Carrier.Pickup => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Unknown carrier."),
    };

    public static string Label(Carrier carrier) => carrier switch
    {
        Carrier.Post => "Standard post",
        Carrier.Courier => "Next-day courier",
        Carrier.Pickup => "Collect in store",
        _ => throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Unknown carrier."),
    };
}
