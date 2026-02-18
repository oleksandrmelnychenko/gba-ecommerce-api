using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetBySupplyOrderNetIdMessage {
    public GetBySupplyOrderNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}