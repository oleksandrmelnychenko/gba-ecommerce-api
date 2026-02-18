using System;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IBaseLifeCycleStatusRepository {
    long Add(BaseLifeCycleStatus baseLifeCycleStatus);

    void Update(BaseLifeCycleStatus baseLifeCycleStatus);

    BaseLifeCycleStatus GetByNetId(Guid netId);
}