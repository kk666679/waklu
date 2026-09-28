using System.Globalization;
using System.Text;
using System.Text.Json;
using HalalChain.DataFlow.Models;
using Microsoft.Extensions.Logging;

namespace HalalChain.DataFlow.Validation;

/// <summary>
/// Records rows that failed validation or loading so they are inspectable
/// and replayable rather than lost. A dead-letter record must always carry
/// the original payload, otherwise the failed row is unactionable.
/// </summary>
public interface IDeadLetterSink
{
    Task WriteAsync(DataFlowErrorRecord error, CancellationToken cancellationToken = default);
    Task WriteBatchAsync(IReadOnlyCollection<DataFlowErrorRecord> errors, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataFlowErrorRecord>> ReadAsync(int maxRecords = 100, CancellationToken cancellationToken = default);
    Task<int> PurgeAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Appends dead-letter records to newline-delimited JSON under a configured
/// directory. One file per day keeps a runaway failure from producing a
/// single unparseably large file.
/// </summary>
public sealed class FileSystemDeadLetterSink : IDeadLetterSink
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private readonly string _directory;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly ILogger<FileSystemDeadLetterSink> _logger;

    public FileSystemDeadLetterSink(string directory, ILogger<FileSystemDeadLetterSink> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(logger);

        _directory = directory;
        _logger = logger;
        Directory.CreateDirectory(_directory);
    }

    public async Task WriteAsync(DataFlowErrorRecord error, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(error);

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(
                ResolvePath(error.OccurredAt),
                JsonSerializer.Serialize(error, Json) + Environment.NewLine,
                Encoding.UTF8,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A dead-letter write must never mask the original failure.
            _logger.LogError(ex, "Failed to append dead-letter record {ErrorId}", error.ErrorId);
            throw;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task WriteBatchAsync(
        IReadOnlyCollection<DataFlowErrorRecord> errors,
        CancellationToken cancellationToken = default)
    {
        if (errors.Count == 0) return;

        var sb = new StringBuilder();
        foreach (var error in errors)
        {
            sb.Append(JsonSerializer.Serialize(error, Json)).Append(Environment.NewLine);
        }

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(
                ResolvePath(DateTimeOffset.UtcNow), sb.ToString(), Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<IReadOnlyList<DataFlowErrorRecord>> ReadAsync(
        int maxRecords = 100,
        CancellationToken cancellationToken = default)
    {
        if (maxRecords <= 0) return Array.Empty<DataFlowErrorRecord>();

        var results = new List<DataFlowErrorRecord>();

        foreach (var file in EnumerateFilesNewestFirst())
        {
            var lines = await File.ReadAllLinesAsync(file, cancellationToken).ConfigureAwait(false);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var record = JsonSerializer.Deserialize<DataFlowErrorRecord>(line, Json);
                    if (record is not null) results.Add(record);
                }
                catch (JsonException)
                {
                    // A partially written final line is expected after a
                    // crash; skip rather than failing the whole read.
                }

                if (results.Count >= maxRecords) return results;
            }

            if (results.Count >= maxRecords) break;
        }

        return results;
    }

    public Task<int> PurgeAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = olderThan.UtcDateTime.Date;
        var removed = 0;

        foreach (var file in EnumerateFilesNewestFirst())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The filename encodes the write date, so the whole file goes.
            if (TryReadWriteDate(file, out var written) && written < cutoff)
            {
                File.Delete(file);
                removed++;
            }
        }

        _logger.LogInformation("Purged {Count} dead-letter files older than {Cutoff}", removed, cutoff);
        return Task.FromResult(removed);
    }

    /// <summary>
    /// Recovers the write date encoded in a dead-letter filename. The
    /// "deadletter-" prefix must be stripped before the date will parse,
    /// and an unparseable name is never treated as old enough to delete.
    /// </summary>
    private static bool TryReadWriteDate(string path, out DateTime written)
    {
        written = default;

        var name = Path.GetFileNameWithoutExtension(path);
        const string prefix = "deadletter-";

        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        return DateTime.TryParseExact(
            name[prefix.Length..], "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out written);
    }

    private IEnumerable<string> EnumerateFilesNewestFirst() =>
        Directory.EnumerateFiles(_directory, "deadletter-*.ndjson")
                  .OrderByDescending(f => f, StringComparer.Ordinal);

    private string ResolvePath(DateTimeOffset timestamp) =>
        Path.Combine(_directory, $"deadletter-{timestamp.UtcDateTime:yyyy-MM-dd}.ndjson");
}
