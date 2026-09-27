using System.Text.Json;
using HalalChain.Application.Storage;
using Microsoft.Extensions.Options;

namespace HalalChain.Storage.Evidence;

/// <summary>
/// Append-only newline-delimited JSON. Every read, ingest, and retention
/// decision lands here. For production, replace with a Postgres-backed
/// adapter; this exists so the audit trail is never silently absent.
/// </summary>
public sealed class NdjsonAccessLogger : IAccessLog
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public NdjsonAccessLogger(IOptions<NdjsonAccessLogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _path = Path.GetFullPath(options.Value.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public async Task RecordAsync(AccessRecord record, CancellationToken ct = default)
    {
        var line = JsonSerializer.Serialize(record);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class NdjsonAccessLogOptions
{
    public const string SectionName = "AccessLog:Ndjson";
    public string Path { get; set; } = "./.data/access-log.ndjson";
}
