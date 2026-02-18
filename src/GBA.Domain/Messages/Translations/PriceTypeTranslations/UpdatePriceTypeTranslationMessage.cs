using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PriceTypeTranslations;

public sealed class UpdatePriceTypeTranslationMessage {
    public UpdatePriceTypeTranslationMessage(PriceTypeTranslation priceTypeTranslation) {
        PriceTypeTranslation = priceTypeTranslation;
    }

    public PriceTypeTranslation PriceTypeTranslation { get; set; }
}