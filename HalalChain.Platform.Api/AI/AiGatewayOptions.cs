namespace HalalChain.Platform.Api.AI;

public sealed class AiGatewayOptions
{
    public const string SectionName = "AiGateway";
    public string BaseUrl { get; set; } = "http://localhost:7071";
    public string? ApiKey { get; set; }
}

public sealed class TawheedOptions
{
    public const string SectionName = "Tawheed";
    public string BaseUrl { get; set; } = "http://localhost:8000";
}
