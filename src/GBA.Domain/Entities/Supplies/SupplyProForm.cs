using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyProForm : EntityBase {
    public SupplyProForm() {
        ProFormDocuments = new HashSet<ProFormDocument>();

        SupplyOrders = new HashSet<SupplyOrder>();

        PaymentDeliveryProtocols = new HashSet<SupplyOrderPaymentDeliveryProtocol>();

        InformationDeliveryProtocols = new HashSet<SupplyInformationDeliveryProtocol>();
    }

    public decimal NetPrice { get; set; }

    public string Number { get; set; }

    public string ServiceNumber { get; set; }

    public DateTime? DateFrom { get; set; }

    public bool IsSkipped { get; set; }

    public ICollection<ProFormDocument> ProFormDocuments { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<SupplyOrderPaymentDeliveryProtocol> PaymentDeliveryProtocols { get; set; }

    public ICollection<SupplyInformationDeliveryProtocol> InformationDeliveryProtocols { get; set; }
}