using Lld.Domain.Access;
using Lld.Domain.Audit;
using Lld.Domain.Identity;
using Lld.Domain.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lld.Infrastructure.Persistence;

/// <summary>An ASP.NET Core Identity user plus the fields this app needs.</summary>
public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Set when the password is a temporary one; cleared after the user chooses their own.</summary>
    public bool MustChangePassword { get; set; }

    public bool IsDisabled { get; set; }
}

public sealed class LldDbContext(DbContextOptions<LldDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot compare or sort DateTimeOffset; store UTC ticks so queries like "ExpiresAt > now" translate.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var emailConverter = new ValueConverter<EmailAddress, string>(e => e.Value, v => EmailAddress.FromTrusted(v));

        builder.Entity<AccessRequest>(request =>
        {
            request.ToTable("AccessRequests");
            request.HasKey(r => r.Id);
            request.Property(r => r.Email).HasConversion(emailConverter).HasMaxLength(EmailAddress.MaxLength).IsRequired();
            request.Property(r => r.Name).HasMaxLength(AccessRequest.MaxNameLength).IsRequired();
            request.Property(r => r.Note).HasMaxLength(AccessRequest.MaxNoteLength);
            request.Property(r => r.Status).HasConversion<int>();
            request.Ignore(r => r.DomainEvents);

            // At most one pending request per email (merging relies on this).
            request.HasIndex(r => r.Email).IsUnique().HasFilter($"\"Status\" = {(int)AccessRequestStatus.Pending}");
            request.HasIndex(r => r.Status);
        });

        builder.Entity<RefreshToken>(token =>
        {
            token.ToTable("RefreshTokens");
            token.HasKey(t => t.Id);
            token.Property(t => t.UserId).IsRequired();
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.Property(t => t.RevokedReason).HasConversion<int?>();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.FamilyId);
            token.HasIndex(t => t.UserId);
            token.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditEntry>(entry =>
        {
            entry.ToTable("AuditEntries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Type).HasConversion<string>().HasMaxLength(40);
            entry.Property(e => e.Email).HasMaxLength(EmailAddress.MaxLength);
            entry.Property(e => e.Details).HasMaxLength(500);
            entry.HasIndex(e => e.OccurredAt);
        });

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "role-admin", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "role-admin" },
            new IdentityRole { Id = "role-learner", Name = "Learner", NormalizedName = "LEARNER", ConcurrencyStamp = "role-learner" });
    }
}
