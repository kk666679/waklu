namespace HalalChain.Models;

public enum PayoutStatus
{
    Pending,
    Approved,
    Paid,
    Rejected
}

public class Payout
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
