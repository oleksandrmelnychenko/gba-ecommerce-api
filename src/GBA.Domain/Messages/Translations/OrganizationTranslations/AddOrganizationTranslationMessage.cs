using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.OrganizationTranslations;

public sealed class AddOrganizationTranslationMessage {
    public AddOrganizationTranslationMessage(OrganizationTranslation organizationTranslation) {
        OrganizationTranslation = organizationTranslation;
    }

    public OrganizationTranslation OrganizationTranslation { get; set; }
}