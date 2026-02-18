using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IDynamicProductPlacementRowRepository {
    long Add(DynamicProductPlacementRow row);

    void Add(IEnumerable<DynamicProductPlacementRow> rows);

    void Update(DynamicProductPlacementRow row);

    void Update(IEnumerable<DynamicProductPlacementRow> rows);

    void RemoveById(long id);

    void RemoveAllByColumnIdExceptProvided(long columnId, IEnumerable<long> ids);

    DynamicProductPlacementRow GetById(long id);

    DynamicProductPlacementRow GetByIdWithoutIncludes(long id);

    DynamicProductPlacementRow GetByNetId(Guid netId);

    List<DynamicProductPlacementRow> GetAllByColumnIdExceptProvidedIds(long columnId, IEnumerable<long> ids);
}