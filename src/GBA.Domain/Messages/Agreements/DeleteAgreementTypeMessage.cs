using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class DeleteAgreementTypeMessage {
    public DeleteAgreementTypeMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}