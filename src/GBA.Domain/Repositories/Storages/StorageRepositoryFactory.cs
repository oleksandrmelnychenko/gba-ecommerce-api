using System.Data;
using GBA.Domain.Repositories.Storages.Contracts;

namespace GBA.Domain.Repositories.Storages;

public sealed class StorageRepositoryFactory : IStorageRepositoryFactory {
    public IStorageRepository NewStorageRepository(IDbConnection connection) {
        return new StorageRepository(connection);
    }
}