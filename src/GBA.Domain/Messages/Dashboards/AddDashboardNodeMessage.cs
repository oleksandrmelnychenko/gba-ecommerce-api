using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Messages.Dashboards;

public sealed class AddDashboardNodeMessage {
    public AddDashboardNodeMessage(DashboardNode dashboardNode) {
        DashboardNode = dashboardNode;
    }

    public DashboardNode DashboardNode { get; set; }
}