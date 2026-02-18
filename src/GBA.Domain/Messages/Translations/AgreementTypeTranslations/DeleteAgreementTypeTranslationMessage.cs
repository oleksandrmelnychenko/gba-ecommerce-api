using System;

namespace GBA.Domain.Messages.Translations.AgreementTypeTranslations;

public sealed class DeleteAgreementTypeTranslationMessage {
    public DeleteAgreementTypeTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}