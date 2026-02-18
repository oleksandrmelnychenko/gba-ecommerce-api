namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class SupplyInvoiceDeliveryDocument : EntityBase {
    public long SupplyInvoiceId { get; set; }

    public long SupplyDeliveryDocumentId { get; set; }

    public string DocumentUrl { get; set; }

    public string ContentType { get; set; }

    public string FileName { get; set; }

    public string GeneratedName { get; set; }

    public string Number { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public SupplyDeliveryDocument SupplyDeliveryDocument { get; set; }
}