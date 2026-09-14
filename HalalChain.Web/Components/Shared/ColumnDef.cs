using Microsoft.AspNetCore.Components;

namespace HalalChain.Components.Shared;

public class ColumnDef<T>
{
    public string Header { get; set; } = "";

    /// <summary>
    /// Property name on T used for default rendering, sorting, and filtering.
    /// </summary>
    public string? Property { get; set; }

    /// <summary>
    /// Whether the column should be sortable in the data grid.
    /// </summary>
    public bool Sortable { get; set; }

    /// <summary>
    /// Whether the column is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    public Func<T, RenderFragment> Template { get; set; } = _ => builder => { };

    /// <summary>
    /// Optional sort key function. If null, column is not sortable.
    /// Returns the value to sort by (e.g., p => p.Price).
    /// </summary>
    public Func<T, IComparable?>? SortBy { get; set; }

    /// <summary>
    /// Whether this column should be included in CSV export.
    /// </summary>
    public bool Exportable { get; set; } = true;

    /// <summary>
    /// Custom export formatter. If null, uses ToString() on the SortBy value.
    /// </summary>
    public Func<T, string>? ExportFormat { get; set; }

    /// <summary>
    /// Optional search value extractor for global search filtering.
    /// If null, falls back to rendering the Template as plain text.
    /// </summary>
    public Func<T, string>? SearchValue { get; set; }

    /// <summary>
    /// CSS class to apply to the header cell.
    /// </summary>
    public string? HeaderClass { get; set; }

    /// <summary>
    /// CSS class to apply to the body cell.
    /// </summary>
    public string? CellClass { get; set; }
}
