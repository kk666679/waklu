namespace HalalChain.Marketplace.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for PlatformDbContext.
/// Used by EF Core CLI tools for migrations when the application isn't running.
/// Connects to local PostgreSQL for development.
/// </summary>
public class PlatformDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        
        // Use PostgreSQL for development
        var connectionString = args.FirstOrDefault() 
            ?? "Host=localhost;Port=5432;Database=halalchain_marketplace;Username=halalchain_user;Password=HalalChain123!;";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            opts => opts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        
        return new PlatformDbContext(optionsBuilder.Options);
    }
}
