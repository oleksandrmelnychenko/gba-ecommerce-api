using System;

namespace GBA.Domain.Messages.Sales;

public sealed class ExportInvoiceForPaymentForSaleByNetIdForPrintingMessage {
    public ExportInvoiceForPaymentForSaleByNetIdForPrintingMessage(Guid saleNetId, Guid userNetId, string path) {
        SaleNetId = saleNetId;

        UserNetId = userNetId;

        Path = path;
    }

    public Guid SaleNetId { get; }

    public Guid UserNetId { get; }

    public string Path { get; }
}