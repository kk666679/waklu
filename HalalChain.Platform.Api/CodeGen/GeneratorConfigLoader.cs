using System.Text.Json;
using Json.Schema;

namespace HalalChain.Platform.Api.CodeGen;

public static class GeneratorConfigLoader
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static GeneratorConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Generator config not found: {configPath}");

        var json = File.ReadAllText(configPath);
        ValidateAgainstSchema(json, Path.ChangeExtension(configPath, ".schema.json"));

        var cfg = JsonSerializer.Deserialize<GeneratorConfig>(json, Json)
                  ?? throw new InvalidOperationException("Failed to deserialize generator.json.");

        ValidateSemantic(cfg, configPath);
        return cfg;
    }

    private static void ValidateAgainstSchema(string json, string schemaPath)
    {
        if (!File.Exists(schemaPath)) return;
        var schema = JsonSchema.FromText(File.ReadAllText(schemaPath));
        var doc = JsonNode.Parse(json)!;
        var result = schema.Evaluate(doc, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            var errors = string.Join("\n", result.Details
                .Where(d => d.HasErrors)
                .SelectMany(d => d.Errors!.Select(e => $"  {d.InstanceLocation}: {e.Value}")));
            throw new InvalidOperationException($"generator.json failed schema validation:\n{errors}");
        }
    }

    private static void ValidateSemantic(GeneratorConfig cfg, string path)
    {
        var baseDir = Path.GetDirectoryName(path)!;

        foreach (var (name, tpl) in new[]
        {
            ("list", cfg.Templates.List),
            ("form", cfg.Templates.Form),
            ("detail", cfg.Templates.Detail)
        })
        {
            var full = Path.GetFullPath(Path.Combine(baseDir, tpl));
            if (!File.Exists(full))
                throw new InvalidOperationException($"Template '{name}' not found: {full}");
        }

        foreach (var ov in cfg.Overrides.Values)
        {
            foreach (var p in new[] { ov.ListPage, ov.FormPage, ov.DetailPage })
            {
                if (p is null) continue;
                var full = Path.GetFullPath(Path.Combine(baseDir, "..", "..", p));
                if (!File.Exists(full))
                    throw new InvalidOperationException($"Override page not found: {full}");
            }
        }
    }
}