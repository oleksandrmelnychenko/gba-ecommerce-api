using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.UserRoles.Contracts;

public interface IPermissionsRepository {
    long Add(Permission permission);

    long AddRolePermission(RolePermission rolePermission);

    void Update(Permission permission);

    void RemoveRolePermissionByUserRoleId(long id);

    Permission GetById(long id);

    IEnumerable<Permission> GetPermissionsByDashboardNodeId(long id);

    IEnumerable<Permission> GetPermissionsByUserRoleId(long id);
}