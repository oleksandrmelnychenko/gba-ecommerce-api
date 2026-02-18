using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetGroupedDebtsByClientNetIdMessage {
    public GetGroupedDebtsByClientNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}