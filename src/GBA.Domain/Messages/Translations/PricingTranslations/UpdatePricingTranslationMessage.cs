using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PricingTranslations;

public sealed class UpdatePricingTranslationMessage {
    public UpdatePricingTranslationMessage(PricingTranslation pricingTranslation) {
        PricingTranslation = pricingTranslation;
    }

    public PricingTranslation PricingTranslation { get; set; }
}