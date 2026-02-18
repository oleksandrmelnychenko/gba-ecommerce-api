using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyDeliveryDocument : EntityBase {
    public SupplyDeliveryDocument() {
        SupplyOrderDeliveryDocuments = new HashSet<SupplyOrderDeliveryDocument>();

        SupplyInvoiceDeliveryDocuments = new HashSet<SupplyInvoiceDeliveryDocument>();
    }

    public string Name { get; set; }

    public SupplyTransportationType TransportationType { get; set; }

    public ICollection<SupplyOrderDeliveryDocument> SupplyOrderDeliveryDocuments { get; set; }

    public ICollection<SupplyInvoiceDeliveryDocument> SupplyInvoiceDeliveryDocuments { get; set; }
}