using System.Net.Mail;
using Lld.Domain.Common;

namespace Lld.Domain.Identity;

/// <summary>
/// Value object for an email address. It is the username everywhere, so it is normalised
/// (trimmed, lower-cased) once, here, and compared by value.
/// </summary>
public sealed record EmailAddress
{
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static Result<EmailAddress> Create(string? input)
    {
        var candidate = input?.Trim().ToLowerInvariant() ?? string.Empty;

        if (candidate.Length == 0)
        {
            return Error.Validation("email.required", "Email is required.");
        }

        if (candidate.Length > MaxLength || !IsWellFormed(candidate))
        {
            return Error.Validation("email.invalid", "Enter a valid email address.");
        }

        return new EmailAddress(candidate);
    }

    /// <summary>Rehydrates an already-validated value (persistence only).</summary>
    public static EmailAddress FromTrusted(string value) => new(value);

    public override string ToString() => Value;

    private static bool IsWellFormed(string candidate)
    {
        if (!MailAddress.TryCreate(candidate, out var parsed))
        {
            return false;
        }

        // Reject display-name forms like "Name <a@b.c>" and require a dot in the domain.
        return parsed.Address == candidate && parsed.Host.Contains('.', StringComparison.Ordinal);
    }
}
