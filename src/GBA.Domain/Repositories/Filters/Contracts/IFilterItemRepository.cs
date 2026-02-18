using System.Collections.Generic;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Repositories.Filters.Contracts;

public interface IFilterItemRepository {
    List<FilterItem> GetAllByType(FilterEntityType type);

    FilterItem GetClientTypeRoleFilterItem();
}