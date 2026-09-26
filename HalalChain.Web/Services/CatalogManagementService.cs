using HalalChain.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace HalalChain.Services;

/// <summary>
/// Service for bulk catalog operations: import, export, update.
/// Supports CSV and JSON formats with transaction semantics and validation.
/// </summary>
public interface ICatalogManagementService
{
    /// <summary>
    /// Import products from CSV or JSON file.
    /// </summary>
    Task<BulkImportResult> ImportProductsAsync(BulkImportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Export vendor's catalog to CSV or JSON.
    /// </summary>
    Task<BulkExportResult> ExportProductsAsync(BulkExportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Perform bulk update on products (price, stock, tags, etc.).
    /// </summary>
    Task<BulkUpdateResult> UpdateProductsAsync(BulkUpdateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validate a single product row for import.
    /// </summary>
    Task<ImportRowValidation> ValidateProductRowAsync(Dictionary<string, object> row, CancellationToken ct = default);
}

public class CatalogManagementService : ICatalogManagementService
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CatalogManagementService> _logger;

    public CatalogManagementService(
        IProductService productService,
        ICategoryService categoryService,
        ILogger<CatalogManagementService> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<BulkImportResult> ImportProductsAsync(BulkImportRequest request, CancellationToken ct = default)
    {
        var result = new BulkImportResult
        {
            ImportId = Guid.NewGuid(),
            Status = "Processing"
        };

        try
        {
            var rows = request.Format.ToUpperInvariant() == "CSV"
                ? ParseCsvContent(request.FileContent, request.FieldMapping)
                : ParseJsonContent(request.FileContent);

            result.TotalRows = rows.Count;

            var validationResults = new List<ImportRowValidation>();
            foreach (var (idx, row) in rows.Select((r, i) => (i + 2, r))) // +2 for 1-based + header row
            {
                var validation = await ValidateProductRowAsync(row, ct);
                validation.RowNumber = idx;
                validationResults.Add(validation);
            }

            // If transaction mode and any errors, fail entire import
            if (request.TransactionMode && validationResults.Any(v => !v.IsValid))
            {
                result.Status = "Failed";
                result.ErrorsByRow = validationResults
                    .Where(v => !v.IsValid)
                    .ToDictionary(v => v.RowNumber, v => v.Errors);
                result.Summary = "Import failed: validation errors detected in transaction mode.";
                return result;
            }

            // Import valid rows
            var successCount = 0;
            var failedCount = 0;

            foreach (var (idx, validation) in validationResults.Select((v, i) => (i + 2, v)))
            {
                if (!validation.IsValid)
                {
                    failedCount++;
                    result.ErrorsByRow[idx] = validation.Errors;
                    continue;
                }

                try
                {
                    var product = MapRowToProduct(validationResults[idx - 2], request.VendorId);
                    await _productService.CreateAsync(product, ct);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex, "Failed to import row {RowNumber}", idx);
                    result.ErrorsByRow[idx] = new List<string> { $"Import error: {ex.Message}" };
                }
            }

            result.SuccessfulRows = successCount;
            result.FailedRows = failedCount;
            result.Status = failedCount == 0 ? "Completed" : "Partially Failed";
            result.Summary = $"{successCount} products created/updated, {failedCount} skipped due to errors.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import failed");
            result.Status = "Failed";
            result.Summary = $"Fatal error: {ex.Message}";
        }

        return result;
    }

    public async Task<BulkExportResult> ExportProductsAsync(BulkExportRequest request, CancellationToken ct = default)
    {
        try
        {
            var products = await _productService.GetAllAsync(ct);
            products = FilterProducts(products, request);

            string content;
            string fileExtension;

            if (request.Format.ToUpperInvariant() == "CSV")
            {
                content = ExportToCsv(products, request.Fields);
                fileExtension = "csv";
            }
            else
            {
                content = ExportToJson(products, request.Fields);
                fileExtension = "json";
            }

            // In a real scenario, save to cloud storage and return signed URL
            var fileName = $"catalog-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{fileExtension}";
            var fileSizeBytes = Encoding.UTF8.GetByteCount(content);

            return new BulkExportResult
            {
                DownloadUrl = $"/api/exports/{fileName}", // Placeholder
                RecordCount = products.Count,
                FileSizeBytes = fileSizeBytes,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk export failed");
            throw;
        }
    }

    public async Task<BulkUpdateResult> UpdateProductsAsync(BulkUpdateRequest request, CancellationToken ct = default)
    {
        var result = new BulkUpdateResult
        {
            UpdateId = Guid.NewGuid(),
            Status = "Completed"
        };

        try
        {
            foreach (var productId in request.ProductIds)
            {
                var product = await _productService.GetByIdAsync(productId, ct);
                if (product == null)
                {
                    result.FailedCount++;
                    continue;
                }

                ApplyBulkUpdate(product, request.OperationType, request.UpdateData);
                await _productService.UpdateAsync(product, ct);
                result.UpdatedCount++;
            }

            result.Summary = $"{result.UpdatedCount} products updated, {result.FailedCount} failed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk update failed");
            result.Status = "Failed";
            result.Summary = $"Error: {ex.Message}";
        }

        return result;
    }

    public async Task<ImportRowValidation> ValidateProductRowAsync(Dictionary<string, object> row, CancellationToken ct = default)
    {
        var validation = new ImportRowValidation { IsValid = true };

        // Name is required
        if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]?.ToString()))
        {
            validation.IsValid = false;
            validation.Errors.Add("Name is required.");
        }

        // SKU is required
        if (!row.ContainsKey("Sku") || string.IsNullOrWhiteSpace(row["Sku"]?.ToString()))
        {
            validation.IsValid = false;
            validation.Errors.Add("SKU is required.");
        }

        // Price must be positive
        if (row.ContainsKey("Price"))
        {
            if (!decimal.TryParse(row["Price"]?.ToString(), out var price) || price <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("Price must be a positive decimal value.");
            }
        }
        else
        {
            validation.IsValid = false;
            validation.Errors.Add("Price is required.");
        }

        // Stock must be non-negative
        if (row.ContainsKey("StockQuantity"))
        {
            if (!int.TryParse(row["StockQuantity"]?.ToString(), out var stock) || stock < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("StockQuantity must be a non-negative integer.");
            }
        }

        // Category validation
        if (row.ContainsKey("CategoryId"))
        {
            if (!int.TryParse(row["CategoryId"]?.ToString(), out var categoryId))
            {
                validation.IsValid = false;
                validation.Errors.Add("CategoryId must be a valid integer.");
            }
            else
            {
                var category = await _categoryService.GetByIdAsync(categoryId, ct);
                if (category == null)
                {
                    validation.Warnings.Add($"CategoryId {categoryId} not found; using default.");
                }
            }
        }

        return validation;
    }

    // Private helpers

    private List<Dictionary<string, object>> ParseCsvContent(string content, Dictionary<string, string>? fieldMapping = null)
    {
        var rows = new List<Dictionary<string, object>>();
        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord;
        if (headers == null) return rows;

        while (csv.Read())
        {
            var row = new Dictionary<string, object>();
            foreach (var header in headers)
            {
                var key = fieldMapping?.ContainsKey(header) == true ? fieldMapping[header] : header;
                row[key] = csv.GetField(header) ?? string.Empty;
            }
            rows.Add(row);
        }

        return rows;
    }

    private List<Dictionary<string, object>> ParseJsonContent(string content)
    {
        var rows = new List<Dictionary<string, object>>();
        var jsonArray = JsonSerializer.Deserialize<JsonElement>(content);

        if (jsonArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonArray.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var row = new Dictionary<string, object>();
                    foreach (var property in item.EnumerateObject())
                    {
                        row[property.Name] = property.Value.GetRawText();
                    }
                    rows.Add(row);
                }
            }
        }

        return rows;
    }

    private Product MapRowToProduct(ImportRowValidation validation, Guid vendorId)
    {
        // This would map CSV row to Product object
        // Implementation depends on your row structure
        return new Product();
    }

    private List<Product> FilterProducts(IReadOnlyList<Product> products, BulkExportRequest request)
    {
        var filtered = products.AsEnumerable();

        if (request.OnlyActive)
            filtered = filtered.Where(p => p.IsActive);

        if (request.CategoryIds?.Any() == true)
            filtered = filtered.Where(p => request.CategoryIds.Contains(p.CategoryId));

        return filtered.ToList();
    }

    private string ExportToCsv(List<Product> products, List<string>? fields = null)
    {
        var sb = new StringBuilder();
        // CSV export implementation
        sb.AppendLine("Id,Name,Sku,Price,StockQuantity,CategoryId,VendorId");

        foreach (var product in products)
        {
            sb.AppendLine($"{product.Id},{product.Name},{product.Sku},{product.Price},{product.StockQuantity},{product.CategoryId},{product.VendorId}");
        }

        return sb.ToString();
    }

    private string ExportToJson(List<Product> products, List<string>? fields = null)
    {
        return JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
    }

    private void ApplyBulkUpdate(Product product, string operationType, Dictionary<string, object>? updateData)
    {
        if (updateData == null) return;

        switch (operationType)
        {
            case "UpdatePrice":
                if (updateData.ContainsKey("priceAdjustmentPercent") &&
                    decimal.TryParse(updateData["priceAdjustmentPercent"]?.ToString(), out var percent))
                {
                    product.Price = product.Price * (1 + percent / 100);
                }
                if (updateData.ContainsKey("compareAtPrice") &&
                    decimal.TryParse(updateData["compareAtPrice"]?.ToString(), out var compareAt))
                {
                    product.CompareAtPrice = compareAt;
                }
                break;

            case "UpdateStock":
                if (updateData.ContainsKey("stockQuantity") &&
                    int.TryParse(updateData["stockQuantity"]?.ToString(), out var stock))
                {
                    product.StockQuantity = stock;
                }
                break;

            case "SetTags":
                if (updateData.ContainsKey("tags") && updateData["tags"] is List<string> tags)
                {
                    product.Tags = tags;
                }
                break;

            case "Activate":
                product.IsActive = true;
                break;

            case "Deactivate":
                product.IsActive = false;
                break;
        }
    }
}

// Request/Response DTOs
public class BulkImportRequest
{
    public string Format { get; set; } = "CSV";
    public string FileContent { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string Mode { get; set; } = "Merge";
    public bool TransactionMode { get; set; } = true;
    public Dictionary<string, string>? FieldMapping { get; set; }
}

public class BulkImportResult
{
    public Guid ImportId { get; set; }
    public string Status { get; set; } = "Processing";
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public Dictionary<int, List<string>> ErrorsByRow { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
}

public class BulkExportRequest
{
    public string Format { get; set; } = "CSV";
    public Guid VendorId { get; set; }
    public List<string>? Fields { get; set; }
    public bool OnlyActive { get; set; } = true;
    public List<int>? CategoryIds { get; set; }
    public bool IncludeVariants { get; set; } = true;
}

public class BulkExportResult
{
    public string DownloadUrl { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class BulkUpdateRequest
{
    public string OperationType { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public List<int> ProductIds { get; set; } = [];
    public Dictionary<string, object>? UpdateData { get; set; }
}

public class BulkUpdateResult
{
    public Guid UpdateId { get; set; }
    public string Status { get; set; } = "Completed";
    public int UpdatedCount { get; set; }
    public int FailedCount { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class ImportRowValidation
{
    public int RowNumber { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
