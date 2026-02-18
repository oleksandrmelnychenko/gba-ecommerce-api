using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Repositories.Dashboards.Contracts;

public interface IDashboardNodeRepository {
    long Add(DashboardNode node);

    void Update(DashboardNode node);

    DashboardNode GetById(long id);

    DashboardNode GetByNetId(Guid netId);

    List<DashboardNode> GetAllChilds(long id);

    void Remove(Guid netId);
}