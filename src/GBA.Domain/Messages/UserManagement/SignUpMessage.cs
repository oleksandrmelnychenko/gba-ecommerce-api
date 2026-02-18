using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement;

public sealed class SignUpMessage {
    public SignUpMessage(
        User userProfile,
        string password) {
        UserProfile = userProfile;

        Password = password;
    }

    public User UserProfile { get; set; }

    public string Password { get; set; }
}