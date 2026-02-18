using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetAllAgreementsByClientNetIdMessage {
    public GetAllAgreementsByClientNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}