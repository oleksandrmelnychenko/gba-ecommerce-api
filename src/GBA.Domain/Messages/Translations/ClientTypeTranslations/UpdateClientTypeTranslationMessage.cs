using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.ClientTypeTranslations;

public sealed class UpdateClientTypeTranslationMessage {
    public UpdateClientTypeTranslationMessage(ClientTypeTranslation clientTypeTranslation) {
        ClientTypeTranslation = clientTypeTranslation;
    }

    public ClientTypeTranslation ClientTypeTranslation { get; set; }
}