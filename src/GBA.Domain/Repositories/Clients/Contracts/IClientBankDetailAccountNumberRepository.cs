using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientBankDetailAccountNumberRepository {
    long Add(ClientBankDetailAccountNumber clientBankDetailAccountNumber);

    void Update(ClientBankDetailAccountNumber clientBankDetailAccountNumber);
}