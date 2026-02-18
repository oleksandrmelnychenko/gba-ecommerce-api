using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class MoveProductPlacementFromSaleReturnMessage {
    public MoveProductPlacementFromSaleReturnMessage(SaleReturnItem saleReturnItem) {
        SaleReturnItem = saleReturnItem;
    }

    public SaleReturnItem SaleReturnItem { get; }
}