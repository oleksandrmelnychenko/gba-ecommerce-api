using System;

namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class GetUserProfileByNetIdMessage {
    public GetUserProfileByNetIdMessage(Guid netId) {
        NetUid = netId;
    }

    public Guid NetUid { get; set; }
}