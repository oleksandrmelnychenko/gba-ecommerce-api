using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.AgreementTypeTranslations;

public sealed class AddAgreementTypeTranslationMessage {
    public AddAgreementTypeTranslationMessage(AgreementTypeTranslation agreementTypeTranslation) {
        AgreementTypeTranslation = agreementTypeTranslation;
    }

    public AgreementTypeTranslation AgreementTypeTranslation { get; set; }
}