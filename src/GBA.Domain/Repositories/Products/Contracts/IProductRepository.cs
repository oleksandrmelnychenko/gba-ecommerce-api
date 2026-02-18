using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductRepository {
    long Add(Product product);

    void Update(Product product);
    void UpdateMainOriginalNumber(string mainOriginalNumber, Guid netUid);

    void Remove(Guid netId);

    void Remove(long id);

    void UpdateProductHasAnalogue();

    void UpdateProductHasComponent();

    void UpdateProductHasAnalogue(long baseProductId);

    void UpdateProductHasComponent(long baseProductId);

    void UncheckProductHasAnalogue(IEnumerable<long> baseProductIds);

    void UncheckProductHasComponent(IEnumerable<long> baseProductIds);
}