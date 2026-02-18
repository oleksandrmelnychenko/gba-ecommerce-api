using System.Collections.Generic;
using GBA.Domain.Entities.Users;

namespace GBA.Domain.Messages.UserManagement;

public sealed class OldUsersMessage {
    public OldUsersMessage(
        List<OldUserShop> userProfile
    ) {
        OldUsers = userProfile;
    }

    public List<OldUserShop> OldUsers { get; set; }
}