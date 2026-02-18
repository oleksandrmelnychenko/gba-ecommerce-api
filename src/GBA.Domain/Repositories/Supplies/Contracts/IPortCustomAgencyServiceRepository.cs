using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPortCustomAgencyServiceRepository {
    long Add(PortCustomAgencyService portCustomAgencyService);

    void Update(PortCustomAgencyService portCustomAgencyService);

    PortCustomAgencyService GetById(long id);

    List<PortCustomAgencyService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    PortCustomAgencyService GetByIdWithoutIncludes(long id);
}