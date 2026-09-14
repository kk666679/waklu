using AutoMapper;
using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Vendors.Commands;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Vendors.Requests;

namespace HalalChain.Application.Common.Mappings;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<CreateProductRequest, CreateProductCommand>();
        CreateMap<UpdateProductRequest, UpdateProductCommand>();
        CreateMap<RegisterVendorRequest, RegisterVendorCommand>();
        CreateMap<UpdateVendorRequest, UpdateVendorCommand>();
    }
}
