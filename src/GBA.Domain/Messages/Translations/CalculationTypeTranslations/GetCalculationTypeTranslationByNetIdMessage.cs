using System;

namespace GBA.Domain.Messages.Translations.CalculationTypeTranslations;

public sealed class GetCalculationTypeTranslationByNetIdMessage {
    public GetCalculationTypeTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}