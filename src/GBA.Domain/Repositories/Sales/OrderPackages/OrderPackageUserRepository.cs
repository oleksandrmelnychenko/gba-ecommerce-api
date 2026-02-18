using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales.OrderPackages;

public sealed class OrderPackageUserRepository : IOrderPackageUserRepository {
    private readonly IDbConnection _connection;

    public OrderPackageUserRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<OrderPackageUser> orderPackageUsers) {
        _connection.Execute(
            "INSERT INTO [OrderPackageUser] (UserId, OrderPackageId, Updated) " +
            "VALUES (@UserId, @OrderPackageId, getutcdate())",
            orderPackageUsers
        );
    }

    public void RemoveAllByOrderPackageId(long orderPackageId) {
        _connection.Execute(
            "UPDATE [OrderPackageUser] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [OrderPackageUser].OrderPackageID = @OrderPackageId",
            new { OrderPackageId = orderPackageId }
        );
    }

    public void RemoveAllByOrderPackageIdExceptProvided(long orderPackageId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrderPackageUser] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [OrderPackageUser].OrderPackageID = @OrderPackageId " +
            "AND [OrderPackageUser].ID NOT IN @Ids",
            new { OrderPackageId = orderPackageId, Ids = ids }
        );
    }
}