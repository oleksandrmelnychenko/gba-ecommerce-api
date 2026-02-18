using System;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class GetSupplyOrganizationByNetIdMessage {
    public GetSupplyOrganizationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}