using System;

namespace GBA.Domain.Messages.UserManagement.UserProfileRoles;

public sealed class GetUserProfileRoleByNetIdMessage {
    public GetUserProfileRoleByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}