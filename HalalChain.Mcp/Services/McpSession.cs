using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Handshake state for the current process. The server is a short-lived
/// console host speaking to exactly one client over stdio, so a singleton
/// is the correct lifetime.
/// </summary>
internal sealed class McpSession : IMcpSession
{
    private readonly object _gate = new();

    private string? _protocolVersion;
    private ClientCapabilities _capabilities = new();
    private ClientIdentity? _client;

    public string? ProtocolVersion
    {
        get { lock (_gate) { return _protocolVersion; } }
    }

    public ClientCapabilities Capabilities
    {
        get { lock (_gate) { return _capabilities; } }
    }

    public ClientIdentity? Client
    {
        get { lock (_gate) { return _client; } }
    }

    public bool IsInitialized
    {
        get { lock (_gate) { return _protocolVersion is not null; } }
    }

    public void CompleteInitialize(string protocolVersion, ClientCapabilities capabilities, ClientIdentity? client)
    {
        lock (_gate)
        {
            _protocolVersion = protocolVersion;
            _capabilities = capabilities ?? new ClientCapabilities();
            _client = client;
        }
    }
}
