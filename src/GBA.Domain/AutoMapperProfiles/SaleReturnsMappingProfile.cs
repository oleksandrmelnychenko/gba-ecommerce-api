using System;
using AutoMapper;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.EntityHelpers.GbaDataExportModels.SaleReturns;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class SaleReturnsMappingProfile : Profile {
    public SaleReturnsMappingProfile() {
        CreateMap<SaleReturnItem, SaleReturnItemDto>()
            .ForMember(dest => dest.StorageName, opt => opt.MapFrom(src => src.Storage.Name))
            .ForMember(dest => dest.OrderItemStorageName, opt => opt.MapFrom(src => src.OrderItem.Storage.Name))
            .ForMember(dest => dest.MeasureUnit, opt => opt.MapFrom(src => src.OrderItem.Product.MeasureUnit.CodeOneC))
            .ForMember(dest => dest.Price,
                opt => opt.MapFrom(src => decimal.Round(src.OrderItem.PricePerItem * src.OrderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero)))
            .ForMember(dest => dest.OrderItemAmount, opt => opt.MapFrom(src => src.OrderItem.TotalAmountLocal))
            .ForMember(dest => dest.OrderItemVatAmount, opt => opt.MapFrom(src => src.OrderItem.TotalVat))
            .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.OrderItem.OneTimeDiscount))
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.OrderItem.Product))
            .ForMember(dest => dest.Sale, opt => opt.MapFrom(src => src.OrderItem.Order.Sale));

        CreateMap<SaleReturn, SaleReturnDto>()
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.FromDate))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.Number))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmountLocal))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.TotalVatAmount))
            .ForMember(dest => dest.ExchangeRate, opt => opt.MapFrom(src => src.ExchangeRate))
            .ForMember(dest => dest.IncludesVat, opt => opt.MapFrom(src => src.Storage.ForVatProducts))
            .ForMember(dest => dest.Pricing, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Pricing.Name))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Organization.Name))
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Organization.USREOU))
            .ForMember(dest => dest.VatRate, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Organization.VatRate.Value))
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client))
            .ForMember(dest => dest.Agreement, opt => opt.MapFrom(src => src.ClientAgreement.Agreement))
            .ForMember(dest => dest.AgreementCurrency, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Currency))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.SaleReturnItems))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => string.Empty));
        // .ForMember(dest => dest.Responsible, opt => opt.MapFrom(src => string.Concat(src.User.FirstName, " ", src.User.MiddleName, " " , src.User.LastName)))
        // .ForMember(dest => dest.Sale, opt => opt.MapFrom(src => src.Sale));
    }
}