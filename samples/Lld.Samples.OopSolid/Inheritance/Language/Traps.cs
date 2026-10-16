namespace Lld.Samples.OopSolid.Inheritance.Language;

public class Greeter(string name)
{
    public string Name { get; } = name;

    public string Greet() => $"Hello, {Name}";
}

/// <summary><c>new</c> hides the base method instead of overriding it: which one runs depends on the variable's type.</summary>
public sealed class LoudGreeter(string name) : Greeter(name)
{
    public new string Greet() => $"HELLO, {Name.ToUpperInvariant()}";
}

#pragma warning disable CA2214 // Calling an overridable method from a constructor is the trap this sample shows.

/// <summary>The base constructor calls a virtual method before the subclass constructor has run.</summary>
public class Widget
{
    protected Widget() => Description = Describe();

    public string Description { get; }

    protected virtual string Describe() => "widget";
}

#pragma warning restore CA2214

public sealed class Label : Widget
{
    private readonly string _text;

    public Label(string text) => _text = text;

    protected override string Describe() => $"label '{_text}'";
}

/// <summary>C# 14 extension members: add behaviour to a type you can't (or shouldn't) inherit from.</summary>
public static class StringExtensions
{
    extension(string text)
    {
        public bool IsBlank => string.IsNullOrWhiteSpace(text);

        public string Initials() =>
            string.Concat(text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0])));
    }
}
