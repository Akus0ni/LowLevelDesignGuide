namespace Lld.Samples.Patterns.Creational.AbstractFactory.After;

public interface IButton
{
    string Render();
}

public interface IBadge
{
    string Render();
}

/// <summary>The abstract factory: one method per product. A theme is a family whose products always match.</summary>
public interface IThemeFactory
{
    IButton CreateButton();

    IBadge CreateBadge();
}

public sealed class LightTheme : IThemeFactory
{
    public IButton CreateButton() => new LightButton();

    public IBadge CreateBadge() => new LightBadge();
}

public sealed class DarkTheme : IThemeFactory
{
    public IButton CreateButton() => new DarkButton();

    public IBadge CreateBadge() => new DarkBadge();
}

/// <summary>The client knows only the factory, so it can never mix two themes.</summary>
public sealed class Screen(IThemeFactory theme)
{
    public string Render() => $"{theme.CreateButton().Render()} {theme.CreateBadge().Render()}";
}

public sealed class LightButton : IButton
{
    public string Render() => "[light button]";
}

public sealed class LightBadge : IBadge
{
    public string Render() => "(light badge)";
}

public sealed class DarkButton : IButton
{
    public string Render() => "[dark button]";
}

public sealed class DarkBadge : IBadge
{
    public string Render() => "(dark badge)";
}
