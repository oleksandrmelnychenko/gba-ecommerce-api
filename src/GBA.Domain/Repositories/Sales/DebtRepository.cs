using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class DebtRepository : IDebtRepository {
    private readonly IDbConnection _connection;

    public DebtRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Debt debt) {
        return _connection.Query<long>(
                "INSERT INTO Debt (Total, Days, Updated) " +
                "VALUES (@Total, @Days, getutcdate());" +
                "SELECT SCOPE_IDENTITY()",
                debt
            )
            .Single();
    }

    public long AddWithCreatedDate(Debt debt) {
        return _connection.Query<long>(
                "INSERT INTO Debt (Total, Days, Created, Updated) " +
                "VALUES (@Total, @Days, @Created, getutcdate());" +
                "SELECT SCOPE_IDENTITY()",
                debt
            )
            .Single();
    }


    public List<Debt> GetAll() {
        return _connection.Query<Debt>(
                "SELECT * FROM Debt WHERE Deleted = 0"
            )
            .ToList();
    }

    public Debt GetById(long id) {
        return _connection.Query<Debt>(
                "SELECT * FROM Debt WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Debt GetByNetId(Guid netId) {
        return _connection.Query<Debt>(
                "SELECT * FROM Debt WHERE NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Debt SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(Debt debt) {
        _connection.Execute(
            "UPDATE Debt SET Total = @Total, Days = @Days, Updated = getutcdate() WHERE NetUID = @NetUid",
            debt
        );
    }

    public List<dynamic> GetTopByAllClients() {
        return _connection.Query<dynamic>(
                "SELECT TOP(5) Client.FullName " +
                ",SUM(Debt.Total) AS TotalAmount " +
                "FROM Debt " +
                "LEFT JOIN ClientInDebt " +
                "ON ClientInDebt.DebtID = Debt.ID " +
                "LEFT JOIN Client " +
                "ON ClientInDebt.ClientID = Client.ID " +
                "WHERE ClientInDebt.Deleted = 0 " +
                "GROUP BY Client.FullName " +
                "ORDER BY TotalAmount DESC"
            )
            .ToList();
    }

    public List<dynamic> GetTopByManagers() {
        return _connection.Query<dynamic>(
                "SELECT TOP(5) SUM(Debt.Total) AS TotalAmount " +
                ",[User].LastName " +
                "FROM Debt " +
                "LEFT JOIN ClientInDebt " +
                "ON Debt.ID = ClientInDebt.ID " +
                "LEFT JOIN Client " +
                "ON ClientInDebt.ClientID = Client.ID " +
                "LEFT JOIN ClientUserProfile " +
                "ON ClientUserProfile.ClientID = Client.ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = ClientUserProfile.UserProfileID " +
                "WHERE Debt.Deleted = 0 " +
                "AND ClientInDebt.Deleted = 0 " +
                "GROUP BY [User].LastName " +
                "ORDER BY TotalAmount DESC, [User].LastName "
            )
            .ToList();
    }
}