using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using RazorLight;
using HalalChain.Platform.Api.CodeGen.Models;
using HalalChain.Platform.Api.CodeGen.OpenApi;

namespace HalalChain.Platform.Api.CodeGen;

public sealed class Generator(GeneratorConfig cfg, string projectDir, Action<string> log)
{
    public GenerationResult Run()
    {
        var openApiPath = Path.GetFullPath(Path.Combine(projectDir, cfg.OpenApiPath));
        if (!File.Exists(openApiPath))
            throw new FileNotFoundException($"OpenAPI not found: {openApiPath}");

        var doc = new OpenApiStringReader().Read(File.ReadAllText(openApiPath), out var diag);
        if (diag.Errors.Count > 0)
            throw new InvalidOperationException(
                "OpenAPI parse errors:\n" + string.Join("\n", diag.Errors.Select(e => e.Message)));

        var extractor = new AdminUiExtractor();
        var resources = extractor.Extract(doc, cfg);
        log($"Extracted {resources.Count} resources eligible for generation.");

        var outputRoot = Path.GetFullPath(Path.Combine(projectDir, cfg.OutputRoot));
        Directory.CreateDirectory(outputRoot);

        // Wipe generated files (not the root)
        foreach (var f in Directory.EnumerateFiles(outputRoot, "*.razor", SearchOption.AllDirectories))
            File.Delete(f);
        var navFile = Path.Combine(outputRoot, "GeneratedAdminNav.cs");
        if (File.Exists(navFile)) File.Delete(navFile);

        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(projectDir, "CodeGen"))
            .UseMemoryCachingProvider()
            .Build();

        var pageCount = 0;
        var warnings = new List<string>();

        foreach (var resource in resources)
        {
            if (cfg.Overrides.TryGetValue(resource.Name, out var ov))
            {
                log($"Override: skipping generated pages for '{resource.Name}'");
                continue;
            }

            var ctx = BuildTemplateContext(resource, cfg);

            var listTpl = cfg.Templates.List;
            var listRazor = engine.CompileRenderAsync(listTpl, ctx).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(outputRoot, $"{resource.Name}List.razor"), listRazor);
            pageCount++;

            if (resource.HasCreate || resource.HasUpdate)
            {
                var formRazor = engine.CompileRenderAsync(cfg.Templates.Form, ctx).GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(outputRoot, $"{resource.Name}Form.razor"), formRazor);
                pageCount++;
            }

            if (resource.HasDetail)
            {
                var detailRazor = engine.CompileRenderAsync(cfg.Templates.Detail, ctx).GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(outputRoot, $"{resource.Name}Detail.razor"), detailRazor);
                pageCount++;
            }
        }

        if (cfg.Templates.Nav is { } navTpl)
        {
            var navCtx = new { Namespace = cfg.Namespace, Resources = resources, cfg.RoutePrefix };
            var navRazor = engine.CompileRenderAsync(navTpl, navCtx).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(outputRoot, "GeneratedAdminNav.cs"), navRazor);
        }

        return new GenerationResult(pageCount, 1, warnings);
    }

    private static object BuildTemplateContext(GeneratedResource r, GeneratorConfig cfg) => new
    {
        Name = r.Name,
        DisplayName = r.Name,
        Segment = r.Segment,
        RouteSegment = r.Segment,
        RoutePrefix = cfg.RoutePrefix,
        ApiPath = r.ApiPath,
        DtoType = r.DtoType,
        CreateRequestType = r.CreateRequestType,
        UpdateRequestType = r.UpdateRequestType,
        HasCreate = r.HasCreate,
        HasUpdate = r.HasUpdate,
        HasDelete = r.HasDelete,
        HasDetail = r.HasDetail,
        Policy = r.Policy,
        Icon = r.Icon,
        Order = r.Order,
        GeneratedAt = DateTimeOffset.UtcNow,
        SourceMapPath = $"{r.ApiPath.TrimStart('/').Replace('/', '_')}.source.json",
        Properties = SchemaInspector.DescribeProperties(r.DtoType)
    };
}

public sealed record GenerationResult(int PageCount, int NavFragmentCount, IReadOnlyList<string> Warnings);