using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.ClientTypeRoleTranslations;

public sealed class AddClientTypeRoleTranslationMessage {
    public AddClientTypeRoleTranslationMessage(ClientTypeRoleTranslation clientTypeRoleTranslation) {
        ClientTypeRoleTranslation = clientTypeRoleTranslation;
    }

    public ClientTypeRoleTranslation ClientTypeRoleTranslation { get; set; }
}