using System;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;

public sealed class SaleClientDto {
    public Guid NetUid { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public Guid GroupNetUid { get; set; }
    public string GroupName { get; set; }
    public string TIN { get; set; }
    public string USREOU { get; set; }
    public string SROI { get; set; }
    public bool VatPayer { get; set; } // HAS SROI 
    public string Region { get; set; }
    public string RegionCode { get; set; }
    public bool BuyerFolder { get; set; } = true;
    public string BuyerManager { get; set; }
    public string LegalAddress { get; set; }
    public string ActualAddress { get; set; }
    public string PhoneForSms { get; set; }
    public string PersonFullName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
}