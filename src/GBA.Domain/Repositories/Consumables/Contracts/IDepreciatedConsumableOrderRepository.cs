using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IDepreciatedConsumableOrderRepository {
    long Add(DepreciatedConsumableOrder depreciatedConsumableOrder);

    void Update(DepreciatedConsumableOrder depreciatedConsumableOrder);

    void Remove(Guid netId);

    void Remove(long id);

    DepreciatedConsumableOrder GetLastRecord();

    DepreciatedConsumableOrder GetById(long id);

    DepreciatedConsumableOrder GetByNetIdWithoutIncludes(Guid netId);

    IEnumerable<DepreciatedConsumableOrder> GetAll();

    IEnumerable<DepreciatedConsumableOrder> GetAllFiltered(DateTime from, DateTime to, string value, Guid? storageNetId);
}