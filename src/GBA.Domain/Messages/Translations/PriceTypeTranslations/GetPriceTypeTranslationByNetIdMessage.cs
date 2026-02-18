using System;

namespace GBA.Domain.Messages.Translations.PriceTypeTranslations;

public sealed class GetPriceTypeTranslationByNetIdMessage {
    public GetPriceTypeTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}