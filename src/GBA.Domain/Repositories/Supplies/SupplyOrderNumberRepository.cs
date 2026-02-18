using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyOrderNumberRepository : ISupplyOrderNumberRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderNumber supplyOrderNumber) {
        return _connection.Query<long>(
                "INSERT INTO SupplyOrderNumber (Number, Updated) " +
                "VALUES(@Number, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                supplyOrderNumber
            )
            .Single();
    }

    public SupplyOrderNumber GetLastRecord() {
        return _connection.Query<SupplyOrderNumber>(
                "SELECT TOP(1) * FROM SupplyOrderNumber " +
                "WHERE [Number] != N'Ввід боргів з 1С' " +
                "ORDER BY SupplyOrderNumber.Created DESC"
            )
            .SingleOrDefault();
    }

    public void Remove(SupplyOrderNumber supplyOrderNumber) {
        _connection.Execute(
            "UPDATE SupplyOrderNumber SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            supplyOrderNumber
        );
    }

    public void Update(SupplyOrderNumber supplyOrderNumber) {
        _connection.Execute(
            "UPDATE SupplyOrderNumber SET Number = @Number, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            supplyOrderNumber
        );
    }
}