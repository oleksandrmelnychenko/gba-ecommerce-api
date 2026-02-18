using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ICustomServiceRepository {
    long Add(CustomService customService);

    void Add(IEnumerable<CustomService> customServices);

    void Update(CustomService customService);

    CustomService GetByIdWithoutIncludes(long id);

    List<CustomService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);
}