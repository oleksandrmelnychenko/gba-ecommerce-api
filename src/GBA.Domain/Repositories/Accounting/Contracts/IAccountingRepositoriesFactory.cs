using System.Data;

namespace GBA.Domain.Repositories.Accounting.Contracts;

public interface IAccountingRepositoriesFactory {
    IAccountingPayableInfoRepository NewAccountingPayableInfoRepository(IDbConnection connection, IDbConnection currencyExchangeConnection);

    IAccountingDocumentNameRepository NewAccountingDocumentNameRepository(IDbConnection connection);
}