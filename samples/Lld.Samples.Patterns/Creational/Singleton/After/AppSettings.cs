namespace Lld.Samples.Patterns.Creational.Singleton.After;

/// <summary>
/// The classic Singleton: nobody can call <c>new</c>, and the one instance is made the first time someone asks,
/// safely even when many threads ask at once (<see cref="Lazy{T}"/>).
/// </summary>
public sealed class AppSettings
{
    private static readonly Lazy<AppSettings> Shared = new(() => new AppSettings());

    private AppSettings() => Theme = SettingsFile.ReadTheme();

    public static AppSettings Instance => Shared.Value;

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
