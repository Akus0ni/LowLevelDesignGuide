namespace Lld.Samples.Patterns.Creational.AbstractFactory.Before;

/// <summary>
/// Every widget checks the theme itself. A new theme means editing every widget, and one forgotten check gives a
/// screen with a dark button next to a light badge.
/// </summary>
public static class Screen
{
    public static string Render(string theme) => $"{Button(theme)} {Badge(theme)}";

    private static string Button(string theme) => theme == "dark" ? "[dark button]" : "[light button]";

    private static string Badge(string theme) => theme == "dark" ? "(dark badge)" : "(light badge)";
}
