using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetOrderItemAndSaleStatisticsMessage {
    public GetOrderItemAndSaleStatisticsMessage(Guid saleFrom, Guid saleTo, long orderItemId, bool isNewToSale) {
        SaleFrom = saleFrom;

        SaleTo = saleTo;

        OrderItemId = orderItemId;

        IsNewToSale = isNewToSale;
    }

    public Guid SaleFrom { get; }

    public Guid SaleTo { get; }

    public long OrderItemId { get; }

    public bool IsNewToSale { get; }
}