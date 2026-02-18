using System.Data;

namespace GBA.Domain.Repositories.OriginalNumbers.Contracts;

public interface IOriginalNumberRepositoryFactory {
    IOriginalNumberRepository NewOriginalNumberRepository(IDbConnection connection);
}