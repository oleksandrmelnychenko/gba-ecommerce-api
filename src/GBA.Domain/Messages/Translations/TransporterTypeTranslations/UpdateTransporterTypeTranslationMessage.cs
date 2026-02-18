using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.TransporterTypeTranslations;

public sealed class UpdateTransporterTypeTranslationMessage {
    public UpdateTransporterTypeTranslationMessage(TransporterTypeTranslation transporterTypeTranslation) {
        TransporterTypeTranslation = transporterTypeTranslation;
    }

    public TransporterTypeTranslation TransporterTypeTranslation { get; set; }
}