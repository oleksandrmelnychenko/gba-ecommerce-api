using System;

namespace GBA.Domain.Messages.Translations.MeasureUnitTranslations;

public sealed class GetMeasureUnitTranslationByNetIdMessage {
    public GetMeasureUnitTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}