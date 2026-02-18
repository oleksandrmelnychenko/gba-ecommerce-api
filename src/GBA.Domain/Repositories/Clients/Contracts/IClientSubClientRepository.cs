using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientSubClientRepository {
    void Add(ClientSubClient clientSubClient);

    List<ClientSubClient> GetAllClientSubClients(Guid clientNetId);

    IEnumerable<ClientSubClient> GetAllByRootClientId(long clientId);

    ClientSubClient GetRootBySubClientNetId(Guid subClientNetId);

    ClientSubClient GetByClientIdIfExists(long clientId);
}