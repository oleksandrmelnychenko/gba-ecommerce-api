using System;

namespace GBA.Domain.Messages.UserManagement.UserProfileRoles;

public sealed class DeleteUserProfileRoleMessage {
    public DeleteUserProfileRoleMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}