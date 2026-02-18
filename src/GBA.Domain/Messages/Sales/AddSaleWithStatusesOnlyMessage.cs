using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class AddSaleWithStatusesOnlyMessage {
    public AddSaleWithStatusesOnlyMessage(Sale sale, Guid userNetId) {
        Sale = sale;

        UserNetId = userNetId;
    }

    public AddSaleWithStatusesOnlyMessage(Sale sale, Guid userNetId, object originalMessage) {
        Sale = sale;

        UserNetId = userNetId;
        OriginalMessage = originalMessage;
    }

    public Sale Sale { get; set; }

    public Guid UserNetId { get; set; }

    public object OriginalMessage { get; }
}