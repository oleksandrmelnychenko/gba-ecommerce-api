using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class GetSupplyOrderProductIncomeByNetIdMessage {
    public GetSupplyOrderProductIncomeByNetIdMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}