using System.Data;

namespace GBA.Domain.Repositories.Identities.Contracts;

public interface IIdentityRepositoriesFactory {
    IIdentityRepository NewIdentityRepository();

    IIdentityRolesRepository NewIdentityRolesRepository();

    IUserTokenRepository NewUserTokenRepository(IDbConnection connection);
}