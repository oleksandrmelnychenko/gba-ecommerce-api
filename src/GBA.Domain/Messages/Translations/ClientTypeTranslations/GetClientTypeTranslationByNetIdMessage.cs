using System;

namespace GBA.Domain.Messages.Translations.ClientTypeTranslations;

public sealed class GetClientTypeTranslationByNetIdMessage {
    public GetClientTypeTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}