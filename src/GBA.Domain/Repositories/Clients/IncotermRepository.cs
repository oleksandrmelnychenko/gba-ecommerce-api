using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class IncotermRepository : IIncotermRepository {
    private readonly IDbConnection _connection;

    public IncotermRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Incoterm incoterm) {
        return _connection.Query<long>(
                "INSERT INTO [Incoterm] (IncotermName, Updated) " +
                "VALUES (@IncotermName, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                incoterm
            )
            .Single();
    }

    public void Update(Incoterm incoterm) {
        _connection.Execute(
            "UPDATE [Incoterm] " +
            "SET IncotermName = @IncotermName, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            incoterm
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [Incoterm] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public Incoterm GetById(long id) {
        return _connection.Query<Incoterm>(
                "SELECT * " +
                "FROM [Incoterm] " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .Single();
    }

    public Incoterm GetByNetId(Guid netId) {
        return _connection.Query<Incoterm>(
                "SELECT * " +
                "FROM [Incoterm] " +
                "WHERE NetUID = @NetId",
                new { NetId = netId }
            )
            .Single();
    }

    public IEnumerable<Incoterm> GetAll() {
        return _connection.Query<Incoterm>(
            "SELECT * " +
            "FROM [Incoterm] " +
            "WHERE Deleted = 0"
        );
    }
}