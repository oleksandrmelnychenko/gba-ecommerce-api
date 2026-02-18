using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetOrderItemAndSaleStatisticMessage {
    public GetOrderItemAndSaleStatisticMessage(long orderItemId, Guid saleNetId) {
        OrderItemId = orderItemId;

        SaleNetId = saleNetId;
    }

    public long OrderItemId { get; private set; }

    public Guid SaleNetId { get; private set; }
}