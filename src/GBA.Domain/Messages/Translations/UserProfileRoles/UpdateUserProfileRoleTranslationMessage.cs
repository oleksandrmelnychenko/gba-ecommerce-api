using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.UserProfileRoles;

public sealed class UpdateUserProfileRoleTranslationMessage {
    public UpdateUserProfileRoleTranslationMessage(UserRoleTranslation userProfileRoleTranslation) {
        UserProfileRoleTranslation = userProfileRoleTranslation;
    }

    public UserRoleTranslation UserProfileRoleTranslation { get; set; }
}