using System.Text.Json;
using HalalChain.Application.Storage;
using Microsoft.Extensions.Options;

namespace HalalChain.Storage.Evidence;

/// <summary>
/// JSON-file-backed metadata store for dev and contract tests. Production
/// uses the EF Core/Postgres adapter implemented in the API.
/// </summary>
public sealed class FileSystemEvidenceMetadataStore : IEvidenceMetadataStore
{
    private const string RecordExtension = ".json";
    private const string TombstoneExtension = ".tombstone";
    private const string LeaseExtension = ".lease";

    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileSystemEvidenceMetadataStore(IOptions<FileSystemMetadataOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _root = Path.GetFullPath(options.Value.Root);
        Directory.CreateDirectory(_root);
    }

    public async Task InsertAsync(EvidenceRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var path = PathFor(record.Id);
        var json = JsonSerializer.Serialize(record);
        await WriteAtomicAsync(path, json, ct).ConfigureAwait(false);
    }

    public async Task<EvidenceRecord?> GetAsync(EvidenceId id, CancellationToken ct = default)
    {
        var path = PathFor(id);
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<EvidenceRecord>(json);
    }

    public async Task<IReadOnlyList<EvidenceRecord>> QueryAsync(
        RetentionPolicy policy, int limit, CancellationToken ct = default)
    {
        // Scope filtering and the age decision belong to RetentionEvaluator, not
        // the persistence adapter. This returns every live candidate; the
        // evaluator is the single authority on Keep / Extend / Tombstone.
        var result = new List<EvidenceRecord>();

        foreach (var file in Directory.EnumerateFiles(_root, $"*{RecordExtension}"))
        {
            ct.ThrowIfCancellationRequested();
            if (result.Count >= limit) break;

            var id = EvidenceId.FromString(Path.GetFileNameWithoutExtension(file));
            if (File.Exists(PathFor(id, TombstoneExtension))) continue;

            var record = await GetAsync(id, ct).ConfigureAwait(false);
            if (record is not null) result.Add(record);
        }

        return result;
    }

    public async Task MarkTombstonedAsync(EvidenceId id, CancellationToken ct = default)
    {
        if (await GetAsync(id, ct).ConfigureAwait(false) is null) return;
        await File.WriteAllTextAsync(
            PathFor(id, TombstoneExtension), "tombstoned", ct).ConfigureAwait(false);
    }

    public async Task ExtendLeaseAsync(EvidenceId id, TimeSpan extension, CancellationToken ct = default)
    {
        if (await GetAsync(id, ct).ConfigureAwait(false) is null) return;
        await File.WriteAllTextAsync(
            PathFor(id, LeaseExtension), extension.ToString(), ct).ConfigureAwait(false);
    }

    private string PathFor(EvidenceId id, string extension = RecordExtension)
        => Path.Combine(_root, $"{id}{extension}");

    private async Task WriteAtomicAsync(string path, string contents, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var tmp = path + ".tmp";
            await File.WriteAllTextAsync(tmp, contents, ct).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class FileSystemMetadataOptions
{
    public const string SectionName = "Evidence:FileSystemMetadata";
    public string Root { get; set; } = "./.data/evidence-meta";
}
