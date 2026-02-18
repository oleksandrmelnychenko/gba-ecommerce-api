using System;

namespace GBA.Domain.Messages.Clients.OrganizationClients;

public sealed class RemoveOrganizationClientByNetIdMessage {
    public RemoveOrganizationClientByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}