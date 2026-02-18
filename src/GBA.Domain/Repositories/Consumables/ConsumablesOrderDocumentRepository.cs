using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Consumables.Orders;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumablesOrderDocumentRepository : IConsumablesOrderDocumentRepository {
    private readonly IDbConnection _connection;

    public ConsumablesOrderDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ConsumablesOrderDocument> consumablesOrderDocuments) {
        _connection.Execute(
            "INSERT INTO [ConsumablesOrderDocument] (ConsumablesOrderId, DocumentUrl, ContentType, FileName, GeneratedName, Updated) " +
            "VALUES (@ConsumablesOrderId, @DocumentUrl, @ContentType, @FileName, @GeneratedName, GETUTCDATE())",
            consumablesOrderDocuments
        );
    }

    public void Update(IEnumerable<ConsumablesOrderDocument> consumablesOrderDocuments) {
        _connection.Execute(
            "UPDATE [ConsumablesOrderDocument] " +
            "SET ConsumablesOrderId = @ConsumablesOrderId, DocumentUrl = @DocumentUrl, ContentType = @ContentType, GeneratedName = @GeneratedName, " +
            "FileName = @FileName, Updated = GETUTCDATE() " +
            "WHERE [ConsumablesOrderDocument].ID = @Id",
            consumablesOrderDocuments
        );
    }

    public void Remove(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ConsumablesOrderDocument] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ConsumablesOrderDocument].ID IN @Ids",
            new { Ids = ids }
        );
    }
}