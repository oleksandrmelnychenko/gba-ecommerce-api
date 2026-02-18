using System;
using AutoMapper;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.GbaDataExportModels.PaidServices;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class PaidServiceMappingProfile : Profile {
    public PaidServiceMappingProfile() {
        CreateMap<PackingListPackageOrderItemSupplyService, PaidServiceItemDto>()
            .ForMember(dest => dest.MeasureUnit, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product.MeasureUnit.CodeOneC))
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product))
            .ForMember(dest => dest.Qty, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.Qty))
            .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.TotalNetWeight))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => decimal.Round(src.PackingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero)))
            .ForMember(dest => dest.ExpenseAmount, opt => opt.MapFrom(src =>
                src.Currency.CodeOneC.Equals("980")
                    ? decimal.Round(src.NetValueUah, 2, MidpointRounding.AwayFromZero)
                    : decimal.Round(src.NetValueEur, 2, MidpointRounding.AwayFromZero)));
        // .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PerUnitPrice));


        CreateMap<MergedService, PaidServiceDto>()
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.FromDate))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.ServiceNumber))
            .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.Number))
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.SupplyOrganization))
            .ForMember(dest => dest.Agreement, opt => opt.MapFrom(src => src.SupplyOrganizationAgreement))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.AccountingGrossPrice))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.AccountingVat))
            .ForMember(dest => dest.VatRate, opt => opt.MapFrom(src => src.AccountingVatPercent))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.PackingListPackageOrderItemSupplyServices));
    }
}