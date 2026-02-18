namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class GetCreateDocumentProductPlacementStorageMessage {
    public GetCreateDocumentProductPlacementStorageMessage(
        string saleInvoicesFolderPath
    ) {
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public string SaleInvoicesFolderPath { get; }
}