using System;

namespace GBA.Domain.Messages.Translations.ClientTypeRoleTranslations;

public sealed class DeleteClientTypeRoleTranslationMessage {
    public DeleteClientTypeRoleTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}