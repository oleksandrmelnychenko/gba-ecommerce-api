using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Dashboards;

namespace GBA.Domain.Repositories.Dashboards.Contracts;

public interface IDashboardNodeModuleRepository {
    long Add(DashboardNodeModule module);

    void Update(DashboardNodeModule module);

    DashboardNodeModule GetById(long id);

    DashboardNodeModule GetByNetId(Guid netId);

    List<DashboardNodeModule> GetAll();

    List<DashboardNodeModule> GetAllByUserRoleId(long id);

    void Remove(Guid netId);
}