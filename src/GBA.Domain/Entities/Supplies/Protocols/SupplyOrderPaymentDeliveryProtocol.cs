using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class SupplyOrderPaymentDeliveryProtocol : EntityBase {
    public SupplyOrderPaymentDeliveryProtocol() {
        PaymentDeliveryDocuments = new HashSet<PaymentDeliveryDocument>();
    }

    public long UserId { get; set; }

    public long? SupplyPaymentTaskId { get; set; }

    public long? SupplyInvoiceId { get; set; }

    public long? SupplyProFormId { get; set; }

    public long SupplyOrderPaymentDeliveryProtocolKeyId { get; set; }

    public decimal Value { get; set; }

    public double Discount { get; set; }

    public bool IsAccounting { get; set; }

    public SupplyOrderPaymentDeliveryProtocolKey SupplyOrderPaymentDeliveryProtocolKey { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public SupplyProForm SupplyProForm { get; set; }

    public User User { get; set; }

    public ICollection<PaymentDeliveryDocument> PaymentDeliveryDocuments { get; set; }
}