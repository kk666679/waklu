using HalalChain.Application.Storage;
using HalalChain.Storage.Adapters.FileSystem;
using HalalChain.Storage.Evidence;
using HalalChain.Storage.Integrity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalalChain.Storage.DependencyInjection;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the filesystem adapter set. Call this for dev and tests.
    /// The API's composition root selects the adapter based on BLOB__PROVIDER.
    /// </summary>
    public static IServiceCollection AddFileSystemStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FileSystemOptions>(
            configuration.GetSection(FileSystemOptions.SectionName));
        services.Configure<FileSystemMetadataOptions>(
            configuration.GetSection(FileSystemMetadataOptions.SectionName));
        services.Configure<LocalProxyOptions>(
            configuration.GetSection(LocalProxyOptions.SectionName));

        services.AddSingleton<IContentHasher, Sha256ContentHasher>();
        services.AddSingleton<IBlobStore, FileSystemBlobStore>();
        services.AddSingleton<IEvidenceMetadataStore, FileSystemEvidenceMetadataStore>();
        services.AddSingleton<ISignedUrlIssuer, LocalProxySignedUrlIssuer>();
        services.AddSingleton<RetentionEvaluator>();
        services.AddSingleton<IEvidenceStore, EvidenceStore>();

        return services;
    }

    /// <summary>
    /// Replaces the access log with the NDJSON adapter. Production uses the
    /// Postgres adapter registered by the API module.
    /// </summary>
    public static IServiceCollection AddNdjsonAccessLog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<NdjsonAccessLogOptions>(
            configuration.GetSection(NdjsonAccessLogOptions.SectionName));
        services.AddSingleton<IAccessLog, NdjsonAccessLogger>();
        return services;
    }

    /// <summary>No-op access log for tests. Never use in production.</summary>
    public static IServiceCollection AddNullAccessLog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAccessLog, NullAccessLogger>();
        return services;
    }
}
