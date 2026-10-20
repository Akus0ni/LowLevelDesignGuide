namespace Lld.Samples.Uml.State;

public enum ParcelStatus
{
    Created,
    Shipped,
    Delivered,
    Cancelled,
}

/// <summary>
/// A state machine in code: each action is allowed in exactly the states the diagram draws an arrow from, and moves
/// to the state the arrow points at. Anything else is refused.
/// </summary>
public sealed class Parcel
{
    public ParcelStatus Status { get; private set; } = ParcelStatus.Created;

    public void Ship() => Move(ParcelStatus.Created, ParcelStatus.Shipped, "ship");

    public void Cancel() => Move(ParcelStatus.Created, ParcelStatus.Cancelled, "cancel");

    public void Deliver() => Move(ParcelStatus.Shipped, ParcelStatus.Delivered, "deliver");

    private void Move(ParcelStatus from, ParcelStatus to, string action)
    {
        if (Status != from)
        {
            throw new InvalidOperationException($"Can't {action} a parcel that is {Status}.");
        }

        Status = to;
    }
}
