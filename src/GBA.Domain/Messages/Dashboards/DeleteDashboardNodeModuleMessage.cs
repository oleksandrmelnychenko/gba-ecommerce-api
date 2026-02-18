using System;

namespace GBA.Domain.Messages.Dashboards;

public sealed class DeleteDashboardNodeModuleMessage {
    public DeleteDashboardNodeModuleMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}