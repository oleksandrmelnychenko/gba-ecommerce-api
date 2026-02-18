using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientUserProfileRepository {
    void Add(IEnumerable<ClientUserProfile> clientUserProfiles);

    void Update(IEnumerable<ClientUserProfile> clientUserProfiles);

    void Remove(IEnumerable<ClientUserProfile> clientUserProfiles);

    void RemoveAllByClientId(long clientId);

    List<ClientUserProfile> GetAllByClientId(long id);
}