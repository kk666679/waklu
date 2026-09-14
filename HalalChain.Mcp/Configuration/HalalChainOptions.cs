using System.Text.Json.Serialization;

namespace HalalChain.Mcp.Configuration;

public sealed class HalalChainOptions
{
    public const string SectionName = "HalalChain";

    public string SolutionRoot { get; set; } = "";

    public ServiceEndpoints Services { get; set; } = new();

    public McpOptions Mcp { get; set; } = new();

    public static string GetEnvironmentVariablePrefix() => "HALALCHAIN_";

    public static string GetSolutionRootEnvVar() => $"{GetEnvironmentVariablePrefix()}SOLUTION_ROOT";
}

public sealed class ServiceEndpoints
{
    public string PlatformApi { get; set; } = "http://localhost:5001";
    public string HalalChain { get; set; } = "http://localhost:5200";
    public string Marketplace { get; set; } = "http://localhost:5201";
    public string Tawheed { get; set; } = "http://localhost:8000";
    public string AiInference { get; set; } = "http://localhost:7071";
}

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public int MaxRequestBytes { get; set; } = 1_048_576;

    public int ToolTimeoutSeconds { get; set; } = 30;
}
