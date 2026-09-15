using System.Reflection;

namespace HalalChain.Platform.Api.CodeGen.OpenApi;

public static class SchemaInspector
{
    public static IReadOnlyList<PropertyInfo> DescribeProperties(string dtoTypeName)
    {
        // This is a placeholder - in real implementation we'd load the schema from OpenAPI
        // and return property metadata. For now return empty list.
        return Array.Empty<PropertyInfo>();
    }
}