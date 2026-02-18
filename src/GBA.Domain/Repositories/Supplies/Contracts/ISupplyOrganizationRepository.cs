using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrganizationRepository {
    long Add(SupplyOrganization supplyOrganization);

    void Update(SupplyOrganization supplyOrganization);

    SupplyOrganization GetById(long id);

    SupplyOrganization GetByNetId(Guid netId);

    List<SupplyOrganization> GetAll(Guid? organizationNetId);

    List<SupplyOrganization> GetAll();

    List<SupplyOrganization> GetAllFromSearchFiltered(string value, long limit, long offset);

    List<SupplyOrganization> GetAllFromSearch(string value, Guid? organizationNetId);

    void Remove(Guid netId);
}