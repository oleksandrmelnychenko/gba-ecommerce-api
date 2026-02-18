using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Carriers;

public sealed class GetStathamByNetIdMessage {
    public GetStathamByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}