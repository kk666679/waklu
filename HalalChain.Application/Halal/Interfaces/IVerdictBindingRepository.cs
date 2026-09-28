namespace HalalChain.Application.Halal.Interfaces;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Halal;

public interface IVerdictBindingRepository
{
    Task<VerdictBinding?> GetForProductAsync(ProductId productId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVerdictBinding>> QueryByCertificateAsync(
        CertificateId certificateId,
        CancellationToken ct = default);

    Task UpsertAsync(VerdictBinding binding, CancellationToken ct = default);
}

public sealed record ProductVerdictBinding(
    ProductId ProductId,
    Product Product,
    VerdictBinding Binding);
