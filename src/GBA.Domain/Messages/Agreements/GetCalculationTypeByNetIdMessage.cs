using System;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetCalculationTypeByNetIdMessage {
    public GetCalculationTypeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}