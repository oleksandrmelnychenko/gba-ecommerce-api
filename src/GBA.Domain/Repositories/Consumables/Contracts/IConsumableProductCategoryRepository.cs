using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumableProductCategoryRepository {
    long Add(ConsumableProductCategory consumableProductCategory);

    void Update(ConsumableProductCategory consumableProductCategory);

    ConsumableProductCategory GetById(long id);

    ConsumableProductCategory GetByNetId(Guid netId);

    IEnumerable<ConsumableProductCategory> GetAll();

    IEnumerable<ConsumableProductCategory> GetAllFromSearch(string value);

    void Remove(Guid netId);

    ConsumableProductCategory GetConsumableProductCategoriesSupplyService(string value);

    bool IsCategoryForSupplyService();

    void UpdateAllCategorySupplyService();

    ConsumableProductCategory GetConsumableCategoriesSupplyServiceIfExist();
}