using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lld.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create migrations without starting the API.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LldDbContext>
{
    public LldDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LldDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
