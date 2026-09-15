using System.Text;
using System.Text.Json;

namespace HalalChain.Web.Infrastructure;

public static class JwtTokenReader
{
    public static Guid? ReadUserId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            var padded = payload.Length % 4 != 0
                ? payload + new string('=', 4 - payload.Length % 4)
                : payload;
            var bytes = Convert.FromBase64String(padded);
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(bytes));
            if (doc.RootElement.TryGetProperty("sub", out var sub) &&
                Guid.TryParse(sub.GetString(), out var guid))
                return guid;
        }
        catch
        {
            return null;
        }
        return null;
    }

    public static string? ReadEmail(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            var padded = payload.Length % 4 != 0
                ? payload + new string('=', 4 - payload.Length % 4)
                : payload;
            var bytes = Convert.FromBase64String(padded);
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(bytes));
            if (doc.RootElement.TryGetProperty("email", out var email))
                return email.GetString();
        }
        catch
        {
            return null;
        }
        return null;
    }

    public static string? ReadDisplayName(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            var padded = payload.Length % 4 != 0
                ? payload + new string('=', 4 - payload.Length % 4)
                : payload;
            var bytes = Convert.FromBase64String(padded);
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(bytes));
            if (doc.RootElement.TryGetProperty("display_name", out var name))
                return name.GetString();
            if (doc.RootElement.TryGetProperty("unique_name", out var uniqueName))
                return uniqueName.GetString();
        }
        catch
        {
            return null;
        }
        return null;
    }

    public static string[] ReadRoles(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return Array.Empty<string>();
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return Array.Empty<string>();
            var payload = parts[1];
            var padded = payload.Length % 4 != 0
                ? payload + new string('=', 4 - payload.Length % 4)
                : payload;
            var bytes = Convert.FromBase64String(padded);
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(bytes));
            if (doc.RootElement.TryGetProperty("role", out var roles))
            {
                if (roles.ValueKind == JsonValueKind.Array)
                    return roles.EnumerateArray().Select(r => r.GetString() ?? "").ToArray();
                var single = roles.GetString();
                return single is not null ? new[] { single } : Array.Empty<string>();
            }
        }
        catch
        {
            // fall through
        }
        return Array.Empty<string>();
    }
}