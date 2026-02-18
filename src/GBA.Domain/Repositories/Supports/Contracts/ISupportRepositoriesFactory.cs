using System.Data;

namespace GBA.Domain.Repositories.Supports.Contracts;

public interface ISupportRepositoriesFactory {
    ISupportVideoRepository NewSupportVideoRepository(IDbConnection connection);
}