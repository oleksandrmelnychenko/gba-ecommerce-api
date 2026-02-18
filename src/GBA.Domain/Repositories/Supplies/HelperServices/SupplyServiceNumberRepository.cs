using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class SupplyServiceNumberRepository : ISupplyServiceNumberRepository {
    private readonly IDbConnection _connection;

    public SupplyServiceNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(string number, bool isPoland = true) {
        _connection.Execute(
            "INSERT INTO [SupplyServiceNumber] (Number, IsPoland, Updated) " +
            "VALUES (@Number, @IsPoland, GETUTCDATE())",
            new { Number = number, IsPoland = isPoland }
        );
    }

    public SupplyServiceNumber GetLastRecord(bool isPoland = true) {
        return _connection.Query<SupplyServiceNumber>(
                "SELECT TOP(1) [SupplyServiceNumber].* " +
                "FROM [SupplyServiceNumber] " +
                "WHERE [SupplyServiceNumber].IsPoland = @IsPoland " +
                "ORDER BY [SupplyServiceNumber].ID DESC",
                new { IsPoland = isPoland }
            )
            .SingleOrDefault();
    }
}