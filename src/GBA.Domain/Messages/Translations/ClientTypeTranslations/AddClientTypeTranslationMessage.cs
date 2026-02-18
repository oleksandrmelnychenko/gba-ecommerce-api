using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.ClientTypeTranslations;

public sealed class AddClientTypeTranslationMessage {
    public AddClientTypeTranslationMessage(ClientTypeTranslation clientTypeTranslation) {
        ClientTypeTranslation = clientTypeTranslation;
    }

    public ClientTypeTranslation ClientTypeTranslation { get; set; }
}