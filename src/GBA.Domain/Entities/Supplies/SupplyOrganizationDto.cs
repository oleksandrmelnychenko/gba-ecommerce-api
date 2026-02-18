using System;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyOrganizationDto {
    public Guid NetUid { get; set; }
    public string FullName { get; set; }
    public string Name { get; set; }
    public string TIN { get; set; }
    public string USREOU { get; set; }
    public string SROI { get; set; }
    public bool IsNotResident { get; set; }
    public bool IsIndividual { get; set; }
    public string GroupName { get; set; }
    public string OriginalRegionCode { get; set; }
    public bool VatPayer { get; set; }
    public string Address { get; set; }
    public string EmailAddress { get; set; }
    public string ActualAddress { get; set; }
    public string LegalAddress { get; set; }
    public string ClientNumber { get; set; }
    public string SMSNumber { get; set; }
    public string AccountNumber { get; set; }
    public string ContactPersonName { get; set; }
}
