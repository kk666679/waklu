namespace HalalChain.Marketplace.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for ApplicationDbContext.
/// Used by EF Core CLI tools for migrations when the application isn't running.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use SQLite for local development/migrations
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HalalChain.Marketplace",
            "marketplace.db");
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
