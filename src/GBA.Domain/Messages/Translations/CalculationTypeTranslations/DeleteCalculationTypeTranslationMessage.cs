using System;

namespace GBA.Domain.Messages.Translations.CalculationTypeTranslations;

public sealed class DeleteCalculationTypeTranslationMessage {
    public DeleteCalculationTypeTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}