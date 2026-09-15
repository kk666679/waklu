using HalalChain.Platform.Http;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Flags;
using Microsoft.Extensions.DependencyInjection;

namespace HalalChain.Web.Infrastructure;

public static class HttpResponseServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformWebInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlag, ConfigFeatureFlag>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddTransient<CorrelationIdHandler>();
        services.AddHttpClient<PlatformApiSender>()
            .AddHttpMessageHandler<CorrelationIdHandler>();
        return services;
    }
}