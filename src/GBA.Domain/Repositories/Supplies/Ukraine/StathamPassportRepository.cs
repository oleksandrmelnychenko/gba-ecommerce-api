using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class StathamPassportRepository : IStathamPassportRepository {
    private readonly IDbConnection _connection;

    public StathamPassportRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(StathamPassport stathamPassport) {
        return _connection.Query<long>(
                "INSERT INTO [StathamPassport] (PassportSeria, PassportNumber, PassportIssuedBy, City, Street, HouseNumber, StathamId, " +
                "PassportIssuedDate, PassportCloseDate, Updated)" +
                "VALUES (@PassportSeria, @PassportNumber, @PassportIssuedBy, @City, @Street, @HouseNumber, @StathamId, " +
                "@PassportIssuedDate, @PassportCloseDate, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                stathamPassport
            )
            .Single();
    }

    public void Add(IEnumerable<StathamPassport> stathamPassports) {
        _connection.Execute(
            "INSERT INTO [StathamPassport] (PassportSeria, PassportNumber, PassportIssuedBy, City, Street, HouseNumber, StathamId, " +
            "PassportIssuedDate, PassportCloseDate, Updated)" +
            "VALUES (@PassportSeria, @PassportNumber, @PassportIssuedBy, @City, @Street, @HouseNumber, @StathamId, " +
            "@PassportIssuedDate, @PassportCloseDate, GETUTCDATE())",
            stathamPassports
        );
    }

    public void Update(StathamPassport stathamPassport) {
        _connection.Execute(
            "UPDATE [StathamPassport] " +
            "SET PassportSeria = @PassportSeria, PassportNumber = @PassportNumber, PassportIssuedBy = @PassportIssuedBy, City = @City, Street = @Street, " +
            "HouseNumber = @HouseNumber, StathamId = @StathamId, PassportIssuedDate = @PassportIssuedDate, PassportCloseDate = @PassportCloseDate, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            stathamPassport
        );
    }

    public void Update(IEnumerable<StathamPassport> stathamPassports) {
        _connection.Execute(
            "UPDATE [StathamPassport] " +
            "SET PassportSeria = @PassportSeria, PassportNumber = @PassportNumber, PassportIssuedBy = @PassportIssuedBy, City = @City, Street = @Street, " +
            "HouseNumber = @HouseNumber, StathamId = @StathamId, PassportIssuedDate = @PassportIssuedDate, PassportCloseDate = @PassportCloseDate, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            stathamPassports
        );
    }

    public void RemoveAllByStathamIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [StathamPassport] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE StathamID = @Id " +
            "AND ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }
}