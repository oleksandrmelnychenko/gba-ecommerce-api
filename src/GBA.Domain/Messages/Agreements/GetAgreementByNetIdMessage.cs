using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetAgreementByNetIdMessage {
    public GetAgreementByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}