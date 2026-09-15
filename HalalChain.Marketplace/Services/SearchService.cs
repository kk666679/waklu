using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Marketplace.Services;

public interface ISearchService
{
    Task<Result<SearchSuggestionDto[]>> GetSuggestionsAsync(string query, int limit = 8, CancellationToken ct = default);
}

public sealed class SearchService : ISearchService
{
    private readonly IApiClient _api;
    public SearchService(IApiClient api) => _api = api;

    public async Task<Result<SearchSuggestionDto[]>> GetSuggestionsAsync(string query, int limit = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Result<SearchSuggestionDto[]>.Ok(Array.Empty<SearchSuggestionDto>());
        var result = await _api.GetAsync<SearchSuggestionDto[]>("/api/v1/search/suggestions", new { query, limit }, ct);
        return result.IsSuccess ? Result<SearchSuggestionDto[]>.Ok(result.Value ?? []) : result;
    }
}