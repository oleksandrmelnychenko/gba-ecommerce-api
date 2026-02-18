using System;

namespace GBA.Domain.Messages.Organizations;

public sealed class DeleteOrganizationMessage {
    public DeleteOrganizationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}