using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientBankDetailsRepository {
    long Add(ClientBankDetails clientBankDetails);

    void Update(ClientBankDetails clientBankDetails);
}