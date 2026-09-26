using System.Text.RegularExpressions;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp.Services;

internal sealed partial class PlatformDataService : IPlatformDataService
{
    private readonly string _solutionRoot;
    private readonly ILogger<PlatformDataService> _logger;

    public PlatformDataService(IOptions<HalalChainOptions> options, ILogger<PlatformDataService> logger)
    {
        _logger = logger;
        _solutionRoot = ResolveSolutionRoot(options.Value);
    }

    private static string ResolveSolutionRoot(HalalChainOptions options)
    {
        var envVar = HalalChainOptions.GetSolutionRootEnvVar();
        var fromEnv = Environment.GetEnvironmentVariable(envVar);

        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        if (!string.IsNullOrWhiteSpace(options.SolutionRoot))
        {
            return options.SolutionRoot;
        }

        return DiscoverSolutionRoot();
    }

    private static string DiscoverSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;

        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "HalalChain.Platform.sln")))
            {
                return dir;
            }

            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    public string GetSolutionRoot() => _solutionRoot;

    /// <summary>
    /// False when the configured root is not a directory. Every enumeration
    /// method degrades to an empty result rather than throwing, so a wrong
    /// <c>HALALCHAIN_SOLUTION_ROOT</c> produces a clear empty answer instead of
    /// a JSON-RPC internal error on every call.
    /// </summary>
    private bool RootExists
    {
        get
        {
            var exists = Directory.Exists(_solutionRoot);
            if (!exists)
            {
                _logger.LogWarning(
                    "Solution root does not exist: {Root}. Set {EnvVar} to the repository root.",
                    _solutionRoot,
                    HalalChainOptions.GetSolutionRootEnvVar());
            }

            return exists;
        }
    }

    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        var projects = new List<ProjectInfo>();

        if (!RootExists)
        {
            return projects;
        }

        foreach (var csproj in Directory.GetFiles(_solutionRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_solutionRoot, csproj);
            var name = Path.GetFileNameWithoutExtension(csproj);
            var targetFramework = ExtractTargetFramework(csproj);
            var packageRefs = ExtractPackageReferences(csproj);
            var projectRefs = ExtractProjectReferences(csproj, _solutionRoot);
            var projectKind = DetectProjectKind(csproj);

            projects.Add(new ProjectInfo
            {
                Name = name,
                RelativePath = relativePath,
                TargetFramework = targetFramework,
                PackageReferences = packageRefs,
                ProjectReferences = projectRefs,
                ProjectKind = projectKind,
            });
        }

        return projects.OrderBy(p => p.Name).ToList();
    }

    public IReadOnlyList<PageInfo> GetPages(string? projectName = null)
    {
        var pages = new List<PageInfo>();
        var projectDirs = GetProjectDirectories(projectName);

        foreach (var projectDir in projectDirs)
        {
            var pagesDir = Path.Combine(projectDir, "Pages");
            if (!Directory.Exists(pagesDir)) continue;

            foreach (var razor in Directory.GetFiles(pagesDir, "*.razor", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(razor);
                var routeMatch = RouteRegex().Match(content);
                if (!routeMatch.Success) continue;

                var route = routeMatch.Groups[1].Value;
                var relativePath = Path.GetRelativePath(_solutionRoot, razor);
                var directory = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";
                var layout = ExtractLayout(content);
                var project = Path.GetFileName(projectDir);

                pages.Add(new PageInfo
                {
                    Name = Path.GetFileNameWithoutExtension(razor),
                    Route = route,
                    RelativePath = relativePath,
                    Directory = directory,
                    Layout = layout,
                    ProjectName = project,
                });
            }
        }

        return pages.OrderBy(p => p.ProjectName).ThenBy(p => p.Route).ToList();
    }

    public IReadOnlyList<ComponentInfo> GetComponents(string? projectName = null)
    {
        var components = new List<ComponentInfo>();
        var projectDirs = GetProjectDirectories(projectName);

        foreach (var projectDir in projectDirs)
        {
            var componentsDir = Path.Combine(projectDir, "Components");
            if (!Directory.Exists(componentsDir)) continue;

            foreach (var razor in Directory.GetFiles(componentsDir, "*.razor", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(razor);
                var relativePath = Path.GetRelativePath(_solutionRoot, razor);
                var directory = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";
                var parameters = ExtractParameters(content);
                var project = Path.GetFileName(projectDir);

                components.Add(new ComponentInfo
                {
                    Name = Path.GetFileNameWithoutExtension(razor),
                    RelativePath = relativePath,
                    Directory = directory,
                    Parameters = parameters,
                    ProjectName = project,
                });
            }
        }

        return components.OrderBy(c => c.ProjectName).ThenBy(c => c.Directory).ThenBy(c => c.Name).ToList();
    }

    public IReadOnlyList<ServiceInfoMc> GetServices(string? projectName = null)
    {
        var services = new List<ServiceInfoMc>();
        var projectDirs = GetProjectDirectories(projectName);

        foreach (var projectDir in projectDirs)
        {
            var servicesDir = Path.Combine(projectDir, "Services");
            if (!Directory.Exists(servicesDir)) continue;

            foreach (var cs in Directory.GetFiles(servicesDir, "*.cs"))
            {
                var content = File.ReadAllText(cs);
                var relativePath = Path.GetRelativePath(_solutionRoot, cs);
                var interfaces = ExtractInterfaces(content);
                var methods = ExtractPublicMethods(content);
                var project = Path.GetFileName(projectDir);

                services.Add(new ServiceInfoMc
                {
                    Name = Path.GetFileNameWithoutExtension(cs),
                    RelativePath = relativePath,
                    Interfaces = interfaces,
                    Methods = methods,
                    ProjectName = project,
                });
            }
        }

        return services.OrderBy(s => s.ProjectName).ThenBy(s => s.Name).ToList();
    }

    public IReadOnlyList<ControllerInfo> GetControllers(string? projectName = null)
    {
        var controllers = new List<ControllerInfo>();
        var projectDirs = GetProjectDirectories(projectName);

        foreach (var projectDir in projectDirs)
        {
            var controllersDir = Path.Combine(projectDir, "Controllers");
            if (!Directory.Exists(controllersDir)) continue;

            foreach (var cs in Directory.GetFiles(controllersDir, "*.cs"))
            {
                var content = File.ReadAllText(cs);
                var relativePath = Path.GetRelativePath(_solutionRoot, cs);
                var routes = ExtractControllerRoutes(content);
                var project = Path.GetFileName(projectDir);

                controllers.Add(new ControllerInfo
                {
                    Name = Path.GetFileNameWithoutExtension(cs),
                    RelativePath = relativePath,
                    Routes = routes,
                    ProjectName = project,
                });
            }
        }

        return controllers.OrderBy(c => c.ProjectName).ThenBy(c => c.Name).ToList();
    }

    public IReadOnlyList<ViewInfo> GetViews(string? projectName = null)
    {
        var views = new List<ViewInfo>();
        var projectDirs = GetProjectDirectories(projectName);

        foreach (var projectDir in projectDirs)
        {
            var viewsDir = Path.Combine(projectDir, "Views");
            if (!Directory.Exists(viewsDir)) continue;

            foreach (var cshtml in Directory.GetFiles(viewsDir, "*.cshtml", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(cshtml);
                var relativePath = Path.GetRelativePath(_solutionRoot, cshtml);
                var directory = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";
                var layout = ExtractLayout(content);
                var project = Path.GetFileName(projectDir);

                views.Add(new ViewInfo
                {
                    Name = Path.GetFileNameWithoutExtension(cshtml),
                    RelativePath = relativePath,
                    Directory = directory,
                    Layout = layout,
                    ProjectName = project,
                });
            }
        }

        return views.OrderBy(v => v.ProjectName).ThenBy(v => v.Directory).ThenBy(v => v.Name).ToList();
    }

    public PlatformOverviewMc GetPlatformOverview()
    {
        var projects = GetProjects();
        var pages = GetPages();
        var components = GetComponents();
        var services = GetServices();
        var controllers = GetControllers();
        var views = GetViews();

        var solutionFile = RootExists
            ? Directory.GetFiles(_solutionRoot, "*.sln").FirstOrDefault()
            : null;

        var solutionName = Path.GetFileNameWithoutExtension(solutionFile ?? "HalalChain.Platform.sln");

        return new PlatformOverviewMc
        {
            SolutionName = solutionName,
            DotnetVersion = "10.0",
            TotalProjects = projects.Count,
            TotalPages = pages.Count,
            TotalComponents = components.Count,
            TotalServices = services.Count,
            TotalControllers = controllers.Count,
            TotalViews = views.Count,
            Projects = projects.ToList(),
            Pages = pages.ToList(),
            Components = components.ToList(),
            Services = services.ToList(),
            Controllers = controllers.ToList(),
            Views = views.ToList(),
        };
    }

    public string GetArchitecture()
    {
        if (!RootExists)
        {
            return $"""
                # Solution root unavailable

                `{_solutionRoot}` is not a directory, so nothing could be discovered.
                Set {HalalChainOptions.GetSolutionRootEnvVar()} to the repository root
                and restart the server.
                """;
        }

        var projects = GetProjects();
        var projectKinds = projects.GroupBy(p => p.ProjectKind)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Name))}")
            .ToList();

        var frontends = string.Join(" / ", projects.Where(p => p.ProjectKind is "BlazorWeb" or "MvcWeb").Select(p => p.Name));
        var kindSummary = string.Join("\n", projectKinds.Select(k => "- " + k));

        return $"""
            Browser
              ↓
            {frontends}  ← JWT auth
              ↓
            Platform API (.NET 10) — Modular Monolith
              ├── Modules/Identity, Catalog, Halal, Commerce, AI
              ├── PostgreSQL ← Catalog, Vendors, Orders, Halal records
              ├── Redis ← Cart session, cache
              ├── ai-inference ← Embeddings, classify, rerank
              └── tawheed ← Multi-agent evidence + Policy Engine

            Projects ({projects.Count})
            {kindSummary}
            """;
    }

    private List<string> GetProjectDirectories(string? projectName)
    {
        if (!RootExists)
        {
            return [];
        }

        if (string.IsNullOrEmpty(projectName))
        {
            return Directory.GetFiles(_solutionRoot, "*.csproj", SearchOption.AllDirectories)
                .Select(p => Path.GetDirectoryName(p)!)
                .ToList();
        }

        var csproj = Directory.GetFiles(_solutionRoot, "*.csproj", SearchOption.AllDirectories)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Equals(projectName, StringComparison.OrdinalIgnoreCase));

        if (csproj == null)
            throw new FileNotFoundException($"Project '{projectName}' not found in solution.");

        return [Path.GetDirectoryName(csproj)!];
    }

    private static string DetectProjectKind(string csprojPath)
    {
        var content = File.ReadAllText(csprojPath);
        var sdkMatch = SdkRegex().Match(content);
        var sdk = sdkMatch.Success ? sdkMatch.Groups[1].Value : "";
        var dir = Path.GetDirectoryName(csprojPath)!;
        var hasBlazor = Directory.Exists(Path.Combine(dir, "Components")) ||
                        Directory.Exists(Path.Combine(dir, "Pages")) ||
                        File.Exists(Path.Combine(dir, "_Imports.razor")) ||
                        File.Exists(Path.Combine(dir, "App.razor"));
        var hasViews = Directory.Exists(Path.Combine(dir, "Views"));
        var hasControllers = Directory.Exists(Path.Combine(dir, "Controllers"));
        var name = Path.GetFileNameWithoutExtension(csprojPath);

        if (name.Contains("Tests") || name.Contains("Test")) return "Test";
        if (sdk.Contains("BlazorWebAssembly")) return "BlazorWeb";
        if (hasBlazor) return "BlazorWeb";
        if (hasViews) return "MvcWeb";
        if (hasControllers) return "WebApi";
        if (sdk.Contains("Microsoft.NET.Sdk.Web")) return "Web";
        if (sdk.Contains("Microsoft.NET.Sdk")) return "Library";
        return "Unknown";
    }

    private static List<string> ExtractControllerRoutes(string csContent)
    {
        return RouteAttributeRegex().Matches(csContent)
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    private static string ExtractTargetFramework(string csprojPath)
    {
        var content = File.ReadAllText(csprojPath);
        var match = TargetFrameworkRegex().Match(content);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private static List<string> ExtractPackageReferences(string csprojPath)
    {
        var content = File.ReadAllText(csprojPath);
        return PackageRefRegex().Matches(content)
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    private static List<string> ExtractProjectReferences(string csprojPath, string solutionRoot)
    {
        var content = File.ReadAllText(csprojPath);
        var csprojDir = Path.GetDirectoryName(csprojPath) ?? "";
        return ProjectRefRegex().Matches(content)
            .Select(m =>
            {
                var relativeRef = m.Groups[1].Value;
                var fullPath = Path.GetFullPath(Path.Combine(csprojDir, relativeRef));
                return Path.GetFileNameWithoutExtension(fullPath);
            })
            .ToList();
    }

    private static string? ExtractLayout(string razorContent)
    {
        var match = LayoutRegex().Match(razorContent);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<string> ExtractParameters(string razorContent)
    {
        return ParamRegex().Matches(razorContent)
            .Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}")
            .ToList();
    }

    private static List<string> ExtractInterfaces(string csContent)
    {
        return InterfaceRegex().Matches(csContent)
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    private static List<string> ExtractPublicMethods(string csContent)
    {
        return MethodRegex().Matches(csContent)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(m => !m.StartsWith("//") && !m.StartsWith("class") && !m.StartsWith("record"))
            .ToList();
    }

    [GeneratedRegex(@"@page\s+""([^""]+)""")]
    private static partial Regex RouteRegex();

    [GeneratedRegex(@"TargetFramework>([^<]+)<")]
    private static partial Regex TargetFrameworkRegex();

    [GeneratedRegex(@"Include=""([^""]+)""")]
    private static partial Regex PackageRefRegex();

    [GeneratedRegex(@"Include=""(\.\.[^""]+)""")]
    private static partial Regex ProjectRefRegex();

    [GeneratedRegex(@"@layout\s+(\S+)")]
    private static partial Regex LayoutRegex();

    [GeneratedRegex(@"\[Parameter\]\s+public\s+(\S+)\s+(\S+)")]
    private static partial Regex ParamRegex();

    [GeneratedRegex(@"interface\s+(\w+)")]
    private static partial Regex InterfaceRegex();

    [GeneratedRegex(@"public\s+(?:async\s+)?(?:Task<?\w*>?|void|\w+)\s+(\w+)\s*\(")]
    private static partial Regex MethodRegex();

    [GeneratedRegex(@"<Project\s+Sdk=""([^""]+)""")]
    private static partial Regex SdkRegex();

    [GeneratedRegex(@"\[Route\s*\(\s*@?""([^""]+)""")]
    private static partial Regex RouteAttributeRegex();
}
