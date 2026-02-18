using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetExportRegisterInvoiceMessage {
    public GetExportRegisterInvoiceMessage(
        DateTime from,
        DateTime to,
        string value,
        string saleInvoicesFolderPath) {
        From = from;
        To = to;
        Value = value;
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Value { get; set; }
    public string SaleInvoicesFolderPath { get; }
}