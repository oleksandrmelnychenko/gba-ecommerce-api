using System;

namespace GBA.Domain.Messages.Translations.UserProfileRoles;

public sealed class DeleteUserProfileRoleTranslationMessage {
    public DeleteUserProfileRoleTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}