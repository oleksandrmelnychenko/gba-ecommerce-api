using System;
using AutoMapper;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.EntityHelpers.GbaDataExportModels.DepreciatedOrders;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class DepreciatedOrderMappingProfile : Profile {
    public DepreciatedOrderMappingProfile() {
        CreateMap<DepreciatedOrderItem, DepreciatedOrderItemDto>()
            .ForMember(dest => dest.MeasureUnit, opt => opt.MapFrom(src => src.Product.MeasureUnit.CodeOneC))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => decimal.Round(src.PerUnitPrice * (decimal)src.Qty, 2, MidpointRounding.AwayFromZero)))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PerUnitPrice));

        CreateMap<DepreciatedOrder, DepreciatedOrderDto>()
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.FromDate))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.Number))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization.Name))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.ExchangeRate, opt => opt.MapFrom(src => src.ExchangeRate))
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.MapFrom(src => src.Organization.USREOU))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.DepreciatedOrderItems))
            .ForMember(dest => dest.Responsible,
                opt => opt.MapFrom(src => src.Responsible.FirstName + " " + src.Responsible.MiddleName + " " + src.Responsible.LastName))
            .ForMember(dest => dest.StorageName, opt => opt.MapFrom(src => src.Storage.Name));
    }
}