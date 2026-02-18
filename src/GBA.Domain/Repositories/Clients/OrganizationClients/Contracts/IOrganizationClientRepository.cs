using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients.OrganizationClients;

namespace GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

public interface IOrganizationClientRepository {
    long Add(OrganizationClient client);

    void Update(OrganizationClient client);

    void Remove(long id);

    void Remove(Guid netId);

    OrganizationClient GetById(long id);

    OrganizationClient GetByNetId(Guid netId);

    IEnumerable<OrganizationClient> GetAll();

    IEnumerable<OrganizationClient> GetAllFromSearch(string value);
}