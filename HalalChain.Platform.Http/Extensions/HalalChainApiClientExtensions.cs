using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Flags;
using HalalChain.Platform.Http.Http;
using HalalChain.Platform.Http.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HalalChain.Platform.Http.Extensions;

public static class HalalChainApiClientExtensions
{
    public static IServiceCollection AddHalalChainApiClient(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();

        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddHttpClient<IApiClient, ResilientApiClient>(c =>
        {
            c.BaseAddress = new Uri(config["Api:BaseUrl"]
                ?? config["PlatformApi:BaseUrl"]
                ?? throw new InvalidOperationException("Api:BaseUrl or PlatformApi:BaseUrl is required."));
            c.Timeout = Timeout.InfiniteTimeSpan;
            c.DefaultRequestHeaders.Add("X-Client", "HalalChain/1.0");
        })
        .AddHttpMessageHandler<CorrelationIdHandler>()
        .AddStandardResilienceHandler();

        services.Configure<FeatureFlagOptions>(config.GetSection("FeatureFlags"));
        services.AddSingleton<IFeatureFlag, ConfigFeatureFlag>();

        return services;
    }

    public static IServiceCollection AddRoutedService<TInterface, TInMemory, TApi, TRouter>(
        this IServiceCollection services)
        where TInterface : class
        where TInMemory : class, TInterface
        where TApi : class, TInterface
        where TRouter : class, TInterface
    {
        services.AddScoped<TInMemory>();
        services.AddScoped<TApi>();
        services.AddScoped<TInterface, TRouter>();
        return services;
    }
}