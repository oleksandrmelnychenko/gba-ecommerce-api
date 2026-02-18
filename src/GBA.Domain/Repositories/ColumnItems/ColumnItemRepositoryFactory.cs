using System.Data;
using GBA.Domain.Repositories.ColumnItems.Contracts;

namespace GBA.Domain.Repositories.ColumnItems;

public sealed class ColumnItemRepositoryFactory : IColumnItemRepositoryFactory {
    public IColumnItemRepository NewColumnItemRepository(IDbConnection connection) {
        return new ColumnItemRepository(connection);
    }
}