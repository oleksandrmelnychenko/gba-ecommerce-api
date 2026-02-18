using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PriceTypeTranslations;

public sealed class AddPriceTypeTranslationMessage {
    public AddPriceTypeTranslationMessage(PriceTypeTranslation priceTypeTranslation) {
        PriceTypeTranslation = priceTypeTranslation;
    }

    public PriceTypeTranslation PriceTypeTranslation { get; set; }
}