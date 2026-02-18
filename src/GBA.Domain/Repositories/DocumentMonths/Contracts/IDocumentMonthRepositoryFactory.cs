using System.Data;

namespace GBA.Domain.Repositories.DocumentMonths.Contracts;

public interface IDocumentMonthRepositoryFactory {
    IDocumentMonthRepository NewDocumentMonthRepository(IDbConnection connection);
}