using Microsoft.OpenApi.Models;

namespace HalalChain.Platform.Api.CodeGen.OpenApi;

public sealed class AdminUiExtractor
{
    public IReadOnlyList<GeneratedResource> Extract(OpenApiDocument doc, GeneratorConfig cfg)
    {
        var resources = new List<GeneratedResource>();

        foreach (var (path, pathItem) in doc.Paths)
        {
            if (ShouldSkip(path, pathItem, cfg)) continue;

            var listOp = FindListOperation(pathItem);
            if (listOp is null || !TryGetAdminUi(listOp, out var meta)) continue;

            var dtoType = ExtractDtoType(listOp);
            if (dtoType is null) continue;

            var segment = meta.RouteSegment;
            var hasCreate = pathItem.Operations.ContainsKey(OperationType.Post);
            var hasUpdate = pathItem.Operations.Keys.Any(k => k is OperationType.Put or OperationType.Patch);
            var hasDelete = pathItem.Operations.ContainsKey(OperationType.Delete);
            var hasGetById = doc.Paths.ContainsKey($"{path}/{{id}}");

            resources.Add(new GeneratedResource(
                Name: meta.DisplayName,
                Segment: segment,
                BasePath: path,
                ApiPath: path,
                DtoType: dtoType,
                CreateRequestType: hasCreate ? ExtractBodyType(pathItem.Operations[OperationType.Post]) : null,
                UpdateRequestType: hasUpdate ? ExtractBodyType(
                    pathItem.Operations.First(kv => kv.Key is OperationType.Put or OperationType.Patch).Value) : null,
                HasList: true,
                HasCreate: hasCreate,
                HasUpdate: hasUpdate,
                HasDelete: hasDelete,
                HasDetail: hasGetById,
                Order: meta.Order,
                Icon: meta.Icon,
                Policy: meta.Policy,
                Roles: meta.Roles));
        }

        return resources.OrderBy(r => r.Order).ThenBy(r => r.Name).ToList();
    }

    private static bool ShouldSkip(string path, OpenApiPathItem item, GeneratorConfig cfg)
    {
        foreach (var rule in cfg.Skip)
        {
            if (rule.Path is { } p && GlobMatch(p, path)) return true;
            if (rule.Controller is { } c && ControllerMatches(c, item)) return true;
        }
        return false;
    }

    private static bool TryGetAdminUi(OpenApiOperation op, out AdminUiMeta meta)
    {
        meta = default!;
        if (!op.Extensions.TryGetValue("x-admin-ui", out var extRaw)) return false;
        if (extRaw is not OpenApiObject ext) return false;

        meta = new AdminUiMeta(
            DisplayName: (ext["displayName"] as OpenApiString)?.Value ?? "Untitled",
            RouteSegment: (ext["routeSegment"] as OpenApiString)?.Value ?? "untitled",
            Order: (ext["order"] as OpenApiInteger)?.Value ?? 100,
            Icon: (ext["icon"] as OpenApiString)?.Value ?? "widgets",
            Policy: (ext["policy"] as OpenApiString)?.Value ?? "",
            Roles: (ext["roles"] as OpenApiArray)?.OfType<OpenApiString>().Select(s => s.Value).ToArray() ?? []);
        return true;
    }

    private static OpenApiOperation? FindListOperation(OpenApiPathItem item)
    {
        if (item.Operations.TryGetValue(OperationType.Get, out var getOp))
            return getOp;
        return null;
    }

    private static string? ExtractDtoType(OpenApiOperation op)
    {
        var schema = op.Responses
            .FirstOrDefault(r => r.Key.StartsWith("200"))
            .Value?.Content?.FirstOrDefault().Value?.Schema;

        if (schema is null) return null;
        return schema.Reference?.Id ?? schema.Items?.Reference?.Id;
    }

    private static string? ExtractBodyType(OpenApiOperation op) =>
        op.RequestBody?.Content?.FirstOrDefault().Value?.Schema?.Reference?.Id;

    private static bool GlobMatch(string pattern, string input)
    {
        var regex = "^" + Regex.Escape(pattern).Replace("\\*\\*", ".*").Replace("\\*", "[^/]*") + "$";
        return Regex.IsMatch(input, regex);
    }

    private static bool ControllerMatches(string controller, OpenApiPathItem item)
    {
        var opId = item.Operations.Values.FirstOrDefault()?.OperationId;
        return opId?.StartsWith(controller, StringComparison.Ordinal) == true;
    }
}

public sealed record AdminUiMeta(
    string DisplayName, string RouteSegment, int Order, string Icon, string Policy, string[] Roles);