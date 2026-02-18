using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement.UserProfileRoles;

public sealed class UpdateUserProfileRoleMessage {
    public UpdateUserProfileRoleMessage(UserRole userProfileRole) {
        UserProfileRole = userProfileRole;
    }

    public UserRole UserProfileRole { get; set; }
}