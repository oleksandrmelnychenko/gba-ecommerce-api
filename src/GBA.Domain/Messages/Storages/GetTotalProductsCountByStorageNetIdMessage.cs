using System;

namespace GBA.Domain.Messages.Storages;

public sealed class GetTotalProductsCountByStorageNetIdMessage {
    public GetTotalProductsCountByStorageNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}