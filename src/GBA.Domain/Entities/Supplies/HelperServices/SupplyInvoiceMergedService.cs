namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class SupplyInvoiceMergedService : EntityBase {
    public long SupplyInvoiceId { get; set; }

    public long MergedServiceId { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public MergedService MergedService { get; set; }

    public decimal Value { get; set; }

    public decimal AccountingValue { get; set; }

    public bool IsCalculatedValue { get; set; }

    public decimal ExchangeRateEurToAgreementCurrency { get; set; }

    public decimal ExchangeRateEurToUah { get; set; }
}