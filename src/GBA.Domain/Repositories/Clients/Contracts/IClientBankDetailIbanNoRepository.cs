using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientBankDetailIbanNoRepository {
    long Add(ClientBankDetailIbanNo clientBankDetailIbanNo);

    void Update(ClientBankDetailIbanNo clientBankDetailIbanNo);
}