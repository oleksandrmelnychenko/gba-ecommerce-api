using System.Data;
using GBA.Domain.Repositories.Identities.Contracts;

namespace GBA.Domain.Repositories.Identities;

public sealed class IdentityRepositoriesFactory : IIdentityRepositoriesFactory {
    private readonly IIdentityRepository _identityRepository;

    private readonly IIdentityRolesRepository _identityRolesRepository;

    public IdentityRepositoriesFactory(
        IIdentityRepository identityRepository,
        IIdentityRolesRepository identityRolesRepository) {
        _identityRepository = identityRepository;

        _identityRolesRepository = identityRolesRepository;
    }

    public IIdentityRepository NewIdentityRepository() {
        return _identityRepository;
    }

    public IIdentityRolesRepository NewIdentityRolesRepository() {
        return _identityRolesRepository;
    }

    public IUserTokenRepository NewUserTokenRepository(IDbConnection connection) {
        return new UserTokenRepository(connection);
    }
}