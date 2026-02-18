using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Messages.Dashboards;

public sealed class AddDashboardNodeModuleMessage {
    public AddDashboardNodeModuleMessage(DashboardNodeModule dashboardNodeModule) {
        DashboardNodeModule = dashboardNodeModule;
    }

    public DashboardNodeModule DashboardNodeModule { get; set; }
}