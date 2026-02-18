using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.CalculationTypeTranslations;

public sealed class AddCalculationTypeTranslationMessage {
    public AddCalculationTypeTranslationMessage(CalculationTypeTranslation calculationTypeTranslation) {
        CalculationTypeTranslation = calculationTypeTranslation;
    }

    public CalculationTypeTranslation CalculationTypeTranslation { get; set; }
}