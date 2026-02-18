using System;

namespace GBA.Domain.Messages.Communications.Hubs;

public sealed class AddedSaleNotificationMessage {
    public AddedSaleNotificationMessage(Guid saleNetId) {
        SaleNetId = saleNetId;
    }

    public Guid SaleNetId { get; }
}