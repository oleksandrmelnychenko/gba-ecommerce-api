using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class CompanyCarRoadListDriverRepository : ICompanyCarRoadListDriverRepository {
    private readonly IDbConnection _connection;

    public CompanyCarRoadListDriverRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<CompanyCarRoadListDriver> companyCarRoadListDrivers) {
        _connection.Execute(
            "INSERT INTO [CompanyCarRoadListDriver] (CompanyCarRoadListId, UserId, Updated) " +
            "VALUES (@CompanyCarRoadListId, @UserId, GETUTCDATE())",
            companyCarRoadListDrivers
        );
    }

    public void Update(IEnumerable<CompanyCarRoadListDriver> companyCarRoadListDrivers) {
        _connection.Execute(
            "UPDATE [CompanyCarRoadListDriver] " +
            "SET CompanyCarRoadListId = @CompanyCarRoadListId, UserId = @UserId, Updated = GETUTCDATE() " +
            "WHERE [CompanyCarRoadListDriver].ID = @Id",
            companyCarRoadListDrivers
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [CompanyCarRoadListDriver] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [CompanyCarRoadListDriver].ID IN @Ids",
            new { Ids = ids }
        );
    }
}