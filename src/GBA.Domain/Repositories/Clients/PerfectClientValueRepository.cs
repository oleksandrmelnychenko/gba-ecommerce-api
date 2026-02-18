using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class PerfectClientValueRepository : IPerfectClientValueRepository {
    private readonly IDbConnection _connection;

    public PerfectClientValueRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PerfectClientValue value) {
        return _connection.Query<long>(
                "INSERT INTO PerfectClientValue (Value, IsSelected, PerfectClientId, Updated) " +
                "VALUES (@Value, 0, @PerfectClientId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                value
            )
            .Single();
    }

    public void Add(IEnumerable<PerfectClientValue> values) {
        _connection.Execute(
            "INSERT INTO PerfectClientValue (Value, IsSelected, PerfectClientId, Updated) " +
            "VALUES (@Value, 0, @PerfectClientId, getutcdate())",
            values
        );
    }

    public void Update(IEnumerable<PerfectClientValue> values) {
        _connection.Execute(
            "UPDATE PerfectClientValue SET " +
            "Value = @Value, IsSelected = 0, PerfectClientId = @PerfectClientId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            values
        );
    }

    public void Update(PerfectClientValue value) {
        _connection.Execute(
            "UPDATE PerfectClientValue SET " +
            "Value = @Value, IsSelected = 0, PerfectClientId = @PerfectClientId, Updated = getutcdate()" +
            "WHERE NetUID = @NetUid",
            value
        );
    }
}