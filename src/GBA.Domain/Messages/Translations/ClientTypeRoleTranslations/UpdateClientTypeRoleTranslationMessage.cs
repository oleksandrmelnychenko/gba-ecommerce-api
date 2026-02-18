using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.ClientTypeRoleTranslations;

public sealed class UpdateClientTypeRoleTranslationMessage {
    public UpdateClientTypeRoleTranslationMessage(ClientTypeRoleTranslation clientTypeRoleTranslation) {
        ClientTypeRoleTranslation = clientTypeRoleTranslation;
    }

    public ClientTypeRoleTranslation ClientTypeRoleTranslation { get; set; }
}