using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyProFormRepository {
    long Add(SupplyProForm supplyProform);

    void Update(SupplyProForm supplyProForm);

    SupplyProForm GetByNetId(Guid netId);

    SupplyProForm GetById(long id);

    SupplyProForm GetByIdWithoutIncludes(long id);

    SupplyProForm GetByIdWithAllInclueds(long id);

    SupplyProForm GetByNetIdWithAllInclueds(Guid netId);

    List<SupplyProForm> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);
}