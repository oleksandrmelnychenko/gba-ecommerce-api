using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.AccountingDocumentNames;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.Repositories.Accounting.Contracts;

namespace GBA.Domain.Repositories.Accounting;

public sealed class AccountingOperationRepository : IAccountingOperationNameRepository {
    private readonly IDbConnection _connection;

    public AccountingOperationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public AccountingOperationName GetByOperationType(OperationType type) {
        return _connection.Query<AccountingOperationName>(
            "SELECT * FROM [AccountingOperationName]" +
            "WHERE [AccountingOperationName].Deleted = 0 " +
            "AND [AccountingOperationName].OperationType = @Type ",
            new { Type = type }).FirstOrDefault();
    }
}