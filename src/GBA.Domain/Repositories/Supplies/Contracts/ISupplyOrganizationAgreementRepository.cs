using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrganizationAgreementRepository {
    SupplyOrganizationAgreement GetById(long id);

    long Add(SupplyOrganizationAgreement agreement);

    SupplyOrganizationAgreement GetByNetId(Guid netId);

    List<SupplyOrganizationAgreement> GetAllBySupplyOrganizationId(long id);

    void Add(IEnumerable<SupplyOrganizationAgreement> agreements);

    void Update(IEnumerable<SupplyOrganizationAgreement> agreements);

    void UpdateCurrentAmount(SupplyOrganizationAgreement agreement);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllBySupplyOrganizationId(long id);

    void Update(SupplyOrganizationAgreement agreement);
}