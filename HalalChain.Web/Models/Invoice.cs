namespace HalalChain.Models;

public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    Overdue,
    Cancelled
}

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
