using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.MeasureUnitTranslations;

public sealed class UpdateMeasureUnitTranslationMessage {
    public UpdateMeasureUnitTranslationMessage(MeasureUnitTranslation measureUnitTranslation) {
        MeasureUnitTranslation = measureUnitTranslation;
    }

    public MeasureUnitTranslation MeasureUnitTranslation { get; set; }
}