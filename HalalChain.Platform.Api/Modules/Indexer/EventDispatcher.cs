using Nethereum.RPC.Eth.DTOs;
using HalalChain.Domain.Blockchain;
using HalalChain.Platform.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HalalChain.Platform.Api.Modules.Indexer;

/// <summary>
/// Decodes a chain log and dispatches it to the appropriate handler
/// based on the event signature (topic[0]). The indexer in MVP is
/// "shape-aware": it inspects the log's <c>Topics</c> array and the
/// contract address to route the event.
///
/// In Phase 3 this becomes a full ABI-decoded indexer that
/// auto-generates handler methods from the contract's event ABI.
/// </summary>
public interface IEventDispatcher
{
    Task DispatchAsync(FilterLog log, CancellationToken ct);
}

public sealed class EventDispatcher : IEventDispatcher
{
    private static readonly string SupplierRegisteredTopic      = "0x" + Keccak256("SupplierRegistered(bytes32,address,string,string,uint64)");
    private static readonly string SupplierStatusChangedTopic   = "0x" + Keccak256("SupplierStatusChanged(bytes32,uint8,uint64)");
    private static readonly string ProductRegisteredTopic       = "0x" + Keccak256("ProductRegistered(bytes32,bytes32,bytes32,string,uint64)");
    private static readonly string ProductCurrentCertUpdatedTopic = "0x" + Keccak256("ProductCurrentCertUpdated(bytes32,bytes32)");
    private static readonly string ProductRecalledTopic          = "0x" + Keccak256("ProductRecalled(bytes32,string,uint64)");
    private static readonly string CertificateIssuedTopic        = "0x" + Keccak256("CertificateIssued(bytes32,bytes32,address,string,uint64,uint64,string)");
    private static readonly string CertificateRevokedTopic      = "0x" + Keccak256("CertificateRevoked(bytes32,string,uint64)");
    private static readonly string TraceabilityEventRecordedTopic = "0x" + Keccak256("TraceabilityEventRecorded(bytes32,bytes32,bytes32,uint8,address,uint64)");

    private readonly HalalChainDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(HalalChainDbContext db, IConfiguration config, ILogger<EventDispatcher> logger)
    {
        _db = db; _config = config; _logger = logger;
    }

    public async Task DispatchAsync(FilterLog log, CancellationToken ct)
    {
        if (log.Topics is null || log.Topics.Length == 0) return;
        var sig = log.Topics[0] as string;
        if (string.IsNullOrEmpty(sig)) return;
        try
        {
            if (string.Equals(sig, SupplierRegisteredTopic, StringComparison.OrdinalIgnoreCase))
                await HandleSupplierRegisteredAsync(log, ct);
            else if (string.Equals(sig, SupplierStatusChangedTopic, StringComparison.OrdinalIgnoreCase))
                await HandleSupplierStatusChangedAsync(log, ct);
            else if (string.Equals(sig, ProductRegisteredTopic, StringComparison.OrdinalIgnoreCase))
                await HandleProductRegisteredAsync(log, ct);
            else if (string.Equals(sig, ProductCurrentCertUpdatedTopic, StringComparison.OrdinalIgnoreCase))
                await HandleProductCurrentCertUpdatedAsync(log, ct);
            else if (string.Equals(sig, ProductRecalledTopic, StringComparison.OrdinalIgnoreCase))
                await HandleProductRecalledAsync(log, ct);
            else if (string.Equals(sig, CertificateIssuedTopic, StringComparison.OrdinalIgnoreCase))
                await HandleCertificateIssuedAsync(log, ct);
            else if (string.Equals(sig, CertificateRevokedTopic, StringComparison.OrdinalIgnoreCase))
                await HandleCertificateRevokedAsync(log, ct);
            else if (string.Equals(sig, TraceabilityEventRecordedTopic, StringComparison.OrdinalIgnoreCase))
                await HandleTraceabilityEventRecordedAsync(log, ct);
            else
                _logger.LogDebug("Unknown event topic {Topic} from {Address}", sig, log.Address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log {Tx}:{Log}", log.TransactionHash, log.LogIndex?.Value);
        }
    }

    private async Task HandleSupplierRegisteredAsync(FilterLog log, CancellationToken ct)
    {
        var supplierId = log.Topics[1] as string ?? string.Empty;
        var wallet = "0x" + ((log.Topics[2] as string ?? string.Empty).Substring((log.Topics[2] as string ?? string.Empty).Length - 40));
        var (ipfsCid, jurisdiction, registeredAt) = DecodeSupplierRegisteredData(log.Data);
        _db.SuppliersOnChain.Add(new SupplierOnChain
        {
            Id = Guid.NewGuid(), SupplierId = supplierId, Wallet = wallet,
            IpfsMetadataCid = ipfsCid, Jurisdiction = jurisdiction,
            RegisteredAtUnix = (long)registeredAt, UpdatedAtUnix = (long)registeredAt,
            Status = 0,
            TxHash = log.TransactionHash, BlockNumber = (int)log.BlockNumber.Value,
            IndexedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Indexed SupplierRegistered: {SupplierId} wallet={Wallet}", supplierId, wallet);
    }

    private async Task HandleSupplierStatusChangedAsync(FilterLog log, CancellationToken ct)
    {
        var supplierId = log.Topics[1] as string ?? string.Empty;
        var newStatus = DecodeUint256(log.Data);
        var s = await _db.SuppliersOnChain.FirstOrDefaultAsync(x => x.SupplierId == supplierId, ct);
        if (s is null) return;
        s.Status = (int)newStatus;
        s.UpdatedAtUnix = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleProductRegisteredAsync(FilterLog log, CancellationToken ct)
    {
        var productId = log.Topics[1] as string ?? string.Empty;
        var supplierId = log.Topics[2] as string ?? string.Empty;
        var metadataHash = log.Topics[3] as string ?? string.Empty;
        var (ipfsCid, jurisdiction, registeredAt) = DecodeProductRegisteredData(log.Data);
        _db.ProductsOnChain.Add(new ProductOnChain
        {
            Id = Guid.NewGuid(), ProductId = productId, SupplierId = supplierId,
            MetadataHashHex = metadataHash, IpfsCid = ipfsCid, Jurisdiction = jurisdiction,
            CurrentCertId = "", RegisteredAtUnix = (long)registeredAt, UpdatedAtUnix = (long)registeredAt,
            Status = 0,
            TxHash = log.TransactionHash, BlockNumber = (int)log.BlockNumber.Value,
            IndexedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleProductCurrentCertUpdatedAsync(FilterLog log, CancellationToken ct)
    {
        var productId = log.Topics[1] as string ?? string.Empty;
        var certId = log.Topics[2] as string ?? string.Empty;
        var p = await _db.ProductsOnChain.FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        if (p is null) return;
        p.CurrentCertId = certId;
        p.UpdatedAtUnix = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (certId != "0x" + new string('0', 64)) p.Status = 1;
        else if (p.Status == 1) p.Status = 0;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleProductRecalledAsync(FilterLog log, CancellationToken ct)
    {
        var productId = log.Topics[1] as string ?? string.Empty;
        var (reasonCid, recalledAt) = DecodeProductRecalledData(log.Data);
        var p = await _db.ProductsOnChain.FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        if (p is null) return;
        p.Status = 3;
        p.CurrentCertId = "";
        p.UpdatedAtUnix = (long)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Product RECALLED on-chain: {ProductId} reason={Reason}", productId, reasonCid);
    }

    private async Task HandleCertificateIssuedAsync(FilterLog log, CancellationToken ct)
    {
        var certId = log.Topics[1] as string ?? string.Empty;
        var productId = log.Topics[2] as string ?? string.Empty;
        var topic3 = log.Topics[3] as string ?? string.Empty;
        var certifier = "0x" + topic3.Substring(topic3.Length - 40);
        var (docCid, issuedAt, expiresAt, scopeCid, country) = DecodeCertificateIssuedData(log.Data);
        _db.CertificatesOnChain.Add(new CertificateOnChain
        {
            Id = Guid.NewGuid(), CertId = certId, ProductId = productId, Certifier = certifier,
            DocumentCid = docCid, IssuedAtUnix = (long)issuedAt, ExpiresAtUnix = (long)expiresAt,
            ScopeCid = scopeCid, Country = country, Status = 0,
            TxHash = log.TransactionHash, BlockNumber = (int)log.BlockNumber.Value,
            IndexedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleCertificateRevokedAsync(FilterLog log, CancellationToken ct)
    {
        var certId = log.Topics[1] as string ?? string.Empty;
        var (reasonCid, revokedAt) = DecodeCertificateRevokedData(log.Data);
        var c = await _db.CertificatesOnChain.FirstOrDefaultAsync(x => x.CertId == certId, ct);
        if (c is null) return;
        c.Status = 1;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleTraceabilityEventRecordedAsync(FilterLog log, CancellationToken ct)
    {
        var eventId = log.Topics[1] as string ?? string.Empty;
        var productId = log.Topics[2] as string ?? string.Empty;
        var batchId = log.Topics[3] as string ?? string.Empty;
        var (eventType, actor, locationCid, evidenceCid, notesCid, timestamp) = DecodeTraceabilityData(log.Data);
        _db.TraceabilityEventsOnChain.Add(new TraceabilityEventOnChain
        {
            Id = Guid.NewGuid(), EventId = eventId, ProductId = productId, BatchId = batchId,
            Actor = actor, EventType = (int)eventType, LocationCid = locationCid,
            EvidenceCid = evidenceCid, NotesCid = notesCid, TimestampUnix = (long)timestamp,
            TxHash = log.TransactionHash, BlockNumber = (int)log.BlockNumber.Value,
            LogIndex = (int)log.LogIndex.Value, IndexedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static (string IpfsCid, string Jurisdiction, ulong RegisteredAt) DecodeSupplierRegisteredData(string data)
    {
        var s1Offset = DecodeUint(data, 0);
        var s2Offset = DecodeUint(data, 1);
        var registeredAt = DecodeUint(data, 2);
        var ipfsCid = DecodeStringAt(data, (int)s1Offset);
        var jurisdiction = DecodeStringAt(data, (int)s2Offset);
        return (ipfsCid, jurisdiction, registeredAt);
    }

    private static (string IpfsCid, string Jurisdiction, ulong RegisteredAt) DecodeProductRegisteredData(string data)
        => DecodeSupplierRegisteredData(data);

    private static (string ReasonCid, ulong RecalledAt) DecodeProductRecalledData(string data)
    {
        var sOffset = DecodeUint(data, 0);
        var recalledAt = DecodeUint(data, 1);
        return (DecodeStringAt(data, (int)sOffset), recalledAt);
    }

    private static (string DocCid, ulong IssuedAt, ulong ExpiresAt, string ScopeCid, string Country) DecodeCertificateIssuedData(string data)
    {
        var s1 = DecodeUint(data, 0);
        var issued = DecodeUint(data, 1);
        var expires = DecodeUint(data, 2);
        var s2 = DecodeUint(data, 3);
        var s3 = DecodeUint(data, 4);
        return (DecodeStringAt(data, (int)s1), issued, expires, DecodeStringAt(data, (int)s2), DecodeStringAt(data, (int)s3));
    }

    private static (string ReasonCid, ulong RevokedAt) DecodeCertificateRevokedData(string data)
        => DecodeProductRecalledData(data);

    private static (ulong EventType, string Actor, string LocationCid, string EvidenceCid, string NotesCid, ulong Timestamp) DecodeTraceabilityData(string data)
    {
        var s1 = DecodeUint(data, 0);
        var s2 = DecodeUint(data, 1);
        var s3 = DecodeUint(data, 2);
        var eventType = DecodeUint(data, 3);
        var timestamp = DecodeUint(data, 4);
         return (eventType, "0x", DecodeStringAt(data, (int)s1), DecodeStringAt(data, (int)s2), DecodeStringAt(data, (int)s3), timestamp);
     }

    private static ulong DecodeUint256(string data)
    {
        if (string.IsNullOrEmpty(data)) return 0;
        var hex = data.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? data[2..] : data;
        if (hex.Length < 64) return 0;
        return Convert.ToUInt64(hex.Substring(0, 64), 16);
    }

    private static ulong DecodeUint(string data, int wordIndex)
    {
        if (string.IsNullOrEmpty(data)) return 0;
        var hex = data.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? data[2..] : data;
        var start = wordIndex * 64;
        if (start + 64 > hex.Length) return 0;
        return Convert.ToUInt64(hex.Substring(start, 64), 16);
    }

    private static string DecodeStringAt(string data, int offset)
    {
        var hex = data.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? data[2..] : data;
        var startByte = offset;
        var lengthStart = startByte * 2;
        if (lengthStart + 64 > hex.Length) return string.Empty;
        var length = (int)Convert.ToUInt64(hex.Substring(lengthStart, 64), 16);
        var dataStart = (startByte + 32) * 2;
        var dataHex = hex.Substring(dataStart, length * 2);
        var raw = Convert.FromHexString(dataHex);
        return System.Text.Encoding.UTF8.GetString(raw);
    }

    private static string Keccak256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = Nethereum.Util.Sha3Keccack.Current.CalculateHash(bytes);
        return "0x" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
