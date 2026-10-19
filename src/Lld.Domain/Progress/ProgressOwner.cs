namespace Lld.Domain.Progress;

/// <summary>
/// Whose progress it is: a signed-in user, or the guest who reads on a device without signing in (spec 005 §3.9).
/// <see cref="Key"/> is what a store saves; the prefix keeps a user id from ever looking like the guest.
/// </summary>
public sealed record ProgressOwner
{
    private const string GuestKey = "guest";
    private const string UserPrefix = "user:";

    private ProgressOwner(string key, string? userId)
    {
        Key = key;
        UserId = userId;
    }

    public static ProgressOwner Guest { get; } = new(GuestKey, null);

    public string Key { get; }

    /// <summary>The user's id; null for the guest.</summary>
    public string? UserId { get; }

    public bool IsGuest => UserId is null;

    public static ProgressOwner User(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new ProgressOwner(UserPrefix + userId, userId);
    }

    public override string ToString() => Key;
}
