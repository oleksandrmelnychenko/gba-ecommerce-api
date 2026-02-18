using System.Data;

namespace GBA.Domain.Repositories.Filters.Contracts;

public interface IFilterItemRepositoriesFactory {
    IFilterOperationItemRepository NewFilterOperationItemRepository(IDbConnection connection);

    IFilterItemRepository NewFilterItemRepository(IDbConnection connection);
}