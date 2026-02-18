using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.OriginalNumbers.Contracts;

namespace GBA.Domain.Repositories.OriginalNumbers;

public sealed class OriginalNumberRepository : IOriginalNumberRepository {
    private readonly IDbConnection _connection;

    public OriginalNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OriginalNumber originalNumber) {
        return _connection.Query<long>(
                "INSERT INTO OriginalNumber (Number, MainNumber, Updated) " +
                "VALUES(@Number, @MainNumber, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                originalNumber)
            .Single();
    }

    public List<OriginalNumber> GetAll() {
        return _connection.Query<OriginalNumber>(
                "SELECT * FROM OriginalNumber WHERE Deleted = 0")
            .ToList();
    }

    public OriginalNumber GetById(long id) {
        return _connection.Query<OriginalNumber>(
                "SELECT * FROM OriginalNumber " +
                "WHERE ID = @Id",
                new { Id = id })
            .SingleOrDefault();
    }

    public OriginalNumber GetByNetId(Guid netId) {
        return _connection.Query<OriginalNumber>(
                "SELECT * FROM OriginalNumber " +
                "WHERE NetUID = @NetId",
                new { NetId = netId })
            .SingleOrDefault();
    }

    public OriginalNumber GetByNumber(string number) {
        return _connection.Query<OriginalNumber>(
                "SELECT * " +
                "FROM OriginalNumber " +
                "WHERE MainNumber = @Number",
                new { Number = number })
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE OriginalNumber SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId });
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OriginalNumber] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void DeleteAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "DELETE FROM [OriginalNumber] " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void Update(OriginalNumber originalNumber) {
        _connection.Execute(
            "UPDATE OriginalNumber " +
            "SET Number = @Number, MainNumber = @MainNumber, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            originalNumber);
    }
}