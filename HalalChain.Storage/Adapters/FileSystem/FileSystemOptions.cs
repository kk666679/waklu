namespace HalalChain.Storage.Adapters.FileSystem;

public sealed class FileSystemOptions
{
    public const string SectionName = "Blob:FileSystem";

    public string Root { get; set; } = "./.data/blobs";

    /// <summary>
    /// If true, OpenReadAsync recomputes the hash and rejects mismatches.
    /// Costs a full read, so enable only where integrity is more important
    /// than throughput (e.g. certificate retrieval, audit replay).
    /// </summary>
    public bool VerifyOnRead { get; set; } = false;
}
