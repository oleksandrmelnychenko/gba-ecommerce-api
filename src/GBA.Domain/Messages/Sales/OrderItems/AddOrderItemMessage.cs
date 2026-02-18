using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class AddOrderItemMessage {
    public AddOrderItemMessage(OrderItem orderItem, Guid clientAgreementNetId, Guid saleNetId, Guid userNetId, string saleNumber = null) {
        OrderItem = orderItem;

        ClientAgreementNetId = clientAgreementNetId;

        SaleNetId = saleNetId;

        UserNetId = userNetId;

        SaleNumber = saleNumber;
    }

    public OrderItem OrderItem { get; set; }

    public Guid ClientAgreementNetId { get; set; }

    public Guid SaleNetId { get; set; }

    public Guid UserNetId { get; set; }

    public string SaleNumber { get; }

    public Sale Sale { get; set; }
}