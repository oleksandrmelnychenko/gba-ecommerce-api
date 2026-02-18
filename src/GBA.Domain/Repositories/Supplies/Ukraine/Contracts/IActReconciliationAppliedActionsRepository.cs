using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine.AppliedActions;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IActReconciliationAppliedActionsRepository {
    List<ActReconciliationAppliedAction> GetAllAppliedActionsByActReconciliationNetId(Guid netId);
}