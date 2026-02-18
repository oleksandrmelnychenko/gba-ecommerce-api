using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetTotalDebtsByClientStructureWithRootClientMessage {
    public GetTotalDebtsByClientStructureWithRootClientMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}