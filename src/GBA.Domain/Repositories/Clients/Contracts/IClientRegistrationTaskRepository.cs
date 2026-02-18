using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientRegistrationTaskRepository {
    void Add(ClientRegistrationTask clientRegistrationTask);

    void SetDoneByClientId(long id);
}