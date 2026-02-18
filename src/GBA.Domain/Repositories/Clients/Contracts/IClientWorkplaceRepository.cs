using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientWorkplaceRepository {
    long AddClientWorkplace(ClientWorkplace workplace);

    void Update(ClientWorkplace workplace);

    void RemoveById(long id);

    IEnumerable<Client> GetWorkplacesByMainClientId(long id);

    IEnumerable<Client> GetWorkplacesByMainClientNetId(Guid netId);

    IEnumerable<Client> GetWorkplacesByClientGroupId(long id);

    IEnumerable<Client> GetWorkplacesByClientGroupNetId(Guid netId);
}