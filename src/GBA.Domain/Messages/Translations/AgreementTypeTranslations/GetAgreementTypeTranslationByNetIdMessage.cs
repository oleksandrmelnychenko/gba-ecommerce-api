using System;

namespace GBA.Domain.Messages.Translations.AgreementTypeTranslations;

public sealed class GetAgreementTypeTranslationByNetIdMessage {
    public GetAgreementTypeTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}