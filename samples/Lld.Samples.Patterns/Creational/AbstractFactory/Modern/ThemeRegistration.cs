using Lld.Samples.Patterns.Creational.AbstractFactory.After;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Samples.Patterns.Creational.AbstractFactory.Modern;

public static class ThemeRegistration
{
    /// <summary>
    /// Modern .NET: choose the family once, at start-up. Everything else asks for <see cref="IThemeFactory"/> and
    /// never checks the theme again.
    /// </summary>
    public static IServiceCollection AddTheme(this IServiceCollection services, bool dark) =>
        dark
            ? services.AddSingleton<IThemeFactory, DarkTheme>().AddTransient<Screen>()
            : services.AddSingleton<IThemeFactory, LightTheme>().AddTransient<Screen>();
}
