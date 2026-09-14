using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Api.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Events;

/// <summary>
/// One-shot hosted service that enriches legacy products with the new
/// taxonomy data (ProductTypeId + structured HalalProfile) on first
/// startup. Idempotent: only updates products that haven't been
/// enriched yet, and the underlying bootstrapper is a no-op once the
/// catalogue is fully mapped.
/// </summary>
public sealed class ProductTaxonomyBootstrapService(IServiceProvider sp, ILogger<ProductTaxonomyBootstrapService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Defer until after the host has started so the DB is reachable
        // and other services have finished their own startup work.
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HalalChainDbContext>();

            // Wait briefly for the DB to accept connections (handles the
            // "DB is still starting" race in docker compose)
            for (var i = 0; i < 5; i++)
            {
                try { await db.Database.CanConnectAsync(cancellationToken); break; }
                catch { await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken); }
            }

            var n = await ProductTaxonomyBootstrapper.EnrichAsync(db, cancellationToken);
            if (n > 0)
                log.LogInformation("ProductTaxonomyBootstrap: enriched {Count} legacy products with the new taxonomy.", n);
            else
                log.LogDebug("ProductTaxonomyBootstrap: all products already mapped.");
        }
        catch (Exception ex)
        {
            // Non-fatal: the app should still start even if enrichment
            // fails (e.g. DB not reachable). Products remain unenriched
            // and can be mapped later.
            log.LogWarning(ex, "ProductTaxonomyBootstrap: enrichment failed; products will run without the new taxonomy until retried.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
