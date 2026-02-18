namespace GBA.Domain.Messages.SearchStrategy;

public sealed class GetAllByQueryDocumentMessage {
    public GetAllByQueryDocumentMessage(string value, string saleInvoicesFolderPath) {
        Value = value;
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public string Value { get; }
    public string SaleInvoicesFolderPath { get; }
}