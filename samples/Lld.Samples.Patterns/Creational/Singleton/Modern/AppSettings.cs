using Microsoft.Extensions.DependencyInjection;

namespace Lld.Samples.Patterns.Creational.Singleton.Modern;

public interface ISettingsFile
{
    string ReadTheme();
}

/// <summary>An ordinary class: public constructor, no static <c>Instance</c>. "Only one" is decided at start-up.</summary>
public sealed class AppSettings(ISettingsFile file)
{
    public string Theme { get; set; } = file.ReadTheme();
}

/// <summary>A screen asks for the settings in its constructor, so a test can hand it its own.</summary>
public sealed class ThemePage(AppSettings settings)
{
    public string Render() => $"theme: {settings.Theme}";
}

public static class SettingsRegistration
{
    /// <summary>The container makes one <see cref="AppSettings"/> and gives the same one to everyone who asks.</summary>
    public static IServiceCollection AddSettings<TFile>(this IServiceCollection services)
        where TFile : class, ISettingsFile =>
        services
            .AddSingleton<ISettingsFile, TFile>()
            .AddSingleton<AppSettings>()
            .AddTransient<ThemePage>();
}
