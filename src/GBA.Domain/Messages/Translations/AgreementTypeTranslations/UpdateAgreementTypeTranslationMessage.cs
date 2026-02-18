using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.AgreementTypeTranslations;

public sealed class UpdateAgreementTypeTranslationMessage {
    public UpdateAgreementTypeTranslationMessage(AgreementTypeTranslation agreementTypeTranslation) {
        AgreementTypeTranslation = agreementTypeTranslation;
    }

    public AgreementTypeTranslation AgreementTypeTranslation { get; set; }
}