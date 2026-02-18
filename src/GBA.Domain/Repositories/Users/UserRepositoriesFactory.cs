using System.Data;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Domain.Repositories.Users;

public sealed class UserRepositoriesFactory : IUserRepositoriesFactory {
    public IUserRepository NewUserRepository(IDbConnection connection) {
        return new UserRepository(connection);
    }

    public IUserScreenResolutionRepository NewUserScreenResolutionRepository(IDbConnection connection) {
        return new UserScreenResolutionRepository(connection);
    }
}