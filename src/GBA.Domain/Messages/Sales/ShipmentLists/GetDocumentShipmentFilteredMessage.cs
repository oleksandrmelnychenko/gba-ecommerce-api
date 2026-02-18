using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class GetDocumentShipmentFilteredMessage {
    public GetDocumentShipmentFilteredMessage(Guid netId, string saleInvoicesFolderPath) {
        NetId = netId;
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public Guid NetId { get; set; }

    public string SaleInvoicesFolderPath { get; set; }
}