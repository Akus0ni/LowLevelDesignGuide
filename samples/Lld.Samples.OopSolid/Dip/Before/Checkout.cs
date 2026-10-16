namespace Lld.Samples.OopSolid.Dip.Before;

/// <summary>A low-level detail: talks to a mail server.</summary>
public sealed class SmtpMailer(string server)
{
    public void Send(string to, string body) =>
        throw new InvalidOperationException($"No mail server '{server}' here; can't send '{body}' to {to}.");
}

/// <summary>
/// High-level policy (what checkout means) built directly on a low-level detail (SMTP). It can't run, or be tested,
/// without a mail server, and moving to SMS means editing checkout.
/// </summary>
public sealed class CheckoutService
{
    private readonly SmtpMailer _mailer = new("smtp.example.com");

    public decimal Checkout(string customerEmail, decimal total)
    {
        _mailer.Send(customerEmail, $"Thanks! You paid {total}.");
        return total;
    }
}
