using System.Data;
using GBA.Domain.Repositories.UserRoles.Contracts;

namespace GBA.Domain.Repositories.UserRoles;

public sealed class UserRoleRepositoriesFactory : IUserRoleRepositoriesFactory {
    public IUserRoleRepository NewUserRoleRepository(IDbConnection connection) {
        return new UserRoleRepository(connection);
    }

    public IUserRoleTranslationRepository NewUserRoleTranslationRepository(IDbConnection connection) {
        return new UserRoleTranslationRepository(connection);
    }

    public IUserRoleDashboardNodeRepository NewUserRoleDashboardNodeRepository(IDbConnection connection) {
        return new UserRoleDashboardNodeRepository(connection);
    }

    public IPermissionsRepository NewPermissionsRepository(IDbConnection connection) {
        return new PermissionsRepository(connection);
    }
}