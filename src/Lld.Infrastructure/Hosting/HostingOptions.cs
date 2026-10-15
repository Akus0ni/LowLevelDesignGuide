namespace Lld.Infrastructure.Hosting;

/// <summary>Where the hosted API keeps its protected secrets (<c>Hosting:SecretsPath</c>).</summary>
public sealed class HostingOptions
{
    public const string SectionName = "Hosting";
    public const string SecretsPathKey = "Hosting:SecretsPath";

    public string? SecretsPath { get; set; }
}

/// <summary>
/// Server-status publishing (<c>ServerStatus</c> section). Publishing is off when <see cref="Topic"/> or
/// <see cref="PrivateKey"/> is empty, as in development and CI.
/// </summary>
public sealed class ServerStatusOptions
{
    public const string SectionName = "ServerStatus";

    // Configuration keys that live in the protected secrets.
    public const string TopicKey = "ServerStatus:Topic";
    public const string PrivateKeyKey = "ServerStatus:PrivateKey";

    public string NtfyBaseUrl { get; set; } = "https://ntfy.sh";

    public string? Topic { get; set; }

    /// <summary>ECDSA P-256 private key, PKCS#8 base64. Never committed.</summary>
    public string? PrivateKey { get; set; }

    /// <summary>The tunnel address (e.g. https://name.ngrok-free.dev). When set, "online" is announced only after it answers.</summary>
    public string? PublicUrl { get; set; }

    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromHours(6);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Topic) && !string.IsNullOrWhiteSpace(PrivateKey);
}

/// <summary>Nightly SQLite backups (<c>Backup</c> section).</summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    public string Path { get; set; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LldGuide", "backups");

    public int KeepDays { get; set; } = 14;
}
