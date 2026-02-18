using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IPerfectClientRepository {
    long Add(PerfectClient perfectClient);

    void Update(PerfectClient perfectClient);

    PerfectClient GetById(long id);

    PerfectClient GetByNetId(Guid netId);

    List<PerfectClient> GetAllByType(PerfectClientType type);

    List<PerfectClient> GetAll();

    List<PerfectClient> GetAll(long roleId);

    bool IsAssingedToAnyClient(long perfectClientId);

    void Remove(Guid netId);
}