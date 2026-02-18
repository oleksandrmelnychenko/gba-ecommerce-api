using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientGroupRepository {
    long Add(ClientGroup clientGroup);

    void Update(ClientGroup clientGroup);

    void Remove(long id);
    IEnumerable<ClientGroup> GetAll();

    IEnumerable<ClientGroup> GetAllByClientNetId(Guid netId);

    IEnumerable<ClientGroup> GetAllByClientId(long id);
}