using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleInvoiceDocumentBySaleHistoryNetIdMessage {
    public GetSaleInvoiceDocumentBySaleHistoryNetIdMessage(
        Guid netId,
        string saleInvoicesFolderPath,
        bool isFromStorages) {
        NetId = netId;

        SaleInvoicesFolderPath = saleInvoicesFolderPath;

        IsFromStorages = isFromStorages;
    }

    public Guid NetId { get; set; }

    public string SaleInvoicesFolderPath { get; set; }

    public bool IsFromStorages { get; }
}