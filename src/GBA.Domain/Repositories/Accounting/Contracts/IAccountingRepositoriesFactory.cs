using System.Data;

namespace GBA.Domain.Repositories.Accounting.Contracts;

public interface IAccountingRepositoriesFactory {
    IAccountingDocumentNameRepository NewAccountingDocumentNameRepository(IDbConnection connection);
}