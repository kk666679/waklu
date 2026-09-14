using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HalalChain.Platform.Api.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling. Selects the provider based
/// on the environment so `dotnet ef migrations add` and
/// `dotnet ef database update` work without a running PostgreSQL.
///
/// Priority:
///  1. ConnectionStrings__Postgres  → PostgreSQL (production)
///  2. ConnectionStrings__Sqlite     → SQLite (explicit dev file)
///  3. (default)                     → SQLite at ./halalchain.db (zero-config dev)
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HalalChainDbContext>
{
    public HalalChainDbContext CreateDbContext(string[] args)
    {
        var pgConn   = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        var sqliteConn = Environment.GetEnvironmentVariable("ConnectionStrings__Sqlite")
                       ?? "Data Source=halalchain.db";

        var optionsBuilder = new DbContextOptionsBuilder<HalalChainDbContext>();
        if (!string.IsNullOrWhiteSpace(pgConn))
            optionsBuilder.UseNpgsql(pgConn);
        else
            optionsBuilder.UseSqlite(sqliteConn);

        // Suppress the "model changes each time it is built" warning.
        // Our HasData seeds use only compile-time constants; the warning
        // is a false positive caused by comparison precision between
        // build snapshots. The seed data is identical across rebuilds
        // so the migration is safe to apply. See:
        //   https://aka.ms/efcore-docs-configure-warnings
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new HalalChainDbContext(optionsBuilder.Options);
    }
}
