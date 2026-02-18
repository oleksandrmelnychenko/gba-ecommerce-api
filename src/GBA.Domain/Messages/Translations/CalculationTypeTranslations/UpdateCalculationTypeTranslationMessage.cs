using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.CalculationTypeTranslations;

public sealed class UpdateCalculationTypeTranslationMessage {
    public UpdateCalculationTypeTranslationMessage(CalculationTypeTranslation calculationTypeTranslation) {
        CalculationTypeTranslation = calculationTypeTranslation;
    }

    public CalculationTypeTranslation CalculationTypeTranslation { get; set; }
}