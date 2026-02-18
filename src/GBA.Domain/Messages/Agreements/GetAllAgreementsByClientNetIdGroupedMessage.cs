using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetAllAgreementsByClientNetIdGroupedMessage {
    public GetAllAgreementsByClientNetIdGroupedMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}