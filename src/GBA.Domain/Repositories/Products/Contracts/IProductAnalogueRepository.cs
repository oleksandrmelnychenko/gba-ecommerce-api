using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductAnalogueRepository {
    long Add(ProductAnalogue productAnalogue);

    void Add(IEnumerable<ProductAnalogue> productAnalogues);

    void Update(ProductAnalogue productAnalogue);

    void Update(IEnumerable<ProductAnalogue> productAnalogues);

    void Remove(ProductAnalogue productAnalogue);

    void Remove(IEnumerable<ProductAnalogue> productAnalogues);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByProductId(long productId);

    void DeleteByBaseProductAndAnalogueNetIds(Guid baseProductNetId, Guid analogueNetId);

    void DeleteAllByIds(IEnumerable<long> ids);

    bool CheckIsProductAnalogueExistsByBaseProductAndAnalogueIds(long baseProductId, long analogueId);

    bool CheckIsProductAnalogueExistsByBaseProductAndAnalogueNetIds(Guid baseProductNetId, Guid analogueNetId);

    List<ProductAnalogue> GetAllProductAnaloguesByBaseProductVendorCode(string vendorCode);

    List<ProductAnalogue> GetAllProductAnaloguesByAnalogueVendorCode(string vendorCode);
}