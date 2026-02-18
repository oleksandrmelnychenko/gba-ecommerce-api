using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientOneCRepository : IClientOneCRepository {
    private readonly IDbConnection _connection;

    public ClientOneCRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<long> GetOldEcommerceIdsFromSearchBySales(string value) {
        return _connection.Query<long>(
            "SELECT " +
            "[Clients]._Code AS OldEcommerceId " +
            "FROM [_Reference68] AS [Clients] " +
            "WHERE [Clients]._Marked = 0 " +
            "AND (   PATINDEX(N'%' + @Value + N'%', [Clients]._Fld1115) > 0 " +
            "OR PATINDEX(N'%' + @Value + N'%', [Clients]._Fld1130) > 0 " +
            "OR PATINDEX(N'%' + @Value + N'%', [Clients]._Description) > 0) " +
            "AND [Clients]._IDRRef IN (SELECT [ClientOrder]._Fld3196RRef " +
            "FROM [_Document186] AS [ClientOrder] " +
            "WHERE [ClientOrder]._Posted = 1)",
            new { Value = value }
        );
    }
}