using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetIncomeInfoByProductNetIdMessage {
    public GetIncomeInfoByProductNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}