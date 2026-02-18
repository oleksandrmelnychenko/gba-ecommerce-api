using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Messages.Dashboards;

public sealed class UpdateDashboardNodeModuleMessage {
    public UpdateDashboardNodeModuleMessage(DashboardNodeModule dashboardNodeModule) {
        DashboardNodeModule = dashboardNodeModule;
    }

    public DashboardNodeModule DashboardNodeModule { get; set; }
}