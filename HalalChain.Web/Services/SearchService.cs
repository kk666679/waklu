namespace HalalChain.Services;

public interface ISearchService
{
    Task<List<string>> GetSuggestionsAsync(string query, CancellationToken ct = default);
}

public class SearchService : ISearchService
{
    private static readonly List<string> _suggestions =
    [
        "Halal chicken", "Organic coconut water", "Basmati rice", "Lamb rendang paste",
        "Turkish delight", "Chicken satay", "Mango juice", "Frozen fish fillets",
        "Cheese prata", "Mixed nuts", "Matcha green tea", "Halal certified snacks",
        "JAKIM certified", "Halal beef", "Organic honey", "Dates", "Olive oil", "Zaatar"
    ];

    public Task<List<string>> GetSuggestionsAsync(string query, CancellationToken ct = default)
    {
        var results = _suggestions
            .Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();
        return Task.FromResult(results);
    }
}
