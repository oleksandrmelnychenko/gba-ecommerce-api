using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductLocationHistoryRepository {
    void Add(ProductLocationHistory productLocation);

    void Update(ProductLocationHistory productLocation);

    void UpdateIvoiceDocumentQty(ProductLocationHistory productLocation);

    void Remove(ProductLocationHistory productLocation);

    IEnumerable<ProductLocationHistory> GetAllByOrderItemId(long id);

    IEnumerable<ProductLocationHistory> GetAllByOrderItemIdDeleted(long id);
}