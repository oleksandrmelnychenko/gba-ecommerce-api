using System;

namespace GBA.Domain.Messages.Communications.Hubs;

public sealed class UpdatedSaleNotificationMessage {
    public UpdatedSaleNotificationMessage(Guid saleNetId) {
        SaleNetId = saleNetId;
    }

    public Guid SaleNetId { get; }
}