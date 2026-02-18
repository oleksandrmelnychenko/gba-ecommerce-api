using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement.UserProfileRoles;

public sealed class AddUserProfileRoleMessage {
    public AddUserProfileRoleMessage(UserRole userProfileRole) {
        UserProfileRole = userProfileRole;
    }

    public UserRole UserProfileRole { get; set; }
}