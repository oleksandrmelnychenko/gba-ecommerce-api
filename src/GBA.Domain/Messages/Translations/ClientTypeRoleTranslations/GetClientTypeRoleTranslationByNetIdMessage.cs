using System;

namespace GBA.Domain.Messages.Translations.ClientTypeRoleTranslations;

public sealed class GetClientTypeRoleTranslationByNetIdMessage {
    public GetClientTypeRoleTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}