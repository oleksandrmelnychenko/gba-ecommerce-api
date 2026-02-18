namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class SupplyInvoiceBillOfLadingService : EntityBase {
    public long SupplyInvoiceId { get; set; }

    public long BillOfLadingServiceId { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }

    public decimal Value { get; set; }

    public decimal AccountingValue { get; set; }

    public bool IsCalculatedValue { get; set; }
}