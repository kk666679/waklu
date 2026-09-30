namespace HalalChain.Marketplace.Models.Dashboard;

/// <summary>
/// A vendor as rendered by the admin vendor grid.
/// Projected from the platform vendor contract plus verification state so the
/// grid never has to reach back into the API layer.
/// </summary>
public sealed record AdminVendorRow(
    Guid Id,
    string Name,
    string? Country,
    string? Email,
    string Status,
    bool IsVerified)
{
    /// <summary>Country used for filtering; empty when the vendor has no country.</summary>
    public string CountryFilter => Country ?? string.Empty;
}

/// <summary>A product as rendered by the admin product grid.</summary>
public sealed record AdminProductRow(
    Guid Id,
    string Title,
    string VendorName,
    decimal Price,
    string Currency,
    string StatusLabel)
{
    /// <summary>Numeric status code backing <see cref="StatusLabel"/>.</summary>
    public int StatusCode { get; init; }

    public bool IsOutOfStock { get; init; }
}

/// <summary>An order row for the vendor dashboard.</summary>
public sealed record VendorOrderRow(
    string OrderId,
    string CustomerName,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset OrderDate);

/// <summary>An inventory row for the vendor dashboard.</summary>
public sealed record VendorInventoryRow(
    Guid Id,
    string Name,
    string Sku,
    int StockLevel,
    int MaxStock)
{
    public double StockPercentage => MaxStock > 0 ? (StockLevel * 100.0) / MaxStock : 0;
}

/// <summary>A vendor positioned in the auditor risk matrix.</summary>
public sealed record RiskVendorRow(string Name, int ComplianceScore);

/// <summary>An audit awaiting action in the auditor queue.</summary>
public sealed record AuditQueueRow(
    string VendorName,
    string AuditType,
    string Priority,
    string AssignedTo,
    string Status,
    DateTimeOffset DueDate)
{
    public string PriorityClass => Priority.ToLowerInvariant() switch
    {
        "high" => "priority-high",
        "medium" => "priority-medium",
        "low" => "priority-low",
        _ => "priority-normal"
    };
}

/// <summary>An evidence artifact listed on the auditor dashboard.</summary>
public sealed record EvidenceRow(
    string Title,
    string Category,
    string Description,
    DateTimeOffset UploadedDate,
    string UploadedBy);
