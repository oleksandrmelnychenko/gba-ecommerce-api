using System.Data;

namespace GBA.Domain.Repositories.Banks.Contracts;

public interface IBankRepositoryFactory {
    IBankRepository NewBankRepository(IDbConnection connection);
}