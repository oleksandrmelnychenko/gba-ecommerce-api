using System.Data;
using GBA.Domain.Repositories.OriginalNumbers.Contracts;

namespace GBA.Domain.Repositories.OriginalNumbers;

public sealed class OriginalNumberRepositoryFactory : IOriginalNumberRepositoryFactory {
    public IOriginalNumberRepository NewOriginalNumberRepository(IDbConnection connection) {
        return new OriginalNumberRepository(connection);
    }
}