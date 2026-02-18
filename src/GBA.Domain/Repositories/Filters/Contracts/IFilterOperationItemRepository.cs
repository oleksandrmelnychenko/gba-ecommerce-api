using System.Collections.Generic;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Repositories.Filters.Contracts;

public interface IFilterOperationItemRepository {
    List<FilterOperationItem> GetAll();
}