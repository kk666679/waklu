namespace HalalChain.Platform.Api.Infrastructure.CodeGen;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateAdminUiAttribute : Attribute
{
    public string? DisplayName { get; init; }
    public string? RouteSegment { get; init; }
    public int Order { get; init; } = 100;
    public string? Icon { get; init; }
    public string? Policy { get; init; }
    public string[] Roles { get; init; } = [];
}