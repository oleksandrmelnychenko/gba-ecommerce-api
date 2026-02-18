using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IDynamicProductPlacementColumnRepository {
    long Add(DynamicProductPlacementColumn column);

    void Add(IEnumerable<DynamicProductPlacementColumn> columns);

    void Update(DynamicProductPlacementColumn column);

    void Update(IEnumerable<DynamicProductPlacementColumn> columns);

    void RemoveById(long id);
}