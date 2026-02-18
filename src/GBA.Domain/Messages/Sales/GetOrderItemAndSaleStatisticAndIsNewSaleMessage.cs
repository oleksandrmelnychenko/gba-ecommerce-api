using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class GetOrderItemAndSaleStatisticAndIsNewSaleMessage {
    public GetOrderItemAndSaleStatisticAndIsNewSaleMessage(OrderItem orderItem, Guid saleNetId, bool isNewSale, string errorMessage = "") {
        OrderItem = orderItem;

        SaleNetId = saleNetId;

        IsNewSale = isNewSale;

        ErrorMessage = errorMessage;
    }

    public OrderItem OrderItem { get; private set; }

    public Guid SaleNetId { get; private set; }

    public bool IsNewSale { get; set; }

    public string ErrorMessage { get; set; }
}