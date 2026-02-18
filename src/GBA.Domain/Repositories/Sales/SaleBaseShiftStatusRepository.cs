using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleBaseShiftStatusRepository : ISaleBaseShiftStatusRepository {
    private readonly IDbConnection _connection;

    public SaleBaseShiftStatusRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleBaseShiftStatus saleBaseShiftStatus) {
        return _connection.Query<long>(
                "INSERT INTO SaleBaseShiftStatus (ShiftStatus, Comment, Updated) " +
                "VALUES (@ShiftStatus, @Comment, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                saleBaseShiftStatus
            )
            .Single();
    }

    public void Update(SaleBaseShiftStatus saleBaseShiftStatus) {
        _connection.Execute(
            "UPDATE SaleBaseShiftStatus SET " +
            "ShiftStatus = @ShiftStatus, Comment = @Comment, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            saleBaseShiftStatus
        );
    }
}