using System.Collections.Generic;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities.Dashboards;

public sealed class DashboardNode : EntityBase {
    public DashboardNode() {
        Children = new HashSet<DashboardNode>();
        UserRoleDashboardNodes = new HashSet<UserRoleDashboardNode>();
        UserRoles = new HashSet<UserRole>();
        Permissions = new HashSet<Permission>();
    }

    public string Language { get; set; }

    public string Module { get; set; }

    public string Route { get; set; }

    public string CssClass { get; set; }

    public long? ParentDashboardNodeId { get; set; }

    public long? DashboardNodeModuleId { get; set; }

    public DashboardNodeType DashboardNodeType { get; set; }

    public DashboardNode ParentDashboardNode { get; set; }

    public DashboardNodeModule DashboardNodeModule { get; set; }

    public ICollection<DashboardNode> Children { get; set; }

    public ICollection<UserRoleDashboardNode> UserRoleDashboardNodes { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }

    public ICollection<Permission> Permissions { get; set; }
}