using System;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class DeleteSupplyOrganizationByNetIdMessage {
    public DeleteSupplyOrganizationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}