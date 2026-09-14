using HalalChain.Platform.Contracts.Catalog.Dto;

using AutoMapper;

namespace HalalChain.Application.Common.Mappings;

public class ProductResponseProfile : Profile
{
    public ProductResponseProfile()
    {
        CreateMap<ProductDto, ProductDto>();
        CreateMap<HalalStatusDto, HalalStatusDto>();
        CreateMap<PagedResult<ProductDto>, PagedResult<ProductDto>>();
        CreateMap<EnrichedProductDto, EnrichedProductDto>();
    }
}
