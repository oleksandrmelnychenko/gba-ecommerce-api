using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleByNetIdWithShiftedItemsDocumentMessage {
    public GetSaleByNetIdWithShiftedItemsDocumentMessage(
        Guid netid,
        string saleInvoicesFolderPath
    ) {
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
        NetId = netid;
    }

    public Guid NetId { get; set; }
    public string SaleInvoicesFolderPath { get; }
}