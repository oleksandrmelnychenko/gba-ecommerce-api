using System;

namespace GBA.Domain.Messages.Consignments;

public sealed class StoreConsignmentMovementFromNewOrderItemMessage {
    public StoreConsignmentMovementFromNewOrderItemMessage(long orderItemId, long saleId, object responseActorRef, object originalSender, Guid userNetId) {
        OrderItemId = orderItemId;

        SaleId = saleId;

        ResponseActorRef = responseActorRef;

        OriginalSender = originalSender;

        UserNetId = userNetId;
    }

    public long OrderItemId { get; }

    public long SaleId { get; }

    public object ResponseActorRef { get; }

    public object OriginalSender { get; }

    public Guid UserNetId { get; }
}