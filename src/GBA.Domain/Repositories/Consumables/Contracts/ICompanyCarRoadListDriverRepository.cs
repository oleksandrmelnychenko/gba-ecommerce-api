using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface ICompanyCarRoadListDriverRepository {
    void Add(IEnumerable<CompanyCarRoadListDriver> companyCarRoadListDrivers);

    void Update(IEnumerable<CompanyCarRoadListDriver> companyCarRoadListDrivers);

    void RemoveAllByIds(IEnumerable<long> ids);
}