using System;

namespace GBA.Domain.Messages.Transporters;

public sealed class GetTransporterByNetIdMessage {
    public GetTransporterByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}