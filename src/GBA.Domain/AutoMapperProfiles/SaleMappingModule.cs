using System;
using System.Linq;
using AutoMapper;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;

namespace GBA.Domain.AutoMapperProfiles;

public class SaleMappingModule : Profile {
    public SaleMappingModule() {
        CreateMap<Client, SaleClientDto>(MemberList.None)
            .ForMember(dest => dest.VatPayer, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SROI)))
            .ForMember(dest => dest.GroupNetUid, opt => opt.MapFrom(src => src.ClientInRole.ClientTypeRole.NetUid))
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.ClientInRole.ClientTypeRole.Name))
            .ForMember(dest => dest.BuyerFolder, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.BuyerManager, opt => opt.MapFrom(src => src.MainManager != null ? src.FirstName + " " + src.MiddleName + " " + src.LastName : string.Empty))
            .ForMember(dest => dest.PersonFullName, opt => opt.MapFrom(src => src.FirstName + " " + src.MiddleName + " " + src.LastName))
            .ForMember(dest => dest.Region, opt => opt.MapFrom(src => src.Region != null ? src.Region.Name : string.Empty))
            .ForMember(dest => dest.RegionCode, opt => opt.MapFrom(src => src.RegionCode != null ? src.RegionCode.Value : string.Empty));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.StorageName, opt => opt.MapFrom(src => src.Storage.Name))
            .ForMember(dest => dest.MeasureUnit, opt => opt.MapFrom(src => src.Product.MeasureUnit.CodeOneC))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => decimal.Round(src.PricePerItem * src.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero)))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmountLocal))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.TotalVat))
            .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.OneTimeDiscount));

        CreateMap<OrderItem, ExtendedOrderItemDto>()
            .IncludeBase<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.Sale, opt => opt.MapFrom(src => src.Order.Sale));

        CreateMap<Agreement, SaleAgreementDto>()
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization.Name))
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.MapFrom(src => src.Organization.USREOU))
            .ForMember(dest => dest.VatRate, opt => opt.MapFrom(src => src.Organization.VatRate.Value));

        CreateMap<Sale, SaleDto>()
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.SaleNumber.Value))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmountLocal))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.Order.TotalVat))
            .ForMember(dest => dest.ExchangeRate, opt => opt.MapFrom(src => src.Order.OrderItems.FirstOrDefault().ExchangeRateAmount))
            .ForMember(dest => dest.Pricing, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Pricing.Name))
            .ForMember(dest => dest.IncludesVat, opt => opt.MapFrom(src => src.IsVatSale))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Organization.Name))
            .ForMember(dest => dest.OrganizationUSREOU, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Organization.USREOU))
            .ForMember(dest => dest.VatRate, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Organization.VatRate.Value))
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.ClientAgreement.Client))
            .ForMember(dest => dest.Agreement, opt => opt.MapFrom(src => src.ClientAgreement.Agreement))
            .ForMember(dest => dest.AgreementCurrency, opt => opt.MapFrom(src => src.ClientAgreement.Agreement.Currency))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.Order.OrderItems))
            .ForMember(dest => dest.Responsible, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.MiddleName + " " + src.User.LastName))
            .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.ShipmentDate, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Transporter, opt => opt.MapFrom(src => src.Transporter.Name));

        CreateMap<Sale, InvoiceDto>()
            .IncludeBase<Sale, SaleDto>()
            .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.ChangedToInvoice));
    }
}