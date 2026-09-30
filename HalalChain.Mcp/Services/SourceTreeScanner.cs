namespace HalalChain.Mcp.Services;

/// <summary>
/// Pruned, best-effort walk of the solution tree, shared by the source and
/// governance scanners.
///
/// Two things make this more than a <c>Directory.GetFiles</c> call:
///
///   * <b>Pruning.</b> <c>bin</c>, <c>obj</c>, <c>node_modules</c> and any
///     dot-directory are skipped, so a scan never counts build output — and
///     never reads <c>.kilo/worktrees/</c>, which holds a second copy of the
///     whole solution and would double every count.
///   * <b>Degrading.</b> A missing root yields nothing, and an unreadable file
///     is skipped rather than thrown. A wrong <c>HALALCHAIN_SOLUTION_ROOT</c>
///     must produce an empty answer, not a JSON-RPC internal error.
/// </summary>
internal sealed class SourceTreeScanner
{
    private static readonly string[] PrunedDirectories =
    [
        "bin",
        "obj",
        "node_modules",
        "worktrees",
        "dist",
        "TestResults",
        "packages",
        ".git",
        ".vs",
        ".venv",
        "venv",
        "__pycache__",
    ];

    private readonly string _root;
    private readonly Dictionary<string, string> _projectNameCache = new(StringComparer.OrdinalIgnoreCase);

    public SourceTreeScanner(string root)
    {
        _root = root;
    }

    /// <summary>Absolute solution root this scanner walks.</summary>
    public string Root => _root;

    public bool RootExists => Directory.Exists(_root);

    /// <summary>
    /// Every file with <paramref name="extension"/> under the root, with build
    /// output, dependency caches, dot-directories and worktree copies pruned.
    /// </summary>
    public IEnumerable<string> Enumerate(string extension, string? projectName = null)
    {
        if (!RootExists)
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(_root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();

            foreach (var file in SafeFiles(directory, extension))
            {
                if (projectName is null || BelongsToProject(file, projectName))
                {
                    yield return file;
                }
            }

            foreach (var child in SafeDirectories(directory))
            {
                var name = Path.GetFileName(child);

                // A leading dot marks tooling state (.git, .kilo, .kiro,
                // .autoclaw, .clinerules) — never solution source.
                if (name.StartsWith('.') || IsPruned(name))
                {
                    continue;
                }

                pending.Push(child);
            }
        }
    }

    private static string[] SafeFiles(string directory, string extension)
    {
        try
        {
            return Directory.GetFiles(directory, "*" + extension);
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static string[] SafeDirectories(string directory)
    {
        try
        {
            return Directory.GetDirectories(directory);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    /// <summary>
    /// Restricts a scan to one project. Matching is on the directory segment
    /// name, not a substring of the whole path: a filter of <c>HalalChain.Web</c>
    /// must not also match a file inside <c>HalalChain.Web.Tests</c>.
    /// </summary>
    private bool BelongsToProject(string file, string projectName)
    {
        var segments = RelativePath(file).Split('/');

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals(projectName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Root-relative path with forward slashes, for stable output on every host.</summary>
    public string RelativePath(string fullPath) =>
        Path.GetRelativePath(_root, fullPath).Replace('\\', '/');

    /// <summary>
    /// Forward-slash form of an already-relative path, for callers that built a
    /// path out of literals (a candidate document location, a joined file name)
    /// rather than walking the tree. Output must not differ between Windows and
    /// Linux, so backslashes are normalised away.
    /// </summary>
    public static string Normalise(string relativePath) =>
        relativePath.Replace('\\', '/');

    /// <summary>
    /// The owning project of a file, resolved by walking up to the nearest
    /// directory that holds a <c>.csproj</c>. Cached because a solution-wide
    /// scan asks this question once per file.
    /// </summary>
    public string ProjectNameOf(string fullPath)
    {
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is null)
        {
            return "unknown";
        }

        if (_projectNameCache.TryGetValue(directory, out var cached))
        {
            return cached;
        }

        var name = ResolveProjectName(directory);
        _projectNameCache[directory] = name;
        return name;
    }

    private string ResolveProjectName(string startDirectory)
    {
        var directory = startDirectory;

        while (directory is not null)
        {
            try
            {
                var csproj = Directory.GetFiles(directory, "*.csproj");
                if (csproj.Length > 0)
                {
                    return Path.GetFileNameWithoutExtension(csproj[0]);
                }
            }
            catch (DirectoryNotFoundException)
            {
                break;
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }

            if (directory.Equals(_root, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return Path.GetFileName(startDirectory);
    }

    /// <summary>
    /// Reads a file, returning null when it cannot be read. Callers treat null
    /// as "nothing to report" rather than failing a whole scan over one locked
    /// or deleted file.
    /// </summary>
    public static string? TryRead(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>One-based line number for a character offset, for output that can be navigated.</summary>
    public static int LineNumber(string text, int index)
    {
        var limit = Math.Clamp(index, 0, text.Length);
        var line = 1;

        for (var i = 0; i < limit; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static bool IsPruned(string directoryName)
    {
        foreach (var pruned in PrunedDirectories)
        {
            if (directoryName.Equals(pruned, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
