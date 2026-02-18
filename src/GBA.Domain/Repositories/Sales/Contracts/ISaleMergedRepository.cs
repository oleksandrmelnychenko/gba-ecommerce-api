using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales.SaleMerges;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleMergedRepository {
    long Add(SaleMerged saleMerged);

    void Add(List<SaleMerged> salesMerged);

    void Remove(Guid netId);
}