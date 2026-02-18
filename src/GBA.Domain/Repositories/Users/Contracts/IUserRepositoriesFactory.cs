using System.Data;

namespace GBA.Domain.Repositories.Users.Contracts;

public interface IUserRepositoriesFactory {
    IUserRepository NewUserRepository(IDbConnection connection);

    IUserScreenResolutionRepository NewUserScreenResolutionRepository(IDbConnection connection);
}