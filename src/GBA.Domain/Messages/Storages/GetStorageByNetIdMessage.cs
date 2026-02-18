using System;

namespace GBA.Domain.Messages.Storages;

public sealed class GetStorageByNetIdMessage {
    public GetStorageByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}