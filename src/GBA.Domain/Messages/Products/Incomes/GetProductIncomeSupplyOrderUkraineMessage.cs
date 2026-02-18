using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetProductIncomeSupplyOrderUkraineMessage {
    public GetProductIncomeSupplyOrderUkraineMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}