using AutoMapper;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ProductTransfers;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class ProductTransfersMappingProfile : Profile {
    public ProductTransfersMappingProfile() {
        CreateMap<ProductTransferItem, ProductTransferItemDto>();

        CreateMap<ProductTransfer, ProductTransferDto>()
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.FromDate))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.Number))
            // .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => decimal.Round(src.PricePerItem * src.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero)))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization.Name))
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.MapFrom(src => src.Organization.USREOU))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.ProductTransferItems))
            .ForMember(dest => dest.Responsible,
                opt => opt.MapFrom(src => src.Responsible.FirstName + " " + src.Responsible.MiddleName + " " + src.Responsible.LastName))
            .ForMember(dest => dest.FromStorage, opt => opt.MapFrom(src => src.FromStorage.Name))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.ExchangeRate, opt => opt.MapFrom(src => src.ExchangeRate))
            .ForMember(dest => dest.ToStorage, opt => opt.MapFrom(src => src.ToStorage.Name));
    }
}