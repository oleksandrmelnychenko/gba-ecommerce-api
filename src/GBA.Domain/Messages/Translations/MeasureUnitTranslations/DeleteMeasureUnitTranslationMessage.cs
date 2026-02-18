using System;

namespace GBA.Domain.Messages.Translations.MeasureUnitTranslations;

public class DeleteMeasureUnitTranslationMessage {
    public DeleteMeasureUnitTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}