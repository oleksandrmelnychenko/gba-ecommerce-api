using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientTypeRepository {
    long Add(ClientType clientType);

    void Update(ClientType clientType);

    ClientType GetById(long id);

    ClientType GetByNetId(Guid netId);

    List<ClientType> GetAll(bool withReSale = false);

    void Remove(Guid netId);
}