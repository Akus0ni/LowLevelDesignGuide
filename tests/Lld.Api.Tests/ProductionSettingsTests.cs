using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Lld.Api.Tests;

/// <summary>The hosted settings file must load: a typo here only shows up when the Windows service fails to start.</summary>
public sealed class ProductionSettingsTests
{
    [Fact]
    public void ProductionSettings_ParseAndPointAtTheHostedPaths()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepoRoot(), "src", "Lld.Api", "appsettings.Production.json"))
            .Build();

        configuration["Kestrel:Endpoints:Http:Url"].Should().Be("http://localhost:5280", "the API must not listen on the LAN");
        configuration["Database:Path"].Should().Be(@"C:\ProgramData\LldGuide\data\lld.db");
        configuration["Hosting:SecretsPath"].Should().Be(@"C:\ProgramData\LldGuide\secrets");
        configuration["Backup:Path"].Should().Be(@"D:\Applications\LldGuide\backups");
        configuration["Jwt:SigningKey"].Should().BeNull("secrets never live in the repo");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LowLevelDesignGuide.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
