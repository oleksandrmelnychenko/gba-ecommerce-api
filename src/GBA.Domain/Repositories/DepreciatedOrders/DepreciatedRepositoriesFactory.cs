using System.Data;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;

namespace GBA.Domain.Repositories.DepreciatedOrders;

public sealed class DepreciatedRepositoriesFactory : IDepreciatedRepositoriesFactory {
    public IDepreciatedOrderRepository NewDepreciatedOrderRepository(IDbConnection connection) {
        return new DepreciatedOrderRepository(connection);
    }

    public IDepreciatedOrderItemRepository NewDepreciatedOrderItemRepository(IDbConnection connection) {
        return new DepreciatedOrderItemRepository(connection);
    }
}