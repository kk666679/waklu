using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

public sealed class GetArchitectureTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public GetArchitectureTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_get_architecture";

    public string Description => "Returns the HalalChain system architecture diagram showing all services, data flows, infrastructure, and discovered projects.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("HalalChain architecture");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var arch = _platformData.GetArchitecture();
        var services = _platformData.GetServices();

        var output = $"""
            # HalalChain Architecture

            ## System Diagram

            ```
            {arch.Trim()}
            ```

            ## Service Layer

            The frontend communicates with these services:
            """;

        foreach (var s in services.Where(s => s.Interfaces.Any()))
        {
            output += $"\n- **{s.Name}** ({s.ProjectName}) — implements {string.Join(", ", s.Interfaces)}";
            if (s.Methods.Any())
                output += $"\n  Methods: {string.Join(", ", s.Methods.Take(5))}";
        }

        output += """

            ## Data Contracts

            Shared DTOs from `HalalChain.Platform.Contracts`:
            - `ProductDto`, `CategoryDto`, `HalalStatusDto` — Catalog
            - `OrderDto`, `CartDto`, `CustomerDto` — Commerce
            - `VendorDto` — Vendors
            - `VerificationDto`, `HalalCertificateDto` — Halal
            """;

        return Task.FromResult(output);
    }
}
