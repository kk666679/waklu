using System.Text;
using System.Text.Json;

namespace HalalChain.Platform.Http.Utils;

public static class QueryString
{
    public static string Build(object? query)
    {
        if (query is null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var prop in query.GetType().GetProperties())
        {
            if (prop.GetValue(query) is not { } value) continue;
            Append(sb, prop.Name, value);
        }
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string name, object value)
    {
        switch (value)
        {
            case null:
                return;
            case bool b:
                sb.Append(sb.Length == 0 ? '?' : '&').Append(name).Append('=').Append(b ? "true" : "false");
                break;
            case System.Collections.IEnumerable e and not string:
                foreach (var item in e)
                {
                    if (item is null) continue;
                    sb.Append(sb.Length == 0 ? '?' : '&').Append(name).Append('=').Append(Uri.EscapeDataString(item.ToString()!));
                }
                break;
            default:
                sb.Append(sb.Length == 0 ? '?' : '&').Append(name).Append('=').Append(Uri.EscapeDataString(value.ToString()!));
                break;
        }
    }
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
