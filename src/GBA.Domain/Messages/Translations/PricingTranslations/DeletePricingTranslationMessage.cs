using System;

namespace GBA.Domain.Messages.Translations.PricingTranslations;

public sealed class DeletePricingTranslationMessage {
    public DeletePricingTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}