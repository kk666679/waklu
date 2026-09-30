using System.Text.RegularExpressions;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Reads structure out of the C# sources under the solution root.
///
/// This is the layer behind the Wave-1 tools. It parses text — it does not
/// compile, execute, or evaluate anything, so it cannot produce or influence a
/// compliance verdict. Its only product is a list of things that exist and
/// where they live.
///
/// Detection patterns are deliberately stated in the tool output, because a
/// scanner that hides its heuristic invites a reader to treat "not found" as
/// "does not exist".
/// </summary>
internal sealed partial class CodeIntrospectionService : ICodeIntrospectionService
{
    /// <summary>A type declaration head, with primary constructors and base lists.</summary>
    private const string TypeKind = @"(?:class|record(?:\s+(?:class|struct))?)";

    /// <summary>
    /// Every kind the type finder understands. One pass over a file answers a
    /// "where is this declared?" question for all declaration keywords.
    /// </summary>
    private const string AllDeclarationKinds =
        @"(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)";

    private readonly SourceTreeScanner _tree;
    private readonly ILogger<CodeIntrospectionService> _logger;

    public CodeIntrospectionService(
        IPlatformDataService platformData,
        ILogger<CodeIntrospectionService> logger)
    {
        _logger = logger;

        // The root is taken from the platform data service rather than from
        // AppContext.BaseDirectory: that service already resolves
        // HALALCHAIN_SOLUTION_ROOT, then configuration, then walks up for the
        // .sln. Two root-resolution rules would drift.
        _tree = new SourceTreeScanner(Path.GetFullPath(platformData.GetSolutionRoot()));

        // A missing root is reported once, at construction, and then every scan
        // degrades to an empty result. The alternative — throwing from each
        // method — turns a misconfiguration into a JSON-RPC internal error on
        // every call, which is harder to diagnose than a clear empty answer.
        if (!_tree.RootExists)
        {
            _logger.LogWarning(
                "Solution root does not exist: {Root}. Set {EnvVar} to the repository root; introspection will return empty results.",
                _tree.Root,
                HalalChainOptions.GetSolutionRootEnvVar());
        }
    }

    public IReadOnlyList<EndpointInfo> GetEndpoints(string? projectName = null)
    {
        var endpoints = new List<EndpointInfo>();

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null || !text.Contains("[ApiController]", StringComparison.Ordinal))
            {
                continue;
            }

            var relative = _tree.RelativePath(file);
            var project = _tree.ProjectNameOf(file);
            var className = Path.GetFileNameWithoutExtension(file);
            var classRoute = ResolveClassRoute(text, className);

            foreach (Match match in HttpVerbRegex().Matches(text))
            {
                var verb = match.Groups[1].Value.ToUpperInvariant();
                var template = match.Groups[2].Success ? match.Groups[2].Value : "";
                var method = NextActionName(text, match.Index + match.Length);

                endpoints.Add(new EndpointInfo
                {
                    Verb = verb,
                    Path = CombineRoutes(classRoute, template),
                    Handler = method,
                    Controller = className,
                    RelativePath = relative,
                    ProjectName = project,
                    Line = SourceTreeScanner.LineNumber(text, match.Index),
                });
            }
        }

        return endpoints
            .OrderBy(e => e.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Path, StringComparer.Ordinal)
            .ThenBy(e => e.Verb, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<HandlerInfo> GetHandlers(string? projectName = null)
    {
        var handlers = new List<HandlerInfo>();

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null ||
                (!text.Contains("Handler", StringComparison.Ordinal) &&
                 !text.Contains("IRequestHandler<", StringComparison.Ordinal)))
            {
                continue;
            }

            var relative = _tree.RelativePath(file);
            var project = _tree.ProjectNameOf(file);

            foreach (var declaration in Declarations(text, TypeKind))
            {
                if (declaration.DerivesFrom("IRequestHandler<"))
                {
                    var arguments = declaration.TypeArgumentsOf("IRequestHandler");
                    handlers.Add(new HandlerInfo
                    {
                        Kind = "request",
                        Handler = declaration.Name,
                        Request = arguments.Count > 0 ? arguments[0] : "",
                        Response = arguments.Count > 1 ? arguments[1] : "",
                        RelativePath = relative,
                        ProjectName = project,
                        Line = SourceTreeScanner.LineNumber(text, declaration.Index),
                    });
                }
                else if (declaration.DerivesFrom("INotificationHandler<"))
                {
                    var arguments = declaration.TypeArgumentsOf("INotificationHandler");
                    handlers.Add(new HandlerInfo
                    {
                        Kind = "notification",
                        Handler = declaration.Name,
                        Request = arguments.Count > 0 ? arguments[0] : "",
                        RelativePath = relative,
                        ProjectName = project,
                        Line = SourceTreeScanner.LineNumber(text, declaration.Index),
                    });
                }
            }
        }

        return handlers
            .OrderBy(h => h.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Request, StringComparer.Ordinal)
            .ThenBy(h => h.Handler, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<DomainEventInfo> GetDomainEvents(string? projectName = null)
    {
        var events = new List<DomainEventInfo>();

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null ||
                (!text.Contains("Event", StringComparison.Ordinal) &&
                 !text.Contains("INotification", StringComparison.Ordinal)))
            {
                continue;
            }

            var relative = _tree.RelativePath(file);
            var project = _tree.ProjectNameOf(file);

            foreach (var declaration in Declarations(text, TypeKind))
            {
                // Three shapes are in play in this solution: MediatR
                // notifications, an IDomainEvent marker if one is introduced,
                // and plain *Event payloads handed to IEventBus.PublishAsync.
                var contract = declaration.DerivesFrom("IDomainEvent") ? "IDomainEvent"
                    : declaration.DerivesFrom("INotification") ? "INotification"
                    : declaration.Name.EndsWith("Event", StringComparison.Ordinal) ? "naming-convention (*Event)"
                    : null;

                if (contract is null)
                {
                    continue;
                }

                events.Add(new DomainEventInfo
                {
                    Name = declaration.Name,
                    Contract = contract,
                    RelativePath = relative,
                    ProjectName = project,
                    Line = SourceTreeScanner.LineNumber(text, declaration.Index),
                });
            }
        }

        return events
            .OrderBy(e => e.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<TypeLocationInfo> GetAggregateRoots(string? projectName = null)
    {
        var roots = new List<TypeLocationInfo>();

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null ||
                (!text.Contains("IAggregateRoot", StringComparison.Ordinal) &&
                 !text.Contains("AggregateRoot", StringComparison.Ordinal) &&
                 !text.Contains(": Entity", StringComparison.Ordinal)))
            {
                continue;
            }

            var relative = _tree.RelativePath(file);
            var project = _tree.ProjectNameOf(file);
            var isInterfaceFile = file.EndsWith("IAggregateRoot.cs", StringComparison.OrdinalIgnoreCase);

            foreach (var declaration in Declarations(text, TypeKind))
            {
                if (isInterfaceFile)
                {
                    continue;
                }

                // The base list is checked, not the whole file, so a file that
                // merely mentions AggregateRoot in a comment contributes nothing.
                var kind = declaration.DerivesFrom("IAggregateRoot") ? "aggregate-root (IAggregateRoot)"
                    : declaration.DerivesFrom("AggregateRoot") ? "aggregate-root (base class)"
                    : declaration.DerivesFrom("IEntity") || declaration.DerivesFrom(": Entity") ? "entity"
                    : null;

                if (kind is null)
                {
                    continue;
                }

                roots.Add(new TypeLocationInfo
                {
                    Name = declaration.Name,
                    Kind = kind,
                    RelativePath = relative,
                    ProjectName = project,
                    Line = SourceTreeScanner.LineNumber(text, declaration.Index),
                });
            }
        }

        return roots
            .OrderBy(r => r.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<TypeLocationInfo> GetValueObjects(string? projectName = null)
    {
        var valueObjects = new List<TypeLocationInfo>();

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null || !text.Contains("record", StringComparison.Ordinal))
            {
                continue;
            }

            // A value object is either declared in a ValueObjects unit (a file
            // named ValueObjects.cs, or a folder segment named ValueObjects), or
            // it opts in by implementing IValueObject. Scanning only a
            // `ValueObjects/` folder would miss Domain/Halal/ValueObjects.cs,
            // which is where this solution keeps several of them.
            var relative = _tree.RelativePath(file);
            var inValueObjectsUnit =
                Path.GetFileName(file).Equals("ValueObjects.cs", StringComparison.OrdinalIgnoreCase) ||
                relative.Contains("/ValueObjects/", StringComparison.OrdinalIgnoreCase);

            var project = _tree.ProjectNameOf(file);

            foreach (var declaration in Declarations(text, @"record(?:\s+(?:class|struct))?"))
            {
                var kind = declaration.DerivesFrom("IValueObject") ? "value-object (IValueObject)"
                    : inValueObjectsUnit ? "value-object (ValueObjects unit)"
                    : null;

                if (kind is null)
                {
                    continue;
                }

                valueObjects.Add(new TypeLocationInfo
                {
                    Name = declaration.Name,
                    Kind = kind,
                    RelativePath = relative,
                    ProjectName = project,
                    Line = SourceTreeScanner.LineNumber(text, declaration.Index),
                });
            }
        }

        return valueObjects
            .OrderBy(v => v.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<MigrationInfo> GetMigrations(string? projectName = null)
    {
        var migrations = new List<MigrationInfo>();

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var relative = _tree.RelativePath(file);
            if (!relative.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = MigrationFileRegex().Match(Path.GetFileNameWithoutExtension(file));
            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups["name"].Value;

            // The .Designer.cs companion is the same migration's generated
            // model, so listing both would double every count.
            if (name.EndsWith(".Designer", StringComparison.Ordinal))
            {
                continue;
            }

            migrations.Add(new MigrationInfo
            {
                Timestamp = match.Groups["timestamp"].Value,
                Name = name,
                RelativePath = relative,
                ProjectName = _tree.ProjectNameOf(file),
            });
        }

        // EF orders migrations by the timestamp baked into the file name; the
        // dotted suffix keeps snapshots from interleaving arbitrarily.
        return migrations
            .OrderBy(m => m.Timestamp, StringComparer.Ordinal)
            .ThenBy(m => m.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<TestProjectInfo> GetTestInventory(string? projectName = null)
    {
        var byProject = new Dictionary<string, TestProjectInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var project = _tree.ProjectNameOf(file);

            // A test project is identified by its own name, and the counts come
            // from that project's files. Matching on a "/Tests/" path segment
            // instead would miss files that sit beside it (fixtures, builders).
            if (!project.Contains("Test", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = SourceTreeScanner.TryRead(file);
            if (text is null)
            {
                continue;
            }

            var facts = FactRegex().Matches(text).Count;
            var theories = TheoryRegex().Matches(text).Count;
            var cases = InlineDataRegex().Matches(text).Count;

            if (facts + theories == 0)
            {
                continue;
            }

            if (!byProject.TryGetValue(project, out var info))
            {
                info = new TestProjectInfo { ProjectName = project };
                byProject[project] = info;
            }

            info.Facts += facts;
            info.Theories += theories;
            info.InlineDataCases += cases;
            info.Files++;
        }

        return byProject.Values
            .OrderBy(p => p.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<TypeLocationInfo> FindType(string typeName, string? projectName = null)
    {
        var matches = new List<TypeLocationInfo>();

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return matches;
        }

        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null)
            {
                continue;
            }

            var relative = _tree.RelativePath(file);
            var project = _tree.ProjectNameOf(file);

            foreach (var declaration in Declarations(text, AllDeclarationKinds))
            {
                // Exact matches carry the answer; substring matches are the
                // "roughly this name" fallback an agent expects from a find.
                var exact = declaration.Name.Equals(typeName, StringComparison.Ordinal);
                if (!exact && !declaration.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(new TypeLocationInfo
                {
                    Name = declaration.Name,
                    Kind = declaration.Kind,
                    RelativePath = relative,
                    ProjectName = project,
                    Line = SourceTreeScanner.LineNumber(text, declaration.Index),
                });
            }
        }

        return matches
            .OrderByDescending(m => m.Name.Equals(typeName, StringComparison.Ordinal))
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.RelativePath, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Type declarations in a file as (name, header, offset).
    ///
    /// A single do-everything regex cannot see this solution's shapes. MediatR
    /// handlers are declared with a *primary constructor* and their interface on
    /// the following line:
    ///
    /// <code>
    /// internal sealed class ListVendorsHandler(IVendorRepository vendorRepository)
    ///     : IRequestHandler&lt;ListVendorsQuery, VendorDto[]&gt;
    /// </code>
    ///
    /// A <c>class X : IBoundary</c> pattern silently skips every one of those,
    /// which then reads as "there are no handlers". So the declaration head —
    /// modifiers, primary constructor, base list — is captured verbatim up to
    /// the opening brace, and the base list is inspected as text.
    /// </summary>
    private static IEnumerable<TypeDeclaration> Declarations(string text, string kindPattern)
    {
        Regex regex;

        lock (DeclarationPatterns)
        {
            if (!DeclarationPatterns.TryGetValue(kindPattern, out var cached))
            {
                cached = new Regex(
                    @"^[ \t]*(?:(?:public|internal|protected|private)[ \t]+)?" +
                    @"(?:(?:sealed|abstract|static|partial|readonly)[ \t]+)*" +
                    @"(?<kind>" + kindPattern + @")[ \t]+(?<name>\w+)(?<head>[^{;=]*)",
                    RegexOptions.Multiline);
                DeclarationPatterns[kindPattern] = cached;
            }

            regex = cached;
        }

        foreach (Match match in regex.Matches(text))
        {
            yield return new TypeDeclaration(
                match.Groups["kind"].Value,
                match.Groups["name"].Value,
                match.Groups["head"].Value,
                match.Index);
        }
    }

    private static readonly Dictionary<string, Regex> DeclarationPatterns = new(StringComparer.Ordinal);

    /// <summary>
    /// The action method a <c>[HttpVerb]</c> attribute decorates: the first
    /// method signature after the attribute. Constructors do not match, because
    /// the pattern requires a return type separated from the name by whitespace.
    /// </summary>
    private static string NextActionName(string text, int startIndex)
    {
        if (startIndex >= text.Length)
        {
            return "(unknown)";
        }

        var window = text.Substring(startIndex, Math.Min(600, text.Length - startIndex));
        var match = ActionMethodRegex().Match(window);
        return match.Success ? match.Groups[1].Value : "(unknown)";
    }

    /// <summary>
    /// The controller-level route, taken from the nearest <c>[Route("...")]</c>
    /// above the file's first class declaration. The <c>[controller]</c> token
    /// is expanded, since <c>api/v1/[controller]</c> is the common convention.
    /// </summary>
    private static string ResolveClassRoute(string text, string className)
    {
        var declarations = Declarations(text, "class").ToList();
        var declarationIndex = declarations.Count > 0 ? declarations[0].Index : 0;
        var windowStart = Math.Max(0, declarationIndex - 600);
        var window = text[windowStart..declarationIndex];
        var matches = ClassRouteRegex().Matches(window);

        if (matches.Count == 0)
        {
            return "";
        }

        // Nearest attribute above the class wins: a file can mention [Route]
        // for a second type further down.
        var route = matches[^1].Groups[1].Value;
        var controllerName = className.EndsWith("Controller", StringComparison.Ordinal)
            ? className[..^"Controller".Length]
            : className;

        return route.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Joins a class route and an action template into one path.</summary>
    private static string CombineRoutes(string classRoute, string template)
    {
        var segments = new List<string>();

        foreach (var part in new[] { classRoute, template })
        {
            var trimmed = part.Trim('/');
            if (trimmed.Length > 0)
            {
                segments.Add(trimmed);
            }
        }

        return segments.Count == 0 ? "/" : "/" + string.Join('/', segments);
    }

    /// <summary>
    /// A type declaration head: the kind keyword as written (<c>record struct</c>
    /// stays one kind), the name, the text between it and the opening brace
    /// (primary constructor plus base list), and the offset in the file.
    /// </summary>
    private readonly record struct TypeDeclaration(string Kind, string Name, string Header, int Index)
    {
        /// <summary>True when the header names <paramref name="candidate"/> as a base type or interface.</summary>
        public bool DerivesFrom(string candidate) =>
            Header.Contains(candidate, StringComparison.Ordinal);

        /// <summary>
        /// The type arguments of <paramref name="generic"/> as written in the
        /// header, outermost only: <c>IRequestHandler&lt;Q, R[]&gt;</c> yields
        /// <c>["Q", "R[]"]</c>.
        /// </summary>
        public IReadOnlyList<string> TypeArgumentsOf(string generic)
        {
            var at = Header.IndexOf(generic + "<", StringComparison.Ordinal);
            if (at < 0)
            {
                return [];
            }

            var open = Header.IndexOf('<', at);
            var depth = 0;

            for (var i = open; i < Header.Length; i++)
            {
                if (Header[i] == '<')
                {
                    depth++;
                }
                else if (Header[i] == '>')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return SplitTypeArguments(Header[(open + 1)..i]);
                    }
                }
            }

            return [];
        }

        /// <summary>Splits a generic argument list, respecting nested generics.</summary>
        private static IReadOnlyList<string> SplitTypeArguments(string inner)
        {
            var parts = new List<string>();
            var depth = 0;
            var start = 0;

            for (var i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '<')
                {
                    depth++;
                }
                else if (inner[i] == '>')
                {
                    depth--;
                }
                else if (inner[i] == ',' && depth == 0)
                {
                    parts.Add(inner[start..i].Trim());
                    start = i + 1;
                }
            }

            var last = inner[start..].Trim();
            if (last.Length > 0)
            {
                parts.Add(last);
            }

            return parts;
        }
    }

    [GeneratedRegex(@"\[Http(Get|Post|Put|Patch|Delete|Head|Options)(?:\(\s*""([^""]*)""\s*\))?\]")]
    private static partial Regex HttpVerbRegex();

    [GeneratedRegex(@"\[Route\s*\(\s*@?""([^""]+)""\s*\)\]")]
    private static partial Regex ClassRouteRegex();

    [GeneratedRegex(@"public\s+(?:override\s+|virtual\s+|async\s+)*[\w<>\[\],\.\?]+\s+(\w+)\s*\(")]
    private static partial Regex ActionMethodRegex();

    [GeneratedRegex(@"^(?<timestamp>\d{14})_(?<name>.+)$")]
    private static partial Regex MigrationFileRegex();

    [GeneratedRegex(@"\[Fact\b")]
    private static partial Regex FactRegex();

    [GeneratedRegex(@"\[Theory\b")]
    private static partial Regex TheoryRegex();

    [GeneratedRegex(@"\[InlineData\b")]
    private static partial Regex InlineDataRegex();
}
