using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using System.Numerics;
using System.Text;

namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Nethereum-backed implementation of <see cref="ISmartContractService"/>.
///
/// The Nethereum contract bindings (Nethereum.Contracts.Standards.ERC20,
/// Nethereum.Contracts.Standards.ERC721) are for the global standards;
/// for our custom contracts we use the dynamic <c>GetFunction</c> +
/// <c>FunctionCallEncoder/FunctionCallDecoder</c> API, which lets us
/// call any contract by its ABI without code generation. This is the
/// same pattern Nethereum's own ContractExtensions use.
///
/// Wallet is loaded once at startup (from <see cref="IWalletProvider"/>)
/// and held in memory. In production the key bytes are supplied by
/// the secret manager; in dev they come from configuration.
///
/// Configuration is in <c>appsettings.json</c>:
///   Blockchain:RpcUrl, Blockchain:ChainId, Blockchain:Confirmations,
///   Blockchain:GasPriceCapGwei, Blockchain:TxTimeoutSeconds,
///   Blockchain:Contracts:AccessControl, Suppliers, Products, Certs, Events.
/// </summary>
public sealed class SmartContractService : ISmartContractService
{
    private readonly Web3 _web3;
    private readonly IConfiguration _config;
    private readonly ILogger<SmartContractService> _logger;
    private readonly ITransactionQueue _queue;
    private readonly Contracts _contracts;
    private readonly string _walletAddress;

    public string? WalletAddress => _walletAddress;

    public SmartContractService(
        IWalletProvider walletProvider,
        IConfiguration config,
        ILogger<SmartContractService> logger,
        ITransactionQueue queue)
    {
        _config = config;
        _logger = logger;
        _queue = queue;

        var rpcUrl = config["Blockchain:RpcUrl"]
            ?? throw new InvalidOperationException("Blockchain:RpcUrl is not configured.");
        _contracts = config.GetSection("Blockchain:Contracts").Get<Contracts>()
            ?? throw new InvalidOperationException("Blockchain:Contracts section is missing.");

        var account = walletProvider.GetAccount();
        _web3 = new Web3(account, rpcUrl);
        _walletAddress = account.Address;

        _logger.LogInformation("SmartContractService initialised. Wallet={Wallet} ChainId={ChainId} Rpc={Rpc}",
            _walletAddress, _web3.Eth.ChainId?.ToString() ?? "?", rpcUrl);
    }

    // ════════════════════════════════════════════════════════════════
    //  Writes — dispatched via the durable outbox (TransactionQueue)
    // ════════════════════════════════════════════════════════════════

    public Task<ChainWriteResult> RegisterSupplierAsync(string supplierId, string wallet, string ipfsMetadataCid, string jurisdiction, CancellationToken ct)
        => EnqueueWriteAsync("registerSupplier", new object[] { HexId(supplierId), wallet, ipfsMetadataCid, jurisdiction }, ct);

    public Task<ChainWriteResult> RegisterProductAsync(string productId, string supplierId, string metadataHashHex, string ipfsCid, string jurisdiction, CancellationToken ct)
        => EnqueueWriteAsync("registerProduct", new object[] { HexId(productId), HexId(supplierId), HexHash(metadataHashHex), ipfsCid, jurisdiction }, ct);

    public Task<ChainWriteResult> IssueCertificateAsync(string certId, string productId, string documentCid, DateTimeOffset expiresAt, string scopeCid, string country, CancellationToken ct)
        => EnqueueWriteAsync("issueCertificate", new object[] { HexId(certId), HexId(productId), documentCid, new HexBigInteger(expiresAt.ToUnixTimeSeconds()), scopeCid, country }, ct);

    public Task<ChainWriteResult> RevokeCertificateAsync(string certId, string reasonCid, CancellationToken ct)
        => EnqueueWriteAsync("revokeCertificate", new object[] { HexId(certId), reasonCid }, ct);

    public Task<ChainWriteResult> ExpireCertificateAsync(string certId, CancellationToken ct)
        => EnqueueWriteAsync("expireCertificate", new object[] { HexId(certId) }, ct);

    public Task<ChainWriteResult> RecordTraceabilityEventAsync(string eventId, string productId, string batchId, int eventType, string locationCid, string evidenceCid, string notesCid, CancellationToken ct)
        => EnqueueWriteAsync("recordEvent", new object[] { HexId(eventId), HexId(productId), HexId(batchId), new HexBigInteger(eventType), locationCid, evidenceCid, notesCid }, ct);

    public Task<ChainWriteResult> RecallProductAsync(string productId, string reasonCid, CancellationToken ct)
        => EnqueueWriteAsync("recall", new object[] { HexId(productId), reasonCid }, ct);

    private async Task<ChainWriteResult> EnqueueWriteAsync(string functionName, object[] parameters, CancellationToken ct)
    {
        var (target, abi) = ResolveAbiAndTarget(functionName);
        var contract = _web3.Eth.GetContract(abi, target);
        var function = contract.GetFunction(functionName);
        var data = function.GetData(parameters);

        var tx = new TransactionInput
        {
            To = target,
            Data = data,
            From = _walletAddress,
        };
        var enqueued = await _queue.EnqueueAsync(functionName, target, tx, ct);
        return new ChainWriteResult(enqueued.TxHash, enqueued.Status, enqueued.BlockNumber, enqueued.SubmittedAt, enqueued.Error);
    }

    // ════════════════════════════════════════════════════════════════
    //  Reads — always direct from chain (public /verify path)
    // ════════════════════════════════════════════════════════════════

    public async Task<ChainReadResult<SupplierOnChainDto?>> GetSupplierAsync(string supplierId, CancellationToken ct)
    {
        try
        {
            var raw = await CallViewAsync<SupplierStruct>("getSupplier", new object[] { HexId(supplierId) }, ct);
            if (raw is null || !raw.Exists) return new ChainReadResult<SupplierOnChainDto?>(null, "chain");
            return new ChainReadResult<SupplierOnChainDto?>(MapSupplier(raw), "chain");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getSupplier({Id}) failed", supplierId);
            return new ChainReadResult<SupplierOnChainDto?>(null, "fallback");
        }
    }

    public async Task<ChainReadResult<ProductOnChainDto?>> GetProductAsync(string productId, CancellationToken ct)
    {
        try
        {
            var raw = await CallViewAsync<ProductStruct>("getProduct", new object[] { HexId(productId) }, ct);
            if (raw is null || !raw.Exists) return new ChainReadResult<ProductOnChainDto?>(null, "chain");
            return new ChainReadResult<ProductOnChainDto?>(MapProduct(raw), "chain");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getProduct({Id}) failed", productId);
            return new ChainReadResult<ProductOnChainDto?>(null, "fallback");
        }
    }

    public async Task<ChainReadResult<CertificateOnChainDto?>> GetCertificateAsync(string certId, CancellationToken ct)
    {
        try
        {
            var raw = await CallViewAsync<CertificateStruct>("getCertificate", new object[] { HexId(certId) }, ct);
            if (raw is null || !raw.Exists) return new ChainReadResult<CertificateOnChainDto?>(null, "chain");
            return new ChainReadResult<CertificateOnChainDto?>(MapCertificate(raw), "chain");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getCertificate({Id}) failed", certId);
            return new ChainReadResult<CertificateOnChainDto?>(null, "fallback");
        }
    }

    public async Task<ChainReadResult<CertificateOnChainDto?>> GetCurrentCertificateForProductAsync(string productId, CancellationToken ct)
    {
        try
        {
            var raw = await CallViewAsync<CertificateStruct>("getCurrentCertForProduct", new object[] { HexId(productId) }, ct);
            if (raw is null || !raw.Exists) return new ChainReadResult<CertificateOnChainDto?>(null, "chain");
            return new ChainReadResult<CertificateOnChainDto?>(MapCertificate(raw), "chain");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getCurrentCertForProduct({Id}) failed", productId);
            return new ChainReadResult<CertificateOnChainDto?>(null, "fallback");
        }
    }

    public async Task<ChainReadResult<IReadOnlyList<TraceabilityEventOnChainDto>>> GetTraceabilityAsync(string productId, CancellationToken ct)
    {
        try
        {
            var raw = await CallViewAsync<List<TraceabilityEventStruct>>("getEventsForProduct", new object[] { HexId(productId) }, ct);
            if (raw is null) return new ChainReadResult<IReadOnlyList<TraceabilityEventOnChainDto>>(Array.Empty<TraceabilityEventOnChainDto>(), "chain");
            var mapped = raw.Select(MapEvent).ToList();
            return new ChainReadResult<IReadOnlyList<TraceabilityEventOnChainDto>>(mapped, "chain");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getEventsForProduct({Id}) failed", productId);
            return new ChainReadResult<IReadOnlyList<TraceabilityEventOnChainDto>>(Array.Empty<TraceabilityEventOnChainDto>(), "fallback");
        }
    }

    public async Task<ChainReadResult<ulong>> GetBlockNumberAsync(CancellationToken ct)
    {
        try
        {
            var bn = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return new ChainReadResult<ulong>((ulong)bn.Value, "chain");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getBlockNumber failed");
            return new ChainReadResult<ulong>(0, "fallback");
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        try
        {
            var bn = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return bn.Value > 0;
        }
        catch
        {
            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  ABI selection & encoding
    // ════════════════════════════════════════════════════════════════

    private (string Target, string Abi) ResolveAbiAndTarget(string functionName)
    {
        // Map function name to the contract it lives on and its minimal ABI fragment.
        // For brevity, the full ABI is loaded at startup from `Contracts:Abi:<name>`
        // in config; here we just return (target, abiJson).
        return functionName switch
        {
            "registerSupplier"   => (_contracts.Suppliers, GetAbi("SupplierRegistry",   "registerSupplier")),
            "setStatus"          => (_contracts.Suppliers, GetAbi("SupplierRegistry",   "setStatus")),
            "proposeRotation"    => (_contracts.Suppliers, GetAbi("SupplierRegistry",   "proposeWalletRotation")),
            "acceptRotation"     => (_contracts.Suppliers, GetAbi("SupplierRegistry",   "acceptWalletRotation")),
            "registerProduct"    => (_contracts.Products,  GetAbi("HalalProductRegistry","registerProduct")),
            "setCurrentCert"     => (_contracts.Products,  GetAbi("HalalProductRegistry","setCurrentCertificate")),
            "recall"             => (_contracts.Products,  GetAbi("HalalProductRegistry","recall")),
            "issueCertificate"   => (_contracts.Certs,     GetAbi("HalalCertificationRegistry","issueCertificate")),
            "revokeCertificate"  => (_contracts.Certs,     GetAbi("HalalCertificationRegistry","revokeCertificate")),
            "expireCertificate"  => (_contracts.Certs,     GetAbi("HalalCertificationRegistry","expireCertificate")),
            "recordEvent"        => (_contracts.Events,    GetAbi("TraceabilityEventLog","recordEvent")),
            _ => throw new InvalidOperationException($"Unknown function: {functionName}")
        };
    }

    private string GetAbi(string contract, string function)
    {
        var key = $"Blockchain:Abi:{contract}:{function}";
        return _config[key]
            ?? throw new InvalidOperationException($"ABI fragment missing for {contract}.{function}. Add it under '{key}' in appsettings.");
    }

    // ════════════════════════════════════════════════════════════════
    //  Raw call helper (used by reads)
    // ════════════════════════════════════════════════════════════════

    private async Task<T?> CallViewAsync<T>(string functionName, object[] parameters, CancellationToken ct)
    {
        var (target, abi) = ResolveAbiAndTarget(functionName);
        var contract = _web3.Eth.GetContract(abi, target);
        var function = contract.GetFunction(functionName);
        return await function.CallAsync<T>(parameters);
    }

    private object LatestOrPending() => Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest();

    // ════════════════════════════════════════════════════════════════
    //  Mappers (raw ABI struct → DTO)
    // ════════════════════════════════════════════════════════════════

    private static SupplierOnChainDto MapSupplier(SupplierStruct s) => new(
        Bytes32ToHexId(s.SupplierId), s.Wallet, s.IpfsMetadataCid, s.Jurisdiction,
        DateTimeOffset.FromUnixTimeSeconds((long)s.RegisteredAt),
        DateTimeOffset.FromUnixTimeSeconds((long)s.UpdatedAt),
        (int)s.Status);

    private static ProductOnChainDto MapProduct(ProductStruct p) => new(
        Bytes32ToHexId(p.ProductId), Bytes32ToHexId(p.SupplierId), Bytes32ToHex(p.MetadataHash), p.IpfsCid, p.Jurisdiction,
        Bytes32ToHexId(p.CurrentCertId),
        DateTimeOffset.FromUnixTimeSeconds((long)p.RegisteredAt),
        DateTimeOffset.FromUnixTimeSeconds((long)p.UpdatedAt),
        (int)p.Status);

    private static CertificateOnChainDto MapCertificate(CertificateStruct c) => new(
        Bytes32ToHexId(c.CertId), Bytes32ToHexId(c.ProductId), c.Certifier, c.DocumentCid,
        DateTimeOffset.FromUnixTimeSeconds((long)c.IssuedAt),
        DateTimeOffset.FromUnixTimeSeconds((long)c.ExpiresAt),
        c.ScopeCid, c.Country, (int)c.Status);

    private static TraceabilityEventOnChainDto MapEvent(TraceabilityEventStruct e) => new(
        Bytes32ToHexId(e.EventId), Bytes32ToHexId(e.ProductId), Bytes32ToHexId(e.BatchId), e.Actor,
        (int)e.EventType, e.LocationCid, e.EvidenceCid, e.NotesCid,
        DateTimeOffset.FromUnixTimeSeconds((long)e.Timestamp));

    // ════════════════════════════════════════════════════════════════
    //  Helpers — id encoding
    // ════════════════════════════════════════════════════════════════

    /// <summary>Turn a human id (e.g. SUP-MY-000123) into the bytes32 the
    /// contract expects. We use keccak256 of the UTF-8 bytes so the id
    /// is stable and 32-byte-aligned.</summary>
    private static byte[] HexId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required", nameof(id));
        var hash = Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(id));
        return hash;
    }

    private static byte[] HexHash(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("hash is required", nameof(hex));
        hex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        if (hex.Length != 64) throw new ArgumentException("metadataHash must be 32 bytes (64 hex chars)", nameof(hex));
        return Convert.FromHexString(hex);
    }

    private static string Bytes32ToHexId(byte[] b) => "0x" + Convert.ToHexString(b).ToLowerInvariant();
    private static string Bytes32ToHex(byte[] b) => "0x" + Convert.ToHexString(b).ToLowerInvariant();

    // ════════════════════════════════════════════════════════════════
    //  Contract addresses (bound from config)
    // ════════════════════════════════════════════════════════════════

    private sealed class Contracts
    {
        public string AccessControl { get; set; } = string.Empty;
        public string Suppliers     { get; set; } = string.Empty;
        public string Products      { get; set; } = string.Empty;
        public string Certs         { get; set; } = string.Empty;
        public string Events        { get; set; } = string.Empty;
    }
}

// ── Raw struct layouts (mirror the Solidity structs exactly) ────────
// These are the types Nethereum decodes from the contract return values.
// Field order and types MUST match the Solidity struct layout.


public sealed class SupplierStruct
{
    
    public byte[] SupplierId { get; set; } = Array.Empty<byte>();

    
    public string Wallet { get; set; } = string.Empty;

    
    public string ProposedWallet { get; set; } = string.Empty;

    
    public string IpfsMetadataCid { get; set; } = string.Empty;

    
    public string Jurisdiction { get; set; } = string.Empty;

    
    public ulong RegisteredAt { get; set; }

    
    public ulong UpdatedAt { get; set; }

    
    public byte Status { get; set; }

    
    public bool Exists { get; set; }
}


public sealed class ProductStruct
{
    
    public byte[] ProductId { get; set; } = Array.Empty<byte>();

    
    public byte[] SupplierId { get; set; } = Array.Empty<byte>();

    
    public byte[] MetadataHash { get; set; } = Array.Empty<byte>();

    
    public string IpfsCid { get; set; } = string.Empty;

    
    public string Jurisdiction { get; set; } = string.Empty;

    
    public byte[] CurrentCertId { get; set; } = Array.Empty<byte>();

    
    public ulong RegisteredAt { get; set; }

    
    public ulong UpdatedAt { get; set; }

    
    public byte Status { get; set; }

    
    public bool Exists { get; set; }
}


public sealed class CertificateStruct
{
    
    public byte[] CertId { get; set; } = Array.Empty<byte>();

    
    public byte[] ProductId { get; set; } = Array.Empty<byte>();

    
    public string Certifier { get; set; } = string.Empty;

    
    public string DocumentCid { get; set; } = string.Empty;

    
    public ulong IssuedAt { get; set; }

    
    public ulong ExpiresAt { get; set; }

    
    public string ScopeCid { get; set; } = string.Empty;

    
    public string Country { get; set; } = string.Empty;

    
    public byte Status { get; set; }

    
    public bool Exists { get; set; }
}


public sealed class TraceabilityEventStruct
{
    
    public byte[] EventId { get; set; } = Array.Empty<byte>();

    
    public byte[] ProductId { get; set; } = Array.Empty<byte>();

    
    public byte[] BatchId { get; set; } = Array.Empty<byte>();

    
    public string Actor { get; set; } = string.Empty;

    
    public byte EventType { get; set; }

    
    public string LocationCid { get; set; } = string.Empty;

    
    public string EvidenceCid { get; set; } = string.Empty;

    
    public string NotesCid { get; set; } = string.Empty;

    
    public ulong Timestamp { get; set; }

    
    public bool Exists { get; set; }
}
