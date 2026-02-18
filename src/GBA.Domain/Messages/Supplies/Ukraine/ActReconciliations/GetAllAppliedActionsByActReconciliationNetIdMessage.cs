using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.ActReconciliations;

public sealed class GetAllAppliedActionsByActReconciliationNetIdMessage {
    public GetAllAppliedActionsByActReconciliationNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}