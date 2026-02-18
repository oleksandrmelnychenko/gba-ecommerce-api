using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetAllBySupplyOrderUkraineNetIdMessage {
    public GetAllBySupplyOrderUkraineNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}