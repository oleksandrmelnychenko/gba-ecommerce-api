using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientsWithDebtByManagerNetIdMessage {
    public GetAllClientsWithDebtByManagerNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}