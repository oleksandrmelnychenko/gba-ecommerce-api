using System;

namespace GBA.Domain.Messages.Dashboards;

public sealed class GetAllDashboardNodeModulesByUserRoleMessage {
    public GetAllDashboardNodeModulesByUserRoleMessage(Guid userNetId) {
        UserNetId = userNetId;
    }

    public Guid UserNetId { get; }
}