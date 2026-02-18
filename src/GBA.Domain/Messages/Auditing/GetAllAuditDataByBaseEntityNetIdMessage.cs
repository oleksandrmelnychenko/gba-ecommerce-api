using System;

namespace GBA.Domain.Messages.Auditing;

public sealed class GetAllAuditDataByBaseEntityNetIdMessage {
    public GetAllAuditDataByBaseEntityNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}