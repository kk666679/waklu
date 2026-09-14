namespace HalalChain.Domain.Catalog;

public sealed class ProductEmbedding : IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public float[] Embedding { get; set; } = [];
    public int Dimension { get; set; } = 256;
    public string Model { get; set; } = "halalchain-local-v1";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
