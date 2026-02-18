using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Entities;

public sealed class UserRoleDashboardNode : EntityBase {
    public long UserRoleId { get; set; }

    public long DashboardNodeId { get; set; }

    public UserRole UserRole { get; set; }

    public DashboardNode DashboardNode { get; set; }
}