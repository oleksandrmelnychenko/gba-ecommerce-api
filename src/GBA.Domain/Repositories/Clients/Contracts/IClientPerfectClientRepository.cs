using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientPerfectClientRepository {
    void Add(IEnumerable<ClientPerfectClient> clients);

    void Update(IEnumerable<ClientPerfectClient> clients);

    IEnumerable<ClientPerfectClient> GetAllByClientId(long id);

    void Remove(IEnumerable<ClientPerfectClient> clients);
}