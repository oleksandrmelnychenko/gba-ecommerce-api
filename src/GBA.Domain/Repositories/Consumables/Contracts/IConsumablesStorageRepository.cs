using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumablesStorageRepository {
    long Add(ConsumablesStorage consumablesStorage);

    void Update(ConsumablesStorage consumablesStorage);

    ConsumablesStorage GetById(long id);

    ConsumablesStorage GetByNetId(Guid netId);

    IEnumerable<ConsumablesStorage> GetAll();

    IEnumerable<ConsumablesStorage> GetAllFromSearch(string value);

    void Remove(Guid netId);
}