using System;
using System.Linq;
using AutoMapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.GbaDataExportModels;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class GbaDataExportMappingProfile : Profile {
    private const string Assigned = "Assigned";
    private const string Calculated = "Calculated";
    private const string UahCode = "980";

    private readonly (string, string, string) assignedPriceType = (Assigned, "Базовый", "");
    private readonly (string, string, string) calculatedPriceType = (Calculated, "Динамический", "По процентной наценке на базовый тип");
    
    public GbaDataExportMappingProfile() {
        CreateMap<Pricing, PricingDto>()
            .ForMember(dest => dest.Vat, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.PriceType, opt => opt.MapFrom(src => src.PriceType.Name.Equals(assignedPriceType.Item1) ? assignedPriceType.Item2 : calculatedPriceType.Item2))
            .ForMember(dest => dest.KindPriceType, opt => opt.MapFrom(src => src.PriceType.Name.Equals(assignedPriceType.Item1) ? assignedPriceType.Item2 : calculatedPriceType.Item2))
            .ForMember(dest => dest.MethodPriceCalculation, opt => opt.MapFrom(src => src.PriceType.Name.Equals(assignedPriceType.Item1) ? assignedPriceType.Item3 : calculatedPriceType.Item3));
        
        CreateMap<Client, ClientDto>()
            .ForMember(dest => dest.VatPayer, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SROI)))
            .ForMember(dest => dest.GroupNetUid, opt => opt.MapFrom(src => src.ClientInRole.ClientTypeRole.NetUid))
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.ClientInRole.ClientTypeRole.Name));

        CreateMap<Client, ExtendedClientDto>(MemberList.None)
            .ForMember(dest => dest.VatPayer, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SROI)))
            .ForMember(dest => dest.GroupNetUid, opt => opt.MapFrom(src => src.ClientInRole.ClientTypeRole.NetUid))
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.ClientInRole.ClientTypeRole.Name))
            .ForMember(dest => dest.IsSupplyOrganization, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Agreements, opt => opt.MapFrom(src => src.ClientAgreements.Select(e => e.Agreement)));
            
        CreateMap<SupplyOrganization, ClientDto>()
            .ForMember(dest => dest.VatPayer, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SROI)))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => "Постачальники послуг"))
            .ForMember(dest => dest.GroupNetUid, opt => opt.MapFrom(src => Guid.Empty));

        CreateMap<SupplyOrganizationAgreement, Agreement>(MemberList.None)
            .ForMember(dest => dest.FromDate, opt => opt.MapFrom(src => src.ExistFrom))
            .ForMember(dest => dest.ToDate, opt => opt.MapFrom(src => src.ExistTo));

        CreateMap<Agreement, ExtendedAgreementGTDDto>(MemberList.None) 
            .ForMember(d => d.CurrencyDocument,o => o.MapFrom(s => s.Currency));

        CreateMap<SupplyOrganizationAgreement, ExtendedAgreementDto>(MemberList.None)
            .ForMember(dest => dest.Foreign, opt => opt.MapFrom(src => !src.Currency.Code.Equals(UahCode)))
            .ForMember(dest => dest.FromDate, opt => opt.MapFrom(src => src.ExistFrom))
            .ForMember(dest => dest.ToDate, opt => opt.MapFrom(src => src.ExistTo));
        
        CreateMap<SupplyOrganization, ExtendedClientDto>(MemberList.None)
            .ForMember(dest => dest.VatPayer, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SROI)))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => "Постачальники послуг"))
            .ForMember(dest => dest.GroupNetUid, opt => opt.MapFrom(src => Guid.Empty))
            .ForMember(dest => dest.IsSupplyOrganization, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Agreements, opt => opt.MapFrom(src => src.SupplyOrganizationAgreements));
        CreateMap<SupplyOrganizationAgreement, SupplyOrganizationAgreementDto>()
            .ForMember(dest => dest.FromDate, opt => opt.MapFrom(src => src.ExistFrom))
            .ForMember(dest => dest.ToDate, opt => opt.MapFrom(src => src.ExistTo));
        CreateMap<SupplyOrganization, SupplyOrganizationDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.VatPayer, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.SROI)))
            .ForMember(dest => dest.ClientNumber, opt => opt.MapFrom(src => src.PhoneNumber))
             .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => "Постачальники послуг"))
            .ForMember(dest => dest.SMSNumber, opt => opt.MapFrom(src => src.ContactPersonPhone))
            .ForMember(dest => dest.IsIndividual, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ContactPersonPhone)))
            .ForMember(dest => dest.ActualAddress, opt => opt.MapFrom(src => src.ContactPersonEmail))
            // .ForMember(dest => dest.SROI, opt => opt.Ignore())
             .ForMember(dest => dest.LegalAddress, opt => opt.MapFrom(src => src.ContactPersonEmail));



        CreateMap<MeasureUnit, MeasureUnitDto>();
        CreateMap<Agreement, AgreementDto>();
        CreateMap<Agreement, ExtendedAgreementDto>()
            .ForMember(dest => dest.Foreign, opt => opt.MapFrom(src => !src.Currency.Code.Equals(UahCode)));
        
        CreateMap<Currency, CurrencyDto>();
        
        CreateMap<ProductGroup, ProductGroupDto>()
            .ForMember(dest => dest.ParentNetUid, opt => opt
                .MapFrom(src => src.RootProductGroup != null ? (Guid?)src.RootProductGroup.NetUid : null));
        
        CreateMap<ProductSpecification, ProductSpecificationDto>();
        
        CreateMap<Product, BaseProductExportModel>()
            .ForMember(dest => dest.ProductSpecification, opt => opt.MapFrom(src => src.ProductSpecifications.FirstOrDefault()))
            .ForMember(dest => dest.ParentGroupNetUid, opt => opt
                .MapFrom(src => src.ProductProductGroups.FirstOrDefault().ProductGroup.RootProductGroup != null 
                    ? src.ProductProductGroups.FirstOrDefault().ProductGroup.RootProductGroup.NetUid 
                    : src.ProductProductGroups.FirstOrDefault().ProductGroup.NetUid));
        
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.ProductSpecification, opt => opt.MapFrom(src => src.ProductSpecifications.FirstOrDefault()))
            .ForMember(dest => dest.ProductGroup, opt => opt.MapFrom(src => src.ProductProductGroups.FirstOrDefault().ProductGroup))
            .ForMember(dest => dest.ParentGroupNetUid, opt => opt
                .MapFrom(src => src.ProductProductGroups.FirstOrDefault().ProductGroup.RootProductGroup != null 
                    ? src.ProductProductGroups.FirstOrDefault().ProductGroup.RootProductGroup.NetUid 
                    : src.ProductProductGroups.FirstOrDefault().ProductGroup.NetUid));
    }
}