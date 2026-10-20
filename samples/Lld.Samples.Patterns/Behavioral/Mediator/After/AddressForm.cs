namespace Lld.Samples.Patterns.Behavioral.Mediator.After;

/// <summary>The mediator: colleagues only ever tell it "I changed".</summary>
public interface IFormMediator
{
    void Changed(object control);
}

/// <summary>A colleague: knows the mediator, not the other controls.</summary>
public sealed class CountryBox(IFormMediator form)
{
    public string Country { get; private set; } = "IN";

    public void Select(string country)
    {
        Country = country;
        form.Changed(this);
    }
}

public sealed class PostcodeBox(IFormMediator form)
{
    public string Text { get; private set; } = string.Empty;

    public void Type(string text)
    {
        Text = text;
        form.Changed(this);
    }
}

public sealed class SubmitButton
{
    public bool Enabled { get; set; }
}

/// <summary>
/// The concrete mediator: owns the controls and every rule that connects them, in one place.
/// </summary>
public sealed class AddressForm : IFormMediator
{
    public AddressForm()
    {
        Country = new CountryBox(this);
        Postcode = new PostcodeBox(this);
    }

    public CountryBox Country { get; }

    public PostcodeBox Postcode { get; }

    public SubmitButton Submit { get; } = new();

    public void Changed(object control) => Submit.Enabled = IsValidPostcode(Country.Country, Postcode.Text);

    private static bool IsValidPostcode(string country, string postcode) =>
        country == "IN" ? postcode.Length == 6 && postcode.All(char.IsAsciiDigit) : postcode.Length is >= 5 and <= 8;
}
