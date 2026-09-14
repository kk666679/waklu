namespace HalalChain.Platform.Http.Services;

public sealed record ProductQuery(
    string? Path = null,
    string? Department = null,
    string? Category = null,
    string? Subcategory = null,
    string? Type = null,
    int? Status = null,
    int? Dietary = null,
    string? Country = null,
    int? Channel = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Search = null,
    string? SortBy = "newest",
    int Page = 1,
    int PageSize = 24);
