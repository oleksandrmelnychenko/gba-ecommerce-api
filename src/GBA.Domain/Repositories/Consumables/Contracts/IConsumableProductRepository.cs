using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumableProductRepository {
    long Add(ConsumableProduct consumableProduct);

    void Update(ConsumableProduct consumableProduct);

    ConsumableProduct GetLastRecord();

    ConsumableProduct GetById(long id);

    ConsumableProduct GetByNetId(Guid netId);

    IEnumerable<ConsumableProduct> GetAll();

    IEnumerable<ConsumableProduct> GetAllFromSearchByVendorCode(string value);

    void Remove(Guid netId);
}