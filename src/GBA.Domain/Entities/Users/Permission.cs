using System.Collections.Generic;
using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Entities;

public sealed class Permission : EntityBase {
    public Permission() {
        UserRoles = new HashSet<UserRole>();
        RolePermissions = new HashSet<RolePermission>();
    }

    public string ControlId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public long DashboardNodeId { get; set; }

    public DashboardNode DashboardNode { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; }
}