using HalalChain.Models;

namespace HalalChain.Services;

public interface IShipmentService
{
    Task<IReadOnlyList<Shipment>> GetAllAsync(CancellationToken ct = default);
    Task<Shipment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingAsync(string trackingNumber, CancellationToken ct = default);
    Task<Shipment> CreateAsync(Shipment shipment, CancellationToken ct = default);
    Task<Shipment> UpdateStatusAsync(int id, ShipmentStatus status, CancellationToken ct = default);
}
