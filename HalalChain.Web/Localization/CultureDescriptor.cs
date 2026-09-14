namespace HalalChain.Localization;

public sealed record CultureDescriptor(
    string Id,
    string DisplayName,
    string NativeName,
    string Flag,
    string Direction,
    string Currency,
    string Region)
{
    public bool IsRtl => string.Equals(Direction, "rtl", StringComparison.OrdinalIgnoreCase);
}
