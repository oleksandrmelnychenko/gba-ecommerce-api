using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public sealed class DeliveryDocumentDto {
    
    public Guid NetUid { get; set; }
    public ExtendedClientDto Client { get; set; }
    
    public ExtendedAgreementGTDDto Agreement { get; set; }
    // public CurrencyDto Currency { get; set; } 
    // public CurrencyDto CurrencyDocument { get; set; } 
    
    public SupplyOrganizationAgreementDto CustomsAgreement { get; set; }
    
    public decimal  exchangeRate { get; set; }
    public SupplyOrganizationDto Customs { get; set; }
    public bool IsElectronicDocument { get; set; }

    public bool CustomsValuation { get; set; }
    
    public DateTime DocumentDate { get; set; }

    public string CustomsNumber { get; set; }
    
    public string OrganizationName { get; set; }
    
    public string OrganizationUSREOU { get; set; }
    
    public string Comment { get; set; }
    public List<DeliveryDocumentItemDto> OrderItems { get; set; }
}