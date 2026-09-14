using HalalChain.Models;

namespace HalalChain.Services;

public class ShipmentService : IShipmentService
{
    private readonly List<Shipment> _shipments = new();
    private int _nextId = 1;

    public ShipmentService()
    {
        _shipments.Add(new Shipment { Id = _nextId++, TrackingNumber = "TRK-1001", OrderId = 1, Carrier = "FedEx", Status = ShipmentStatus.InTransit });
        _shipments.Add(new Shipment { Id = _nextId++, TrackingNumber = "TRK-1002", OrderId = 2, Carrier = "UPS", Status = ShipmentStatus.Delivered, DeliveredAt = DateTime.UtcNow.AddDays(-1) });
    }

    public Task<IReadOnlyList<Shipment>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Shipment>>(_shipments.ToList());

    public Task<Shipment?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_shipments.FirstOrDefault(s => s.Id == id));

    public Task<Shipment?> GetByTrackingAsync(string trackingNumber, CancellationToken ct = default)
        => Task.FromResult(_shipments.FirstOrDefault(s => s.TrackingNumber == trackingNumber));

    public Task<Shipment> CreateAsync(Shipment shipment, CancellationToken ct = default)
    {
        shipment.Id = _nextId++;
        shipment.TrackingNumber = $"TRK-{1000 + shipment.Id}";
        _shipments.Add(shipment);
        return Task.FromResult(shipment);
    }

    public Task<Shipment> UpdateStatusAsync(int id, ShipmentStatus status, CancellationToken ct = default)
    {
        var existing = _shipments.FirstOrDefault(s => s.Id == id);
        if (existing is not null)
        {
            existing.Status = status;
            if (status == ShipmentStatus.InTransit && existing.ShippedAt is null) existing.ShippedAt = DateTime.UtcNow;
            if (status == ShipmentStatus.Delivered) existing.DeliveredAt = DateTime.UtcNow;
        }
        return Task.FromResult(existing ?? new Shipment { Id = id });
    }
}
