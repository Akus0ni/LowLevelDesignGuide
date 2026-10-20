namespace Lld.Samples.Patterns.Behavioral.Mediator.Before;

/// <summary>
/// Each control knows the others and updates them itself. The "valid postcode enables Submit" rule is written twice,
/// and adding a control (a "same as billing" box) means editing every control it touches.
/// </summary>
public sealed class SubmitButton
{
    public bool Enabled { get; set; }
}

public sealed class PostcodeBox(SubmitButton submit)
{
    public string Country { get; set; } = "IN";

    public string Text { get; private set; } = string.Empty;

    public void Type(string text)
    {
        Text = text;
        submit.Enabled = Country == "IN" ? text.Length == 6 && text.All(char.IsAsciiDigit) : text.Length is >= 5 and <= 8;
    }
}

public sealed class CountryBox(PostcodeBox postcode, SubmitButton submit)
{
    public void Select(string country)
    {
        postcode.Country = country;
        submit.Enabled = country == "IN"
            ? postcode.Text.Length == 6 && postcode.Text.All(char.IsAsciiDigit)
            : postcode.Text.Length is >= 5 and <= 8;
    }
}
