using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetAllProductIncomesByProductNetIdMessage {
    public GetAllProductIncomesByProductNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}