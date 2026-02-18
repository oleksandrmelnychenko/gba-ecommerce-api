using System;

namespace GBA.Domain.Messages.Clients;

public sealed class IsSubClientsHasAgreementsMessage {
    public IsSubClientsHasAgreementsMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}