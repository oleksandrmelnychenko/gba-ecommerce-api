using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.MeasureUnitTranslations;

public sealed class AddMeasureUnitTranslationMessage {
    public AddMeasureUnitTranslationMessage(MeasureUnitTranslation measureUnitTranslation) {
        MeasureUnitTranslation = measureUnitTranslation;
    }

    public MeasureUnitTranslation MeasureUnitTranslation { get; set; }
}