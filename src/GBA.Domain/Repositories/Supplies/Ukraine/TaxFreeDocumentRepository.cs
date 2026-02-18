using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class TaxFreeDocumentRepository : ITaxFreeDocumentRepository {
    private readonly IDbConnection _connection;

    public TaxFreeDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<TaxFreeDocument> documents) {
        _connection.Execute(
            "INSERT INTO [TaxFreeDocument] (TaxFreeId, DocumentUrl, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES (@TaxFreeId, @DocumentUrl, @FileName, @ContentType, @GeneratedName, GETUTCDATE())",
            documents
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [TaxFreeDocument] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }
}