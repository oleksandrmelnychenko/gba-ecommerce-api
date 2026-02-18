using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.ActReconciliations;

public sealed class GetActReconciliationByNetIdMessage {
    public GetActReconciliationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}