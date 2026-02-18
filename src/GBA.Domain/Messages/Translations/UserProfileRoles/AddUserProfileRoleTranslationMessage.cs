using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.UserProfileRoles;

public sealed class AddUserProfileRoleTranslationMessage {
    public AddUserProfileRoleTranslationMessage(UserRoleTranslation userProfileRoleTranslation) {
        UserProfileRoleTranslation = userProfileRoleTranslation;
    }

    public UserRoleTranslation UserProfileRoleTranslation { get; set; }
}