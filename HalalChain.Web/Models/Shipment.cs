namespace HalalChain.Models;

public enum ShipmentStatus
{
    Pending,
    Picked,
    InTransit,
    Delivered,
    Returned
}

public class Shipment
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public string? ShippingAddress { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }
}
