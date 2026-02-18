using System.Data;
using GBA.Domain.Repositories.Banks.Contracts;

namespace GBA.Domain.Repositories.Banks;

public sealed class BankRepositoryFactory : IBankRepositoryFactory {
    public IBankRepository NewBankRepository(IDbConnection connection) {
        return new BankRepository(connection);
    }
}