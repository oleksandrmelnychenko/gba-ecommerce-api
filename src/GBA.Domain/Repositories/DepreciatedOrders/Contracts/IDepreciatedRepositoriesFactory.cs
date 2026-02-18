using System.Data;

namespace GBA.Domain.Repositories.DepreciatedOrders.Contracts;

public interface IDepreciatedRepositoriesFactory {
    IDepreciatedOrderRepository NewDepreciatedOrderRepository(IDbConnection connection);

    IDepreciatedOrderItemRepository NewDepreciatedOrderItemRepository(IDbConnection connection);
}