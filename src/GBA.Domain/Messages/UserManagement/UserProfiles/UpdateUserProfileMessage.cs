using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class UpdateUserProfileMessage {
    public UpdateUserProfileMessage(User userProfile) {
        UserProfile = userProfile;
    }

    public User UserProfile { get; set; }
}