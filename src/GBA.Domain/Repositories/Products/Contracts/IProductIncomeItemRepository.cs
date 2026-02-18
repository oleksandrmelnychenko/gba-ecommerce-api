using System.Collections.Generic;
using GBA.Domain.Entities.Products.Incomes;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductIncomeItemRepository {
    void Add(IEnumerable<ProductIncomeItem> items);

    long Add(ProductIncomeItem item);

    void UpdateRemainingQty(ProductIncomeItem item);

    void UpdateQtyFields(ProductIncomeItem item);

    ProductIncomeItem GetByProductIncomeAndPackingListPackageOrderItemIdsIfExists(long productIncomeId, long packingListPackageOrderItemId);

    ProductIncomeItem GetByProductIncomeAndSupplyOrderUkraineItemIdsIfExists(long productIncomeId, long supplyOrderUkraineItemId, long? packingListPackageOrderItemId);
}