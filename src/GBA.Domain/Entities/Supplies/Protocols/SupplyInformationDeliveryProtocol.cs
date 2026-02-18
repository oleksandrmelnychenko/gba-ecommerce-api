namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class SupplyInformationDeliveryProtocol : EntityBase {
    public long UserId { get; set; }

    public long? SupplyOrderId { get; set; }

    public long? SupplyInvoiceId { get; set; }

    public long? SupplyProFormId { get; set; }

    public long SupplyInformationDeliveryProtocolKeyId { get; set; }

    public string Value { get; set; }

    public bool IsDefault { get; set; }

    public User User { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public SupplyProForm SupplyProForm { get; set; }

    public SupplyInformationDeliveryProtocolKey SupplyInformationDeliveryProtocolKey { get; set; }
}