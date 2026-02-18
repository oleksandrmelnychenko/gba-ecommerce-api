using System.Data;

namespace GBA.Domain.Repositories.UserRoles.Contracts;

public interface IUserRoleRepositoriesFactory {
    IUserRoleRepository NewUserRoleRepository(IDbConnection connection);

    IUserRoleTranslationRepository NewUserRoleTranslationRepository(IDbConnection connection);

    IUserRoleDashboardNodeRepository NewUserRoleDashboardNodeRepository(IDbConnection connection);

    IPermissionsRepository NewPermissionsRepository(IDbConnection connection);
}