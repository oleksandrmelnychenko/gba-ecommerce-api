using System;

namespace GBA.Domain.Messages.Clients.OrganizationClients;

public sealed class GetOrganizationClientByNetIdMessage {
    public GetOrganizationClientByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}