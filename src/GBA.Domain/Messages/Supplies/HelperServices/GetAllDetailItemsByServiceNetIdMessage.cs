using System;

namespace GBA.Domain.Messages.Supplies.HelperServices;

public sealed class GetAllDetailItemsByServiceNetIdMessage {
    public GetAllDetailItemsByServiceNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}