using System;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientInRoleRepository {
    void Add(ClientInRole clientInRole);

    void Update(ClientInRole clientInRole);

    void Remove(Guid netid);
}