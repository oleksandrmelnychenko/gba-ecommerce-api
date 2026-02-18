using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPortWorkServiceRepository {
    long Add(PortWorkService portWorkService);

    void Update(PortWorkService portWorkService);

    void UpdateSupplyPaymentTaskId(IEnumerable<PortWorkService> containerServices);

    PortWorkService GetById(long id);

    PortWorkService GetByNetId(Guid netId);

    PortWorkService GetByIdWithoutIncludes(long id);

    List<PortWorkService> GetAllRanged(DateTime from, DateTime to);

    List<PortWorkService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);
}