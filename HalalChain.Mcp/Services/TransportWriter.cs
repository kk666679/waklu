using System.Text.Json;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Serialises every frame written to the stdio transport behind one gate.
///
/// The JSON-RPC stream is a single byte channel. The main loop writes
/// responses while tools may concurrently emit progress notifications and
/// log messages, so without a gate two frames can interleave mid-line and
/// desynchronise the client. Owning the gate here — rather than in each
/// writer — is what makes that guarantee hold for all of them.
/// </summary>
public sealed class TransportWriter
{
    private readonly TextWriter _inner;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public TransportWriter(TextWriter inner) => _inner = inner;

    /// <summary>Writes one newline-delimited frame and flushes it.</summary>
    public async Task WriteFrameAsync(string json, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _inner.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
            await _inner.FlushAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Serialises <paramref name="payload"/> and writes it as one frame.</summary>
    public Task WriteFrameAsync<T>(T payload, CancellationToken ct = default) =>
        WriteFrameAsync(JsonSerializer.Serialize(payload, McpJson.Options), ct);
}
