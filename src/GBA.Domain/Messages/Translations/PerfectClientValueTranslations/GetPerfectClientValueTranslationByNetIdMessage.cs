using System;

namespace GBA.Domain.Messages.Translations.PerfectClientValueTranslations;

public sealed class GetPerfectClientValueTranslationByNetIdMessage {
    public GetPerfectClientValueTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}