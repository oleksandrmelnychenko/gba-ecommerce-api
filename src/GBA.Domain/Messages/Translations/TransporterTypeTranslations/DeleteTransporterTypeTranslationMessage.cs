using System;

namespace GBA.Domain.Messages.Translations.TransporterTypeTranslations;

public sealed class DeleteTransporterTypeTranslationMessage {
    public DeleteTransporterTypeTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}