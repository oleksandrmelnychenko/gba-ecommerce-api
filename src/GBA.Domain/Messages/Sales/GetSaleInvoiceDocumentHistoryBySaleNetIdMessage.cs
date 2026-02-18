using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleInvoiceDocumentHistoryBySaleNetIdMessage {
    public GetSaleInvoiceDocumentHistoryBySaleNetIdMessage(
        Guid netId,
        string saleInvoicesFolderPath,
        bool isFromStorages,
        Guid historyNetId) {
        NetId = netId;

        SaleInvoicesFolderPath = saleInvoicesFolderPath;

        IsFromStorages = isFromStorages;
        HistoryNetId = historyNetId;
    }

    public Guid NetId { get; set; }
    public Guid HistoryNetId { get; set; }

    public string SaleInvoicesFolderPath { get; set; }

    public bool IsFromStorages { get; }
}