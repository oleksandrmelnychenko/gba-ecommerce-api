using System;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;

public sealed class SaleAgreementDto {
    public Guid NetUid { get; set; }
    public string OrganizationName { get; set; }
    public string OrganizationUSREOU { get; set; }
    public double VatRate { get; set; }
    public string Name { get; set; }
    public string Number { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public CurrencyDto Currency { get; set; }
    public decimal AmountDebt { get; set; }
    public bool IsControlAmountDebt { get; set; }
    public bool IsControlNumberDaysDebt { get; set; }
    public int NumberDaysDebt { get; set; }
    public PricingDto Pricing { get; set; }
}