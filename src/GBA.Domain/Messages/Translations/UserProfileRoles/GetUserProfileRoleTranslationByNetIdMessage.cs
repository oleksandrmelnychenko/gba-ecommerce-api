using System;

namespace GBA.Domain.Messages.Translations.UserProfileRoles;

public sealed class GetUserProfileRoleTranslationByNetIdMessage {
    public GetUserProfileRoleTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}