using System.Collections.Generic;
using GBA.Domain.Entities.Clients.OrganizationClients;

namespace GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

public interface IOrganizationClientAgreementRepository {
    void Add(OrganizationClientAgreement agreement);

    void Add(IEnumerable<OrganizationClientAgreement> agreements);

    void Update(OrganizationClientAgreement agreement);

    void Update(IEnumerable<OrganizationClientAgreement> agreements);

    OrganizationClientAgreement GetLastRecord();

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByClientIdExceptProvided(long clientId, IEnumerable<long> ids);
}