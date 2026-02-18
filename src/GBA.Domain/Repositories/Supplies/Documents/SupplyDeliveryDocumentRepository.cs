using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class SupplyDeliveryDocumentRepository : ISupplyDeliveryDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyDeliveryDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyDeliveryDocument document) {
        return _connection.Query<long>(
                "INSERT INTO SupplyDeliveryDocument ([Name], TransportationType, Updated) VALUES(@Name, @TransportationType, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                document
            )
            .Single();
    }

    public List<SupplyDeliveryDocument> GetAll() {
        return _connection.Query<SupplyDeliveryDocument>(
                "SELECT * FROM SupplyDeliveryDocument " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public List<string> GetAllNamesGrouped() {
        return _connection.Query<string>(
                "SELECT [SupplyDeliveryDocument].Name " +
                "FROM [SupplyDeliveryDocument] " +
                "WHERE [SupplyDeliveryDocument].Deleted = 0 " +
                "GROUP BY [SupplyDeliveryDocument].Name "
            )
            .ToList();
    }

    public List<SupplyDeliveryDocument> GetAllByType(SupplyTransportationType type) {
        return _connection.Query<SupplyDeliveryDocument>(
                "SELECT * FROM SupplyDeliveryDocument " +
                "WHERE TransportationType = @Type " +
                "AND Deleted = 0",
                new { Type = type }
            )
            .ToList();
    }

    public SupplyDeliveryDocument GetById(long id) {
        return _connection.Query<SupplyDeliveryDocument>(
                "SELECT * FROM SupplyDeliveryDocument WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SupplyDeliveryDocument GetByNetId(Guid netId) {
        return _connection.Query<SupplyDeliveryDocument>(
                "SELECT * FROM SupplyDeliveryDocument WHERE NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE SupplyDeliveryDocument SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(SupplyDeliveryDocument document) {
        _connection.Execute(
            "UPDATE SupplyDeliveryDocument SET [Name] = @Name, TransportationType = @TransportationType, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            document
        );
    }

    public SupplyDeliveryDocument GetForInvoiceByTransportationType(SupplyTransportationType type) {
        return _connection.Query<SupplyDeliveryDocument>(
            "SELECT TOP 1 * FROM [SupplyDeliveryDocument] " +
            "WHERE [Name] = 'Invoice' " +
            "AND [TransportationType] = @Type " +
            "AND [Deleted] = 0 " +
            "ORDER BY [Created] DESC ",
            new { Type = type }).FirstOrDefault();
    }
}