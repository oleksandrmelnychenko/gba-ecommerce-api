using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class SupplyOrderPolandPaymentDeliveryProtocol : EntityBase {
    public SupplyOrderPolandPaymentDeliveryProtocol() {
        InvoiceDocuments = new HashSet<InvoiceDocument>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();
    }

    public long UserId { get; set; }

    public long SupplyPaymentTaskId { get; set; }

    public long SupplyOrderId { get; set; }

    public long SupplyOrderPaymentDeliveryProtocolKeyId { get; set; }

    public string Name { get; set; }

    public string Number { get; set; }

    public string ServiceNumber { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal NetPrice { get; set; }

    public decimal Vat { get; set; }

    public double VatPercent { get; set; }

    public bool IsAccounting { get; set; }

    public DateTime FromDate { get; set; }

    public User User { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public SupplyOrderPaymentDeliveryProtocolKey SupplyOrderPaymentDeliveryProtocolKey { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }
}