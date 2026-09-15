using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using HalalChain.Platform.Http.Abstractions;

namespace HalalChain.Platform.Http.Flags;

public sealed class ConfigFeatureFlag : IFeatureFlag
{
    private readonly IOptionsMonitor<FeatureFlagOptions> _options;

    public ConfigFeatureFlag(IOptionsMonitor<FeatureFlagOptions> options)
    {
        _options = options;
    }

    public bool IsEnabled(string name)
    {
        var def = _options.CurrentValue.Flags.GetValueOrDefault(name);
        return def?.Enabled == true;
    }

    public Task<bool> IsEnabledAsync(string name, CancellationToken ct = default)
        => Task.FromResult(IsEnabled(name));

    public Task<bool> IsEnabledAsync(string name, Guid? userId, CancellationToken ct = default)
    {
        var def = _options.CurrentValue.Flags.GetValueOrDefault(name);
        if (def?.Enabled != true) return Task.FromResult(false);

        if (def.RolloutPercent > 0 && userId.HasValue)
        {
            var bucket = StableBucketing.Compute(userId.Value, name, def.RolloutPercent);
            return Task.FromResult(bucket < def.RolloutPercent);
        }

        if (def.AllowedRoles?.Length > 0 || def.AllowedUserIds?.Length > 0)
        {
            // Role/user checks require auth context — caller must pass userId
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}