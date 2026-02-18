using System;

namespace GBA.Domain.Messages.Translations.OrganizationTranslations;

public sealed class DeleteOrganizationTranslationMessage {
    public DeleteOrganizationTranslationMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}