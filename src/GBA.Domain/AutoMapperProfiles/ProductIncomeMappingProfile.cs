using System.Linq;
using AutoMapper;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ProductIncomeModels;

namespace GBA.Domain.AutoMapperProfiles;

public class ProductIncomeMappingProfile : Profile {
    public ProductIncomeMappingProfile() {
        CreateMap<ProductIncomeItem, ProductIncomeItemDto>()
            .ForMember(dest => dest.Qty, opt => opt.MapFrom(src => src.Qty))
            .ForMember(dest => dest.MeasureUnit, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product.MeasureUnit.CodeOneC))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.UnitPrice))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.TotalNetPrice + src.PackingListPackageOrderItem.VatAmount))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.VatAmount))
            .ForMember(dest => dest.VatPercent, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.VatPercent))
            .ForMember(dest => dest.NetWeight, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.NetWeight))
            .ForMember(dest => dest.Imported, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.SupplyInvoiceOrderItem.ProductIsImported))
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product));

        CreateMap<ProductIncome, ProductIncomeDto>()
            .ForMember(dest => dest.DocumentDate, opt => opt.MapFrom(src => src.FromDate))
            .ForMember(dest => dest.DocumentNumber, opt => opt.MapFrom(src => src.Number))
            .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.PackingList.SupplyInvoice.Number))
            .ForMember(dest => dest.InvoiceDate,
                opt => opt.MapFrom(src =>
                    src.ProductIncomeItems.First().PackingListPackageOrderItem.PackingList.SupplyInvoice.DateCustomDeclaration.HasValue
                        ? src.ProductIncomeItems.First().PackingListPackageOrderItem.PackingList.SupplyInvoice.DateCustomDeclaration.Value
                        : src.ProductIncomeItems.First().PackingListPackageOrderItem.PackingList.SupplyInvoice.DateFrom))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalNetWithVat))
            .ForMember(dest => dest.VatAmount, opt => opt.MapFrom(src => src.TotalVatAmount))
            .ForMember(dest => dest.IncludesVat, opt => opt.MapFrom(src => src.TotalVatAmount != 0))
            .ForMember(dest => dest.ExchangeRate, opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.ExchangeRateAmount))
            .ForMember(dest => dest.TotalNetWeight, opt => opt.MapFrom(src => src.TotalNetWeight))
            .ForMember(dest => dest.TotalGrossWeight, opt => opt.MapFrom(src => src.TotalGrossWeight))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Comment))
            .ForMember(dest => dest.Pricing,
                opt => opt.MapFrom(src =>
                    src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.ClientAgreement.Agreement.ProviderPricing.Name))
            .ForMember(dest => dest.StorageName, opt => opt.MapFrom(src => src.Storage.Name))
            .ForMember(dest => dest.OrganizationName,
                opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.Organization.Name))
            .ForMember(dest => dest.OrganizationUSREOU,
                opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.Organization.USREOU))
            .ForMember(dest => dest.VatRate,
                opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.Organization.VatRate.Value))
            .ForMember(dest => dest.Client,
                opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.Client))
            .ForMember(dest => dest.Agreement,
                opt => opt.MapFrom(src => src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.ClientAgreement.Agreement))
            .ForMember(dest => dest.AgreementCurrency,
                opt => opt.MapFrom(src =>
                    src.ProductIncomeItems.First().PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrder.ClientAgreement.Agreement.Currency))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.ProductIncomeItems))
            .ForMember(dest => dest.Responsible, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.MiddleName + " " + src.User.LastName));
    }
}