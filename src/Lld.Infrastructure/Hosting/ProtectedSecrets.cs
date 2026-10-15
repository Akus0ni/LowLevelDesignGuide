using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Lld.Infrastructure.Hosting;

/// <summary>
/// Secrets for the hosted API, one DPAPI-protected file per configuration key (like Docker secrets):
/// <c>Jwt__SigningKey.secret</c> holds <c>Jwt:SigningKey</c>. DPAPI's machine scope ties the files to this PC;
/// the folder's ACL (set by <c>deploy/install.ps1</c>) decides who on the PC can read them.
/// Windows only: on other platforms the store is empty and writing fails.
/// </summary>
public sealed class ProtectedSecretStore(string directory)
{
    public const string FileExtension = ".secret";
    private const string KeySeparatorInFileName = "__";

    // Fixed extra entropy: another program on this PC using DPAPI can't decrypt these blobs by accident.
    private static readonly byte[] Entropy = "LldGuide.ProtectedSecrets.v1"u8.ToArray();

    public string Directory => directory;

    public bool Exists(string key) => File.Exists(PathFor(key));

    public void Write(string key, string value)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Protected secrets use Windows DPAPI and are only available on Windows.");
        }

        System.IO.Directory.CreateDirectory(directory);
        var blob = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(PathFor(key), blob);
    }

    /// <summary>Every secret as configuration key → value; empty when the folder is missing or not on Windows.</summary>
    public IReadOnlyDictionary<string, string?> ReadAll()
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!OperatingSystem.IsWindows() || !System.IO.Directory.Exists(directory))
        {
            return values;
        }

        foreach (var file in System.IO.Directory.EnumerateFiles(directory, "*" + FileExtension))
        {
            var key = Path.GetFileNameWithoutExtension(file).Replace(KeySeparatorInFileName, ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(file), Entropy, DataProtectionScope.LocalMachine);
            values[key] = Encoding.UTF8.GetString(bytes);
        }

        return values;
    }

    private string PathFor(string key) =>
        Path.Combine(directory, key.Replace(ConfigurationPath.KeyDelimiter, KeySeparatorInFileName, StringComparison.Ordinal) + FileExtension);
}

/// <summary>Adds the protected secrets as a configuration source (loaded once at start-up).</summary>
public sealed class ProtectedSecretsConfigurationSource(string directory) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new Provider(new ProtectedSecretStore(directory));

    private sealed class Provider(ProtectedSecretStore store) : ConfigurationProvider
    {
        public override void Load() => Data = new Dictionary<string, string?>(store.ReadAll(), StringComparer.OrdinalIgnoreCase);
    }
}

public static class ProtectedSecretsConfigurationExtensions
{
    /// <summary>Reads <c>Hosting:SecretsPath</c> from what is configured so far; does nothing when it is not set.</summary>
    public static IConfigurationBuilder AddProtectedSecrets(this IConfigurationManager configuration)
    {
        var directory = configuration[HostingOptions.SecretsPathKey];
        return string.IsNullOrWhiteSpace(directory)
            ? configuration
            : configuration.Add(new ProtectedSecretsConfigurationSource(directory));
    }
}
