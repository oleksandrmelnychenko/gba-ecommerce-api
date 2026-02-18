using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetProductIncomeByNetIdMessage {
    public GetProductIncomeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}