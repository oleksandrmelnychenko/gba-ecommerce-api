using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductLocationRepository {
    void Add(ProductLocation productLocation);

    void Update(ProductLocation productLocation);

    void UpdateIvoiceDocumentQty(ProductLocation productLocation);

    void Remove(ProductLocation productLocation);

    IEnumerable<ProductLocation> GetAllByOrderItemId(long id);

    IEnumerable<ProductLocation> GetAllByOrderItemIdDeleted(long id);
}