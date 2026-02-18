using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.UserRoles.Contracts;

public interface IUserRoleDashboardNodeRepository {
    long Add(UserRoleDashboardNode userRoleDashboardNode);

    void Update(UserRoleDashboardNode userRoleDashboardNode);

    void RemoveByUserRoleId(long id);
}