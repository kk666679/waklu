namespace HalalChain.Domain.Catalog;

public sealed record TaxonomyNode(CategoryId Id, string Name, IReadOnlyList<TaxonomyNode> Children);
