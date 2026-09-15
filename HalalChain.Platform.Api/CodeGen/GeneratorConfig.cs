namespace HalalChain.Platform.Api.CodeGen;

public sealed class GeneratorConfig
{
    public string Version { get; init; } = "1.0";
    public required string OpenApiPath { get; init; }
    public required string OutputRoot { get; init; }
    public required string Namespace { get; init; }
    public string RoutePrefix { get; init; } = "/admin/generated";
    public IReadOnlyList<string> StripRoutePrefixes { get; init; } = ["/api/v1/admin", "/api/v1"];
    public required TemplatePaths Templates { get; init; }
    public IReadOnlyList<string> GlobalUsing { get; init; } = [];
    public DiagnosticsOptions Diagnostics { get; init; } = new();
    public IReadOnlyDictionary<string, OverrideEntry> Overrides { get; init; }
        = new Dictionary<string, OverrideEntry>();
    public IReadOnlyList<SkipRule> Skip { get; init; } = [];
}

public sealed class TemplatePaths
{
    public required string List { get; init; }
    public required string Form { get; init; }
    public required string Detail { get; init; }
    public string? Nav { get; init; }
}

public sealed class DiagnosticsOptions
{
    public bool EmitSourceMap { get; init; }
    public bool FailOnWarning { get; init; }
}

public sealed class OverrideEntry
{
    public string? ListPage { get; init; }
    public string? FormPage { get; init; }
    public string? DetailPage { get; init; }
}

public sealed class SkipRule
{
    public string? Path { get; init; }
    public string? Controller { get; init; }
    public string? Reason { get; init; }
}