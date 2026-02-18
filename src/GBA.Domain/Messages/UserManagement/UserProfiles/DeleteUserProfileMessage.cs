using System;

namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class DeleteUserProfileMessage {
    public DeleteUserProfileMessage(Guid netId) {
        NetUid = netId;
    }

    public Guid NetUid { get; set; }
}