namespace Lld.Samples.Patterns.Creational.Singleton.Before;

/// <summary>
/// Every screen makes its own settings. The file is read again each time, and a theme changed on one screen is never
/// seen by the others.
/// </summary>
public sealed class AppSettings
{
    public AppSettings() => Theme = SettingsFile.ReadTheme();

    public string Theme { get; set; }
}

/// <summary>Stands in for a slow file read; counts how often it happens.</summary>
public static class SettingsFile
{
    private static int _reads;

    public static int Reads => _reads;

    public static string ReadTheme()
    {
        Interlocked.Increment(ref _reads);
        return "light";
    }
}
