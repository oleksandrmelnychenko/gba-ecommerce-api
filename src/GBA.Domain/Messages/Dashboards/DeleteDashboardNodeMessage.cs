using System;

namespace GBA.Domain.Messages.Dashboards;

public sealed class DeleteDashboardNodeMessage {
    public DeleteDashboardNodeMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}