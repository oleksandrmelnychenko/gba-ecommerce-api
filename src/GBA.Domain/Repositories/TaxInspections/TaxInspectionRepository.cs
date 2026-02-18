using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.TaxInspections.Contracts;

namespace GBA.Domain.Repositories.TaxInspections;

public sealed class TaxInspectionRepository : ITaxInspectionRepository {
    private readonly IDbConnection _connection;

    public TaxInspectionRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(TaxInspection taxInspection) {
        return _connection.Query<long>(
                "INSERT INTO [TaxInspection] " +
                "(InspectionNumber, InspectionType, InspectionName, InspectionRegionName, InspectionRegionCode, InspectionAddress, InspectionUSREOU, Updated) " +
                "VALUES " +
                "(@InspectionNumber, @InspectionType, @InspectionName, @InspectionRegionName, @InspectionRegionCode, @InspectionAddress, @InspectionUSREOU, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                taxInspection
            )
            .Single();
    }

    public void Update(TaxInspection taxInspection) {
        _connection.Execute(
            "UPDATE [TaxInspection] " +
            "SET InspectionNumber = @InspectionNumber, InspectionType = @InspectionType, InspectionName = @InspectionName, InspectionRegionName = @InspectionRegionName, " +
            "InspectionRegionCode = @InspectionRegionCode, InspectionAddress = @InspectionAddress, InspectionUSREOU = @InspectionUSREOU, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            taxInspection
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [TaxInspection] " +
            "SET Deleted = 0, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [TaxInspection] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public TaxInspection GetById(long id) {
        return _connection.Query<TaxInspection>(
                "SELECT * " +
                "FROM [TaxInspection] " +
                "WHERE [TaxInspection].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public TaxInspection GetByNetId(Guid netId) {
        return _connection.Query<TaxInspection>(
                "SELECT * " +
                "FROM [TaxInspection] " +
                "WHERE [TaxInspection].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<TaxInspection> GetAll() {
        return _connection.Query<TaxInspection>(
            "SELECT * " +
            "FROM [TaxInspection] " +
            "WHERE [TaxInspection].Deleted = 0"
        );
    }
}