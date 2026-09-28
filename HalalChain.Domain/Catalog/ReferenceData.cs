namespace HalalChain.Domain.Catalog;

/// <summary>
/// Small lookup values that are stable enough to live in code. Anything
/// that changes without a deployment belongs in the database instead.
/// </summary>
public static class ReferenceData
{
    public static readonly IReadOnlyList<string> SupportedCurrencies = ["MYR", "USD", "SGD", "IDR", "AED"];

    public static readonly IReadOnlyList<string> MediaRoles = ["primary", "gallery", "thumbnail", "certificate"];
}
