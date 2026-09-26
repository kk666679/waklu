namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// Request for bulk product operations: import, export, update, delete.
/// Supports CSV and JSON formats with transaction semantics.
/// </summary>
public sealed class CatalogBulkImportRequest
{
    /// <summary>
    /// Import format: "CSV" or "JSON".
    /// </summary>
    public string Format { get; set; } = "CSV";

    /// <summary>
    /// CSV or JSON file content (as base64 or raw text).
    /// </summary>
    public string FileContent { get; set; } = string.Empty;

    /// <summary>
    /// Vendor ID uploading the catalog (usually from auth context).
    /// </summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// Import mode: "Replace" (delete existing), "Merge" (upsert), or "Append" (new only).
    /// </summary>
    public string Mode { get; set; } = "Merge";

    /// <summary>
    /// Whether to rollback entire import on validation error.
    /// If false, valid rows are imported; invalid rows are skipped with warnings.
    /// </summary>
    public bool TransactionMode { get; set; } = true;

    /// <summary>
    /// Field mapping for CSV import (if headers don't match standard names).
    /// Example: { "col_1": "Name", "col_2": "Sku", "col_3": "Price" }
    /// </summary>
    public Dictionary<string, string>? FieldMapping { get; set; }
}

/// <summary>
/// Response from bulk import operation.
/// </summary>
public sealed class CatalogBulkImportResponse
{
    /// <summary>
    /// Unique identifier for this import batch.
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Status: "Pending", "Processing", "Completed", "Failed", "Partially Failed".
    /// </summary>
    public string Status { get; set; } = "Processing";

    /// <summary>
    /// Total rows processed.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Rows successfully imported.
    /// </summary>
    public int SuccessfulRows { get; set; }

    /// <summary>
    /// Rows with validation errors.
    /// </summary>
    public int FailedRows { get; set; }

    /// <summary>
    /// Detailed error messages per row.
    /// Example: { "row_3": ["SKU is required", "Price must be > 0"] }
    /// </summary>
    public Dictionary<int, List<string>> ErrorsByRow { get; set; } = [];

    /// <summary>
    /// Summary of processing (e.g., "3 products created, 5 updated, 1 skipped").
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// URL to download error report (CSV or JSON).
    /// </summary>
    public string? ErrorReportUrl { get; set; }
}

/// <summary>
/// Request for exporting catalog to CSV or JSON.
/// </summary>
public sealed class CatalogBulkExportRequest
{
    /// <summary>
    /// Export format: "CSV" or "JSON".
    /// </summary>
    public string Format { get; set; } = "CSV";

    /// <summary>
    /// Vendor ID whose catalog to export.
    /// </summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// Fields to include in export.
    /// If null, includes all default fields.
    /// </summary>
    public List<string>? Fields { get; set; }

    /// <summary>
    /// Optional filter: only export active products.
    /// </summary>
    public bool OnlyActive { get; set; } = true;

    /// <summary>
    /// Optional category filter: export only products in these categories.
    /// </summary>
    public List<Guid>? CategoryIds { get; set; }

    /// <summary>
    /// Include variants in export (if true, one row per variant; if false, one row per product).
    /// </summary>
    public bool IncludeVariants { get; set; } = true;
}

/// <summary>
/// Response with signed download URL for exported catalog.
/// </summary>
public sealed class CatalogBulkExportResponse
{
    /// <summary>
    /// Download URL (signed, expires in 24 hours).
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Total records in export.
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Approximate file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Expiration time for the download URL.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Bulk update request for pricing, inventory, or other batch changes.
/// </summary>
public sealed class CatalogBulkUpdateRequest
{
    /// <summary>
    /// Operation type: "UpdatePrice", "UpdateStock", "SetTags", "SetHalalStatus", "Activate", "Deactivate".
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Vendor ID performing the update.
    /// </summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// Product IDs to update (max 1000).
    /// </summary>
    public List<Guid> ProductIds { get; set; } = [];

    /// <summary>
    /// Update data (structure depends on OperationType).
    /// Example for UpdatePrice: { "priceAdjustmentPercent": 10, "compareAtPrice": 99.99 }
    /// </summary>
    public Dictionary<string, object>? UpdateData { get; set; }
}

/// <summary>
/// Response from bulk update operation.
/// </summary>
public sealed class CatalogBulkUpdateResponse
{
    /// <summary>
    /// Unique identifier for this update batch.
    /// </summary>
    public Guid UpdateId { get; set; }

    /// <summary>
    /// Status: "Completed", "Partial", "Failed".
    /// </summary>
    public string Status { get; set; } = "Completed";

    /// <summary>
    /// Number of products successfully updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of products that failed to update.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Summary message.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// CSV/JSON row validation result (used during import).
/// </summary>
public sealed class CatalogImportRowValidation
{
    /// <summary>
    /// Row number in the import file.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Whether this row is valid and can be imported.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error messages (if any).
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Warnings (non-blocking issues, e.g., "Image URL is invalid, using fallback").
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
