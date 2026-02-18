using System;

namespace GBA.Domain.Messages.Translations.ClientTypeTranslations;

public sealed class DeleteClientTypeTranslationMessage {
    public DeleteClientTypeTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}