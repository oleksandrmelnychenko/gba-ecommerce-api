using System;

namespace GBA.Domain.Messages.Translations.PricingTranslations;

public sealed class GetPricingTranslationByNetIdMessage {
    public GetPricingTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}