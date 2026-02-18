using System.Data;
using GBA.Domain.Repositories.Filters.Contracts;

namespace GBA.Domain.Repositories.Filters;

public sealed class FilterItemRepositoriesFactory : IFilterItemRepositoriesFactory {
    public IFilterItemRepository NewFilterItemRepository(IDbConnection connection) {
        return new FilterItemRepository(connection);
    }

    public IFilterOperationItemRepository NewFilterOperationItemRepository(IDbConnection connection) {
        return new FilterOperationItemRepository(connection);
    }
}