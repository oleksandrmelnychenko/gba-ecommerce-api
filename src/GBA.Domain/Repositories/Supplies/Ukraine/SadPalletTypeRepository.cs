using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SadPalletTypeRepository : ISadPalletTypeRepository {
    private readonly IDbConnection _connection;

    public SadPalletTypeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SadPalletType sadPalletType) {
        return _connection.Query<long>(
                "INSERT INTO [SadPalletType] (Name, CssClass, Weight, Updated) " +
                "VALUES (@Name, @CssClass, @Weight, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                sadPalletType
            )
            .Single();
    }

    public void Update(SadPalletType sadPalletType) {
        _connection.Execute(
            "UPDATE [SadPalletType] " +
            "SET Name = @Name, CssClass = @CssClass, Weight = @Weight, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            sadPalletType
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [SadPalletType] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public SadPalletType GetById(long id) {
        return _connection.Query<SadPalletType>(
                "SELECT * " +
                "FROM [SadPalletType] " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SadPalletType GetByNetId(Guid netId) {
        return _connection.Query<SadPalletType>(
                "SELECT * " +
                "FROM [SadPalletType] " +
                "WHERE NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<SadPalletType> GetAll() {
        return _connection.Query<SadPalletType>(
            "SELECT * " +
            "FROM [SadPalletType] " +
            "WHERE Deleted = 0"
        );
    }
}