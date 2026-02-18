using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class CloseExpiredOrdersMessage {
    public CloseExpiredOrdersMessage(Sale sale, Guid userNetId) {
        Sale = sale;
        UserNetId = userNetId;
    }

    public Sale Sale { get; }
    public Guid UserNetId { get; }
}