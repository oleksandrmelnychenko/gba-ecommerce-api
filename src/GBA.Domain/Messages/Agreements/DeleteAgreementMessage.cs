using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class DeleteAgreementMessage {
    public DeleteAgreementMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}