using System;

namespace GBA.Domain.Messages.Organizations;

public sealed class GetOrganizationByNetIdMessage {
    public GetOrganizationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}