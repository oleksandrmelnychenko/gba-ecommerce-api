using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.TransporterTypeTranslations;

public sealed class AddTransporterTypeTranslationMessage {
    public AddTransporterTypeTranslationMessage(TransporterTypeTranslation transporterTypeTranslation) {
        TransporterTypeTranslation = transporterTypeTranslation;
    }

    public TransporterTypeTranslation TransporterTypeTranslation { get; set; }
}