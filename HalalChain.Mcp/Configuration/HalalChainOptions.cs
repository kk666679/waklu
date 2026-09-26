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

    /// <summary>
    /// How long the server waits for a client to answer a server-initiated
    /// request (sampling or elicitation) before giving up and returning null.
    /// </summary>
    public int ClientRequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Ceiling on a single resource payload. Bounds the damage a hostile or
    /// accidental <c>resources/read</c> can do to the host's memory.
    /// </summary>
    public int MaxResourceBytes { get; set; } = 524_288;

    /// <summary>Page size for the cursor-paginated list methods.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Serve <c>resources/list</c> and <c>resources/read</c>.</summary>
    public bool EnableResources { get; set; } = true;

    /// <summary>Serve <c>prompts/list</c> and <c>prompts/get</c>.</summary>
    public bool EnablePrompts { get; set; } = true;

    /// <summary>Serve <c>completion/complete</c> for prompt and resource arguments.</summary>
    public bool EnableCompletions { get; set; } = true;

    /// <summary>Emit <c>notifications/progress</c> when a request carries a progress token.</summary>
    public bool EnableProgress { get; set; } = true;

    /// <summary>Forward logs to the client as <c>notifications/message</c>.</summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Allow the server to call back into the client's model via
    /// <c>sampling/createMessage</c>. Requires the client to declare sampling.
    /// </summary>
    public bool EnableSampling { get; set; } = true;

    /// <summary>
    /// Allow the server to ask the user for input via <c>elicitation/create</c>.
    /// Requires the client to declare elicitation.
    /// </summary>
    public bool EnableElicitation { get; set; } = true;

    /// <summary>Minimum severity written to stderr. Defaults to information.</summary>
    public LogLevelOption LogLevel { get; set; } = LogLevelOption.Information;
}

/// <summary>
/// Log severity as a configuration value. A dedicated enum keeps the
/// configuration surface readable and lets the binder accept either
/// <c>Warning</c> or <c>warn</c>.
/// </summary>
public enum LogLevelOption
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None,
}
