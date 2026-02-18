using System.Linq;
using AutoMapper;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.GbaDataExportModels;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class DeliveryDocumentMappingProfile : Profile {
    public DeliveryDocumentMappingProfile() {

        CreateMap<PackingListPackageOrderItem, DeliveryDocumentItemDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product))
            .ForMember(dest => dest.MeasureUnit, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product.MeasureUnit.CodeOneC))
            .ForMember(dest => dest.Qty, opt => opt.MapFrom(src => src.Qty))
            .ForMember(dest => dest.InvoiceAmount, opt => opt.MapFrom(src => src.UnitPrice))
            .ForMember(dest => dest.CustomsValue, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product.ProductSpecifications.LastOrDefault().CustomsValue))
            .ForMember(dest => dest.DutyPercent, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product.ProductSpecifications.LastOrDefault().DutyPercent))
            .ForMember(dest => dest.Duty, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product.ProductSpecifications.LastOrDefault().Duty))
            .ForMember(dest => dest.VatPercent, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product.ProductSpecifications.LastOrDefault().VATPercent))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.SupplyInvoiceOrderItem.Product.ProductSpecifications.LastOrDefault().VATValue));

        CreateMap<PackingList, DeliveryDocumentDto>()
            .ForMember(dest => dest.IsElectronicDocument, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CustomsValuation, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.PackingListPackageOrderItems))
            .ForMember(dest => dest.Client, opt => opt.Ignore())
            // .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.Comment, opt => opt.Ignore())
            .ForMember(dest => dest.Agreement, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentDate, opt => opt.Ignore())
            .ForMember(dest => dest.CustomsNumber, opt => opt.Ignore())
            .ForMember(dest => dest.OrganizationName, opt => opt.Ignore())
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.Ignore())
            .ForMember(dest => dest.Customs, opt => opt.Ignore())
            .ForMember(dest => dest.CustomsAgreement, opt => opt.Ignore())
            .ForMember(dest => dest.exchangeRate, opt => opt.Ignore());

    }
}