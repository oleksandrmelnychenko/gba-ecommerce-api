using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleInvoicePzDocumentBySaleNetIdMessage {
    public GetSaleInvoicePzDocumentBySaleNetIdMessage(string saleInvoicesFolderPath, Guid netId) {
        SaleInvoicesFolderPath = saleInvoicesFolderPath;

        NetId = netId;
    }

    public string SaleInvoicesFolderPath { get; }

    public Guid NetId { get; }
}