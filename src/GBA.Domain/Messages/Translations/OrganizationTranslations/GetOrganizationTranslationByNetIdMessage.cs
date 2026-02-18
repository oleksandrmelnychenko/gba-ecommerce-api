using System;

namespace GBA.Domain.Messages.Translations.OrganizationTranslations;

public sealed class GetOrganizationTranslationByNetIdMessage {
    public GetOrganizationTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}