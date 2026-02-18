using System;

namespace GBA.Domain.Messages.Translations.PriceTypeTranslations;

public sealed class DeletePriceTypeTranslationMessage {
    public DeletePriceTypeTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}