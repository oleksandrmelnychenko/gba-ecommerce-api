using System;
using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class AddAgreementMessage {
    public AddAgreementMessage(Agreement agreement, Guid netId) {
        Agreement = agreement;

        NetId = netId;
    }

    public Agreement Agreement { get; set; }

    public Guid NetId { get; set; }
}