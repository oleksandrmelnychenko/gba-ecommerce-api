using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetAgreementTypeByNetIdMessage {
    public GetAgreementTypeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}