using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities;

public sealed class UserRole : EntityBase {
    public UserRole() {
        Users = new HashSet<User>();

        UserRoleTranslations = new HashSet<UserRoleTranslation>();

        DashboardNodeModules = new List<DashboardNodeModule>();

        UserRoleDashboardNodeModules = new HashSet<UserRoleDashboardNode>();

        RolePermissions = new HashSet<RolePermission>();

        Permissions = new HashSet<Permission>();

        DashboardNodes = new HashSet<DashboardNode>();
    }

    public string Name { get; set; }

    public string Dashboard { get; set; }

    public UserRoleType UserRoleType { get; set; }

    public List<DashboardNodeModule> DashboardNodeModules { get; set; }

    public ICollection<User> Users { get; set; }

    public ICollection<UserRoleTranslation> UserRoleTranslations { get; set; }

    public ICollection<UserRoleDashboardNode> UserRoleDashboardNodeModules { get; set; }

    public ICollection<DashboardNode> DashboardNodes { get; set; }

    public ICollection<Permission> Permissions { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; }
}