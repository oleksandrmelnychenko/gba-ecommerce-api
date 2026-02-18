using System.Data;
using GBA.Domain.Repositories.DocumentMonths.Contracts;

namespace GBA.Domain.Repositories.DocumentMonths;

public sealed class DocumentMonthRepositoryFactory : IDocumentMonthRepositoryFactory {
    public IDocumentMonthRepository NewDocumentMonthRepository(IDbConnection connection) {
        return new DocumentMonthRepository(connection);
    }
}