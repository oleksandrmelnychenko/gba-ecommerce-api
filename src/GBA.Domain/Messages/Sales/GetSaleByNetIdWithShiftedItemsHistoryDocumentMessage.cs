using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleByNetIdWithShiftedItemsHistoryDocumentMessage {
    public GetSaleByNetIdWithShiftedItemsHistoryDocumentMessage(
        Guid guid,
        Guid historyNetId,
        string saleInvoicesFolderPath
    ) {
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
        NetId = guid;
        HistoryNetId = historyNetId;
    }

    public Guid NetId { get; set; }
    public Guid HistoryNetId { get; set; }

    public string SaleInvoicesFolderPath { get; }
}