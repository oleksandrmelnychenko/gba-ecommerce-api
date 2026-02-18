using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetTotalDebtsByClientStructureMessage {
    public GetTotalDebtsByClientStructureMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}