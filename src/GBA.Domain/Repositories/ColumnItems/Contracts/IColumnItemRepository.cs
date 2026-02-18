using System;
using System.Collections.Generic;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Repositories.ColumnItems.Contracts;

public interface IColumnItemRepository {
    long Add(ColumnItem columnItem);

    void Update(ColumnItem columnItem);

    ColumnItem GetById(long id);

    ColumnItem GetByNetId(Guid netId);

    List<ColumnItem> GetAllByTypeAndUserId(FilterEntityType type, long id);

    void Remove(Guid netId);
}