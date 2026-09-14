using HalalChain.Platform.Http.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HalalChain.Platform.Http.Extensions;

public static class PlatformApiClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IPlatformApiClient"/> with the shared concrete
    /// <see cref="PlatformApiClient"/>. Must be paired with
    /// <see cref="ServiceCollectionExtensions.AddPlatformHttpClient"/> (or its
    /// cookie-token variant) so the underlying <c>PlatformApiSender</c> is
    /// available.
    /// </summary>
    public static IServiceCollection AddPlatformApiClient(this IServiceCollection services)
    {
        services.AddScoped<IPlatformApiClient, PlatformApiClient>();
        return services;
    }
}
