using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleInvoiceNumberRepository : ISaleInvoiceNumberRepository {
    private readonly IDbConnection _connection;

    public SaleInvoiceNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleInvoiceNumber number) {
        return _connection.Query<long>(
                "INSERT INTO [SaleInvoiceNumber] (Number, Updated) " +
                "VALUES (@Number, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                number
            )
            .Single();
    }

    public SaleInvoiceNumber GetLastRecord() {
        return _connection.Query<SaleInvoiceNumber>(
                "SELECT TOP(1) * " +
                "FROM [SaleInvoiceNumber] " +
                "ORDER BY [SaleInvoiceNumber].ID DESC"
            )
            .SingleOrDefault();
    }
}