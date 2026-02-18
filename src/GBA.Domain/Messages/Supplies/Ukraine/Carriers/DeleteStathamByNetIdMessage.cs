using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Carriers;

public sealed class DeleteStathamByNetIdMessage {
    public DeleteStathamByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}