using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Messages.Dashboards;

public sealed class UpdateDashboardNodeMessage {
    public UpdateDashboardNodeMessage(DashboardNode dashboardNode) {
        DashboardNode = dashboardNode;
    }

    public DashboardNode DashboardNode { get; set; }
}