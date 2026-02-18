using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetAllAgreementsByRetailClientMessage {
    public GetAllAgreementsByRetailClientMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}