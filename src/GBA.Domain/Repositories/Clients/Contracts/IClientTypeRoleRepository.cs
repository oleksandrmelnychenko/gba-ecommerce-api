using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientTypeRoleRepository {
    long Add(ClientTypeRole clientTypeRole);

    void Update(ClientTypeRole clientTypeRole);

    ClientTypeRole GetById(long id);

    ClientTypeRole GetByNetId(Guid netId);

    List<ClientTypeRole> GetAll();

    void Remove(Guid netId);
}