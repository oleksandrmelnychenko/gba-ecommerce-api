using System;

namespace GBA.Domain.Messages.Translations.TransporterTypeTranslations;

public sealed class GetTransporterTypeTranslationByNetIdMessage {
    public GetTransporterTypeTranslationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}