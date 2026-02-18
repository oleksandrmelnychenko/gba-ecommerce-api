using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class GetCreateDocumentProductPlacementStoragesMessage {
    public GetCreateDocumentProductPlacementStoragesMessage(
        List<ProductPlacementStorage> productPlacementStorages,
        string saleInvoicesFolderPath
    ) {
        ProductPlacementStorages = productPlacementStorages;
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public List<ProductPlacementStorage> ProductPlacementStorages { get; }
    public string SaleInvoicesFolderPath { get; }
}