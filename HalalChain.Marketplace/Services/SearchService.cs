using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Http.Models;
using HalalChain.Platform.Http.Services;
using System.Net.Http.Json;

namespace HalalChain.Marketplace.Services;

public interface ISearchService
{
    Task<SearchSuggestionDto[]> GetSuggestionsAsync(string query, int limit = 8, CancellationToken ct = default);
}

public sealed class SearchService : ISearchService
{
    private readonly IPlatformApiClient _api;
    public SearchService(IPlatformApiClient api) => _api = api;

    public async Task<SearchSuggestionDto[]> GetSuggestionsAsync(string query, int limit = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Array.Empty<SearchSuggestionDto>();
        var result = await _api.GetSearchSuggestionsAsync(query, limit, ct);
        return result.IsSuccess && result.Data != null ? result.Data : Array.Empty<SearchSuggestionDto>();
    }
}
