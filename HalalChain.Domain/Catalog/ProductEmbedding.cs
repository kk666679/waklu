namespace HalalChain.Domain.Catalog;

/// <summary>
/// Reference to a product embedding stored in the vector database. The
/// vector itself lives in Qdrant; this record is the join key.
/// </summary>
public sealed record ProductEmbedding(
    ProductId ProductId,
    string VectorId,
    string ModelId,
    int Dimensions,
    DateTimeOffset ComputedAt);
