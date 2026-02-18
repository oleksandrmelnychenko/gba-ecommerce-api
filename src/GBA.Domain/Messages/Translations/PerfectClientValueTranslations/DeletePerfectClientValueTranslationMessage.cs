using System;

namespace GBA.Domain.Messages.Translations.PerfectClientValueTranslations;

public sealed class DeletePerfectClientValueTranslationMessage {
    public DeletePerfectClientValueTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}