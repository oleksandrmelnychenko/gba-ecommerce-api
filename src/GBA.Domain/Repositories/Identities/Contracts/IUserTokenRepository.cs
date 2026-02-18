using GBA.Common.IdentityConfiguration.Entities;

namespace GBA.Domain.Repositories.Identities.Contracts;

public interface IUserTokenRepository {
    bool IsTokenExistForUser(string userId);

    void Add(UserToken userToken);

    void Update(UserToken userToken);

    UserToken GetByUserId(string userId);
}