using System;

namespace GBA.Domain.Messages.Translations.PerfectClientTranslations;

public sealed class DeletePerfectClientTranslationMessage {
    public DeletePerfectClientTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}