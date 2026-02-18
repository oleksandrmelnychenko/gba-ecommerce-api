using System.Collections.Generic;
using GBA.Domain.Entities.Products.Transfers;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductTransferItemRepository {
    long Add(ProductTransferItem item);

    void Add(IEnumerable<ProductTransferItem> items);
}