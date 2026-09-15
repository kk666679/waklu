namespace HalalChain.Platform.Http.Flags;

public sealed class FeatureFlagOptions
{
    public Dictionary<string, FlagDefinition> Flags { get; set; } = new();
}

public sealed class FlagDefinition
{
    public bool Enabled { get; set; }
    public int RolloutPercent { get; set; } = 0;
    public string[]? AllowedRoles { get; set; }
    public string[]? AllowedUserIds { get; set; }
}