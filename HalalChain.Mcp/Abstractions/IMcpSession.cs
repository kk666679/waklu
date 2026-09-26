using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

/// <summary>
/// Per-connection handshake state. One instance per server process; the
/// negotiation result is produced by <c>initialize</c> and every later feature
/// gate reads from here.
/// </summary>
public interface IMcpSession
{
    /// <summary>Revision agreed with the client, or <c>null</c> pre-handshake.</summary>
    string? ProtocolVersion { get; }

    /// <summary>Capabilities the client declared. Empty before the handshake.</summary>
    ClientCapabilities Capabilities { get; }

    /// <summary>True once <c>initialize</c> has completed.</summary>
    bool IsInitialized { get; }

    /// <summary>Client identity reported during the handshake.</summary>
    ClientIdentity? Client { get; }

    /// <summary>Records the negotiated revision and client capabilities.</summary>
    void CompleteInitialize(string protocolVersion, ClientCapabilities capabilities, ClientIdentity? client);
}

public sealed class ClientIdentity
{
    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public string? Title { get; set; }
}
