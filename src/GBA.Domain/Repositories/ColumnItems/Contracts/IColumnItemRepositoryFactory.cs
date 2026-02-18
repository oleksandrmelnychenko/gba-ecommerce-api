using System.Data;

namespace GBA.Domain.Repositories.ColumnItems.Contracts;

public interface IColumnItemRepositoryFactory {
    IColumnItemRepository NewColumnItemRepository(IDbConnection connection);
}