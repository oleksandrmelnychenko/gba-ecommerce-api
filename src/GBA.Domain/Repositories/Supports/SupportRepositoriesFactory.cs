using System.Data;
using GBA.Domain.Repositories.Supports.Contracts;

namespace GBA.Domain.Repositories.Supports;

public sealed class SupportRepositoriesFactory : ISupportRepositoriesFactory {
    public ISupportVideoRepository NewSupportVideoRepository(IDbConnection connection) {
        return new SupportVideoRepository(connection);
    }
}