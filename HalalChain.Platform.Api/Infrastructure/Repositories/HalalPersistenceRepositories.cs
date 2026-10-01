using System.Collections.Concurrent;
using HalalChain.Application.Halal.Interfaces;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Halal;
using HalalChain.Platform.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Infrastructure.Repositories;

public sealed class EfCertificateRepository(HalalChainDbContext db) : ICertificateRepository
{
    public async Task<Certificate?> GetAsync(CertificateId id, CancellationToken ct = default)
    {
        var certificateId = Guid.TryParse(id.Value, out var parsed)
            ? parsed
            : Guid.Empty;

        return await db.Products
            .Include(p => p.Certificates)
            .SelectMany(p => p.Certificates)
            .FirstOrDefaultAsync(c => c.Id == certificateId, ct);
    }

    public async Task<IReadOnlyList<Certificate>> QueryExpiringAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        return await db.Products
            .Include(p => p.Certificates)
            .SelectMany(p => p.Certificates)
            .Where(c => c.ExpiryDate >= from && c.ExpiryDate <= to)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Certificate>> ListForVendorAsync(
        Guid vendorId,
        CancellationToken ct = default)
    {
        return await db.Products
            .Where(p => p.VendorId == vendorId)
            .Include(p => p.Certificates)
            .SelectMany(p => p.Certificates)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Certificate certificate, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Certificates)
            .FirstOrDefaultAsync(p => p.Id == certificate.ProductId, ct)
            ?? throw new InvalidOperationException($"Product '{certificate.ProductId}' not found.");

        product.Certificates.Add(certificate);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class InMemoryVerdictBindingRepository : IVerdictBindingRepository
{
    private readonly ConcurrentDictionary<string, VerdictBinding> _byProduct = new(StringComparer.OrdinalIgnoreCase);

    public Task<VerdictBinding?> GetForProductAsync(ProductId productId, CancellationToken ct = default)
    {
        _byProduct.TryGetValue(productId.Value, out var binding);
        return Task.FromResult<VerdictBinding?>(binding);
    }

    public Task<IReadOnlyList<ProductVerdictBinding>> QueryByCertificateAsync(
        CertificateId certificateId,
        CancellationToken ct = default)
    {
        var matches = _byProduct.Values
            .Where(b => b.CertificateId == certificateId)
            .Select(b => new ProductVerdictBinding(
                ProductId: new ProductId(b.ProductId.Value),
                Product: new Product { Id = Guid.Parse(b.ProductId.Value), Status = ProductStatus.Draft },
                Binding: b))
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductVerdictBinding>>(matches);
    }

    public Task UpsertAsync(VerdictBinding binding, CancellationToken ct = default)
    {
        _byProduct[binding.ProductId.Value] = binding;
        return Task.CompletedTask;
    }
}
