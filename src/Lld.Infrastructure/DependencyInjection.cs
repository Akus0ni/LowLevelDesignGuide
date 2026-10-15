using Lld.Application.Abstractions;
using Lld.Application.Hosting;
using Lld.Infrastructure.Hosting;
using Lld.Infrastructure.Identity;
using Lld.Infrastructure.Persistence;
using Lld.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lld.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>SQLite file shared by the API and the admin console.</summary>
    public string Path { get; set; } = DefaultPath;

    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LldGuide", "lld.db");

    public string ConnectionString => $"Data Source={Path}";
}

public static class DependencyInjection
{
    public const int MaxFailedSignIns = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Registers persistence, Identity and security implementations of the application ports.</summary>
    public static IServiceCollection AddLldInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        services.AddSingleton(database);
        services.AddDbContext<LldDbContext>(options => options.UseSqlite(database.ConnectionString));

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Length over composition rules; the full rule lives in MinLengthPasswordPolicy.
                options.Password.RequiredLength = MinLengthPasswordPolicy.MinLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedSignIns;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LldDbContext>()
            .AddPasswordValidator<PolicyPasswordValidator>();

        services.AddScoped<IAccessRequestRepository, EfAccessRequestRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<IAuditLog, EfAuditLog>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IUserAccounts, IdentityUserAccounts>();

        services.AddSingleton<IPasswordPolicy, MinLengthPasswordPolicy>();
        services.AddSingleton<ITemporaryPasswordGenerator, CryptoTemporaryPasswordGenerator>();
        services.AddSingleton<IRefreshTokenCodec, RefreshTokenCodec>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<DatabaseInitializer>();
        return services;
    }

    /// <summary>
    /// Self-hosting services shared by the API and the admin console: request budget, status announcements, backups.
    /// </summary>
    public static IServiceCollection AddLldHosting(this IServiceCollection services, IConfiguration configuration)
    {
        var budget = configuration.GetSection(BudgetOptions.SectionName).Get<BudgetOptions>() ?? new BudgetOptions();
        services.AddSingleton(budget);
        services.AddScoped<IUsageCounterStore, SqliteUsageCounterStore>();
        services.AddScoped<GetBudgetUsage>();

        services.AddOptions<ServerStatusOptions>().Bind(configuration.GetSection(ServerStatusOptions.SectionName));
        services.AddHttpClient(NtfyStatusAnnouncer.HttpClientName);
        services.AddSingleton<IStatusAnnouncer, NtfyStatusAnnouncer>();

        var backup = configuration.GetSection(BackupOptions.SectionName).Get<BackupOptions>() ?? new BackupOptions();
        services.AddSingleton(backup);
        services.AddSingleton<SqliteBackupService>();

        var hosting = configuration.GetSection(HostingOptions.SectionName).Get<HostingOptions>() ?? new HostingOptions();
        services.AddSingleton(hosting);
        return services;
    }
}

/// <summary>Creates the database folder, applies migrations and enables WAL (API and console run side by side).</summary>
public sealed class DatabaseInitializer(LldDbContext db, DatabaseOptions options)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(options.Path));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }
}
