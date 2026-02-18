using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class StathamCarRepository : IStathamCarRepository {
    private readonly IDbConnection _connection;

    public StathamCarRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<StathamCar> cars) {
        _connection.Execute(
            "INSERT INTO [StathamCar] (Volume, Number, StathamId, Updated) " +
            "VALUES (@Volume, @Number, @StathamId, GETUTCDATE())",
            cars
        );
    }

    public void Update(IEnumerable<StathamCar> cars) {
        _connection.Execute(
            "UPDATE [StathamCar] " +
            "SET Volume = @Volume, Number = @Number, StathamId = @StathamId, Updated = GETUTCDATE() " +
            "WHERE [StathamCar].ID = @Id",
            cars
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [StathamCar] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [StathamCar].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByStathamIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [StathamCar] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [StathamCar].ID NOT IN @Ids " +
            "AND [StathamCar].StathamID = @Id " +
            "AND [StathamCar].Deleted = 0",
            new { Id = id, Ids = ids }
        );
    }
}