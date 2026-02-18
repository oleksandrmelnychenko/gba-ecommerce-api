using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PricingTranslations;

public sealed class AddPricingTranslationMessage {
    public AddPricingTranslationMessage(PricingTranslation pricingTranslation) {
        PricingTranslation = pricingTranslation;
    }

    public PricingTranslation PricingTranslation { get; set; }
}