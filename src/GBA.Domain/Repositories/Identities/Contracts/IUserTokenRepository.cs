using GBA.Common.IdentityConfiguration.Entities;

namespace GBA.Domain.Repositories.Identities.Contracts;

public interface IUserTokenRepository {
    bool IsTokenExistForUser(string userId);

    void Add(UserToken userToken);

    void Update(UserToken userToken);

    bool TryReplace(long id, string userId, string expectedToken, string replacementToken);

    UserToken GetByUserId(string userId);
}
