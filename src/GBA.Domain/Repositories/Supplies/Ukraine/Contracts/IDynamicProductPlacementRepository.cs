using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IDynamicProductPlacementRepository {
    long Add(DynamicProductPlacement placement);

    void Add(IEnumerable<DynamicProductPlacement> placements);

    void Update(DynamicProductPlacement placement);

    void Update(IEnumerable<DynamicProductPlacement> placements);

    void SetIsAppliedById(long id);

    void RemoveAllByRowId(long rowId);

    void RemoveAllByRowIdExceptProvided(long rowId, IEnumerable<long> ids);

    IEnumerable<DynamicProductPlacement> GetAllAppliedByRowId(long rowId);

    IEnumerable<DynamicProductPlacement> GetAllByRowIdExceptProvided(long rowId, IEnumerable<long> ids);
}