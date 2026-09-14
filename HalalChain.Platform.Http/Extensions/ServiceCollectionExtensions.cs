using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.TokenAccessors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HalalChain.Platform.Http.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PlatformApiSender"/> with a typed HttpClient.
    /// The accessor is registered as scoped by default; pass a different
    /// lifetime via the configure accessor delegate if your host needs it.
    /// </summary>
    public static IServiceCollection AddPlatformHttpClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient<PlatformApiSender>((sp, client) =>
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            configureClient?.Invoke(client);
        });
        return services;
    }

    /// <summary>
    /// Registers the cookie-based token accessor (MVC) alongside the shared sender.
    /// </summary>
    public static IServiceCollection AddPlatformHttpClientWithCookieToken(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
    {
        services.AddPlatformHttpClient(configureClient);
        services.AddScoped<IPlatformTokenAccessor, CookieTokenAccessor>();
        return services;
    }
}
