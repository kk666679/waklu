using System.Text;
using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Serves the solution's own documentation and source as MCP resources.
///
/// Two families are exposed:
///   * synthetic resources under <c>halalchain://</c> that are computed from
///     <see cref="IPlatformDataService"/> (architecture, project inventory);
///   * file-backed resources rooted at the solution directory.
///
/// File-backed reads are allowlisted by extension and directory and are
/// re-validated against the resolved root, so a crafted URI cannot walk out
/// of the repository.
/// </summary>
internal sealed class ResourceRegistry : IResourceRegistry
{
    private const string Scheme = "halalchain://";

    private static readonly string[] RootFiles =
    [
        "AGENTS.md",
        "Modelfile",
        "README.md",
        "service-manifest.yaml",
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".md", ".yaml", ".yml", ".json", ".txt", ".sln", ".csproj", ".props",
        ".cs", ".razor", ".py", ".sol", ".jsonc", ".http",
    ];

    private static readonly string[] AllowedDirectories =
    [
        "docs", ".halalchain", "infrastructure", "scripts",
    ];

    private readonly IPlatformDataService _platformData;
    private readonly McpOptions _options;
    private readonly ILogger<ResourceRegistry> _logger;
    private readonly string _root;

    public ResourceRegistry(
        IPlatformDataService platformData,
        IOptions<HalalChainOptions> options,
        ILogger<ResourceRegistry> logger)
    {
        _platformData = platformData;
        _options = options.Value.Mcp;
        _logger = logger;
        _root = Path.GetFullPath(platformData.GetSolutionRoot());
    }

    public IReadOnlyList<ResourceDefinition> All
    {
        get
        {
            if (!_options.EnableResources)
            {
                return [];
            }

            var definitions = new List<ResourceDefinition>
            {
                Synthetic("architecture", "HalalChain architecture",
                    "Rendered system diagram and service data flow.", "text/markdown"),
                Synthetic("solution/projects", "Project inventory",
                    "Every .csproj in the solution with framework, kind, and references.", "application/json"),
                Synthetic("solution/pages", "Razor page inventory",
                    "Every @page route discovered in the solution.", "application/json"),
                Synthetic("solution/services", "Service class inventory",
                    "Every class under a Services/ directory with its interfaces and methods.", "application/json"),
                Synthetic("docs/index", "Documentation index",
                    "Every markdown document under docs/ with its size.", "application/json"),
                Synthetic("runbooks/index", "Runbook index",
                    "Operational runbooks under docs/runbooks/.", "application/json"),
            };

            foreach (var file in RootFiles)
            {
                if (File.Exists(Path.Combine(_root, file)))
                {
                    definitions.Add(new ResourceDefinition
                    {
                        Uri = $"{Scheme}file/{file}",
                        Name = file,
                        Title = file,
                        Description = $"Repository file: {file}",
                        MimeType = MimeTypeFor(file),
                        Size = SafeLength(Path.Combine(_root, file)),
                    });
                }
            }

            return definitions;
        }
    }

    public bool Contains(string uri)
    {
        if (!_options.EnableResources || string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        return All.Any(r => string.Equals(r.Uri, uri, StringComparison.OrdinalIgnoreCase))
            || TryResolveFileUri(uri, out _, out _);
    }

    public bool TryRead(string uri, out ResourceContents? contents, out string? error)
    {
        contents = null;
        error = null;

        if (!_options.EnableResources)
        {
            error = "Resources are disabled on this server.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri))
        {
            error = "Missing resource uri.";
            return false;
        }

        var synthetic = uri.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)
            ? uri[Scheme.Length..]
            : null;

        if (synthetic is not null)
        {
            return TryReadSynthetic(synthetic, out contents, out error);
        }

        if (!TryResolveFileUri(uri, out var path, out error))
        {
            return false;
        }

        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                error = "Resource not found.";
                return false;
            }

            if (info.Length > _options.MaxResourceBytes)
            {
                error = $"Resource is {info.Length} bytes, above the {_options.MaxResourceBytes} byte limit.";
                return false;
            }

            contents = new ResourceContents
            {
                Uri = uri,
                MimeType = MimeTypeFor(path),
                Text = File.ReadAllText(path, Encoding.UTF8),
            };
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed reading resource {Uri}", uri);
            error = "Resource could not be read.";
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied reading resource {Uri}", uri);
            error = "Resource access denied.";
            return false;
        }
    }

    private bool TryReadSynthetic(string name, out ResourceContents? contents, out string? error)
    {
        contents = null;
        error = null;

        var (mime, payload) = name switch
        {
            "architecture" => ("text/markdown", _platformData.GetArchitecture()),
            "solution/projects" => ("application/json",
                JsonSerializer.Serialize(_platformData.GetProjects(), McpJson.Options)),
            "solution/pages" => ("application/json",
                JsonSerializer.Serialize(_platformData.GetPages(), McpJson.Options)),
            "solution/services" => ("application/json",
                JsonSerializer.Serialize(_platformData.GetServices(), McpJson.Options)),
            "docs/index" => ("application/json", JsonSerializer.Serialize(EnumerateDocs(), McpJson.Options)),
            "runbooks/index" => ("application/json",
                JsonSerializer.Serialize(EnumerateRunbooks(), McpJson.Options)),
            _ => (string.Empty, string.Empty),
        };

        if (mime.Length == 0)
        {
            error = $"Unknown resource '{name}'.";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(payload) > _options.MaxResourceBytes)
        {
            error = $"Resource exceeds the {_options.MaxResourceBytes} byte limit.";
            return false;
        }

        contents = new ResourceContents
        {
            Uri = $"{Scheme}{name}",
            MimeType = mime,
            Text = payload,
        };
        return true;
    }

    private bool TryResolveFileUri(string uri, out string path, out string? error)
    {
        path = string.Empty;
        error = null;

        const string filePrefix = "file://";
        if (!uri.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
        {
            error = "Resource uri must start with 'halalchain://' or 'file://'.";
            return false;
        }

        var relative = uri[filePrefix.Length..].Replace('/', Path.DirectorySeparatorChar);

        if (relative.Length == 0)
        {
            error = "Empty resource path.";
            return false;
        }

        // Reject traversal before touching the filesystem, then re-check the
        // fully resolved path against the root. Either check alone is
        // insufficient: the first misses symlink-style tricks, the second
        // misses percent- or dot-segment normalisation.
        if (relative.Contains("..", StringComparison.Ordinal))
        {
            error = "Resource path may not contain '..'.";
            return false;
        }

        var extension = Path.GetExtension(relative);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            error = $"Resource extension '{extension}' is not served.";
            return false;
        }

        var topLevel = relative.Split(Path.DirectorySeparatorChar)[0];
        if (!RootFiles.Contains(relative, StringComparer.OrdinalIgnoreCase) &&
            !AllowedDirectories.Contains(topLevel, StringComparer.OrdinalIgnoreCase))
        {
            error = $"Resource directory '{topLevel}' is not served.";
            return false;
        }

        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(_root, relative));
        }
        catch (ArgumentException)
        {
            error = "Resource path is not valid.";
            return false;
        }

        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            error = "Resource path escapes the solution root.";
            return false;
        }

        path = candidate;
        return true;
    }

    /// <summary>Docs index. Runbooks are excluded; they have their own index.</summary>
    private IReadOnlyList<DocEntry> EnumerateDocs() => Enumerate("docs", excludeRunbooks: true);

    /// <summary>Runbook index. Nothing is excluded — this is the leaf directory.</summary>
    private IReadOnlyList<DocEntry> EnumerateRunbooks() => Enumerate("docs/runbooks", excludeRunbooks: false);

    private IReadOnlyList<DocEntry> Enumerate(string relativeDirectory, bool excludeRunbooks)
    {
        var entries = new List<DocEntry>();
        var directory = Path.GetFullPath(Path.Combine(_root, relativeDirectory));

        if (!Directory.Exists(directory))
        {
            return entries;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(_root, file).Replace('\\', '/');
                if (excludeRunbooks && relative.StartsWith("docs/runbooks/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entries.Add(new DocEntry
                {
                    Uri = $"{Scheme}file/{relative}",
                    Path = relative,
                    Bytes = SafeLength(file) ?? 0,
                });
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed enumerating documents under {Directory}", relativeDirectory);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied enumerating {Directory}", relativeDirectory);
        }

        return entries.OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static ResourceDefinition Synthetic(string name, string title, string description, string mimeType) =>
        new()
        {
            Uri = $"{Scheme}{name}",
            Name = name,
            Title = title,
            Description = description,
            MimeType = mimeType,
        };

    private static long? SafeLength(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? info.Length : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string MimeTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".md" => "text/markdown",
        ".json" => "application/json",
        ".yaml" or ".yml" => "application/yaml",
        ".cs" => "text/x-csharp",
        ".razor" => "text/x-razor",
        ".py" => "text/x-python",
        ".sol" => "text/x-solidity",
        ".sln" => "text/plain",
        ".csproj" or ".props" => "application/xml",
        ".http" => "message/http",
        _ => "text/plain",
    };

    private sealed class DocEntry
    {
        public string Uri { get; set; } = "";
        public string Path { get; set; } = "";
        public long Bytes { get; set; }
    }
}
