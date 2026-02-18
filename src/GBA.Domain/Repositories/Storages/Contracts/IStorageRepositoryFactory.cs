using System.Data;

namespace GBA.Domain.Repositories.Storages.Contracts;

public interface IStorageRepositoryFactory {
    IStorageRepository NewStorageRepository(IDbConnection connection);
}