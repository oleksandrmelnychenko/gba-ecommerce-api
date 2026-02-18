using AutoMapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ProductCapitalizations;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class ProductCapitalizationMappingProfile : Profile {
    public ProductCapitalizationMappingProfile() {
        CreateMap<ProductCapitalization, ProductCapitalizationDto>()
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization.Name))
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.MapFrom(src => src.Organization.USREOU))
            .ForMember(dest => dest.VatRate, opt => opt.MapFrom(src => src.Organization.VatRate.Value))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.StorageName, opt => opt.MapFrom(src => src.Storage.Name))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.ProductCapitalizationItems))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.Number))
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.FromDate));

        CreateMap<ProductCapitalizationItem, ProductCapitalizationItemDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.UnitPrice));
    }
}