using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace HalalChain.Application.Common.Mappings;

public static class MappingConfiguration
{
    public static IServiceCollection AddApplicationMapping(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<CatalogProfile>();
            cfg.AddProfile<ProductResponseProfile>();
        });

        return services;
    }
}
