using System;

namespace GBA.Domain.Messages.Translations.PerfectClientTranslations;

public sealed class GetPerfectClientTranslationByNetIdMessage {
    public GetPerfectClientTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}