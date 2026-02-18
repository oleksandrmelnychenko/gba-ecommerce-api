namespace GBA.Domain.Messages.UserManagement;

public sealed class GetPermissionsByDashboardNodeIdMessage {
    public GetPermissionsByDashboardNodeIdMessage(long dashboardNodeId) {
        DashboardNodeId = dashboardNodeId;
    }

    public long DashboardNodeId { get; }
}