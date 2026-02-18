using System.Data;
using GBA.Domain.Repositories.Accounting.Contracts;

namespace GBA.Domain.Repositories.Accounting;

public sealed class AccountingRepositoriesFactory : IAccountingRepositoriesFactory {
    public IAccountingPayableInfoRepository NewAccountingPayableInfoRepository(IDbConnection connection, IDbConnection currencyExchangeConnection) {
        return new AccountingPayableInfoRepository(connection, currencyExchangeConnection);
    }

    public IAccountingDocumentNameRepository NewAccountingDocumentNameRepository(IDbConnection connection) {
        return new AccountingDocumentNameRepository(connection);
    }

    public IAccountingOperationNameRepository NewAccountingOperationNameRepository(IDbConnection connection) {
        return new AccountingOperationRepository(connection);
    }
}