using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SadDocumentRepository : ISadDocumentRepository {
    private readonly IDbConnection _connection;

    public SadDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<SadDocument> documents) {
        _connection.Execute(
            "INSERT INTO [SadDocument] (SadId, DocumentUrl, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES (@SadId, @DocumentUrl, @FileName, @ContentType, @GeneratedName, GETUTCDATE())",
            documents
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [SadDocument] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }
}