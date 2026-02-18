using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class ProFormDocumentRepository : IProFormDocumentRepository {
    private readonly IDbConnection _connection;

    public ProFormDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ProFormDocument> proFormDocuments) {
        _connection.Execute(
            "INSERT INTO ProFormDocument (DocumentUrl, SupplyProFormID, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES(@DocumentUrl, @SupplyProFormID, @FileName, @ContentType, @GeneratedName, getutcdate())",
            proFormDocuments
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ProFormDocument SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void RemoveAll(Guid supplyProFormNetId) {
        _connection.Execute(
            "UPDATE ProFormDocument SET Deleted = 1 " +
            "WHERE SupplyProFormID = (SELECT ID FROM SupplyProForm WHERE NetUID = @SupplyProFormNetId)",
            new { SupplyProFormNetId = supplyProFormNetId }
        );
    }

    public void Update(IEnumerable<ProFormDocument> proFormDocuments) {
        _connection.Execute(
            "UPDATE ProFormDocument " +
            "SET DocumentUrl = @DocumentUrl, SupplyProFormID = @SupplyProFormID, FileName = @FileName, ContentType = @ContentType, GeneratedName = @GeneratedName, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            proFormDocuments
        );
    }

    public void RemoveAllByProFormId(long id) {
        _connection.Execute(
            "UPDATE [ProFormDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ProFormDocument].SupplyProFormID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByProFormIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [ProFormDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ProFormDocument].SupplyProFormID = @Id " +
            "AND [ProFormDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }
}