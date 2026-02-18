using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class BillOfLadingDocumentRepository : IBillOfLadingDocumentRepository {
    private readonly IDbConnection _connection;

    public BillOfLadingDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(BillOfLadingDocument billOfLadingDocument) {
        return _connection.Query<long>(
                "INSERT INTO BillOfLadingDocument ([Number], [Amount], [Date], GeneratedName, [FileName], ContentType,  DocumentUrl, Updated, [BillOfLadingServiceID]) " +
                "VALUES(@Number, @Amount, @Date, @GeneratedName, @FileName, @ContentType,  @DocumentUrl, getutcdate(), @BillOfLadingServiceId); " +
                "SELECT SCOPE_IDENTITY()",
                billOfLadingDocument
            )
            .Single();
    }

    public void Add(IEnumerable<BillOfLadingDocument> billOfLadingDocument) {
        _connection.Query(
            "INSERT INTO BillOfLadingDocument ([Number], [Amount], [Date], GeneratedName, [FileName], ContentType,  DocumentUrl, Updated, [BillOfLadingServiceID]) " +
            "VALUES(@Number, @Amount, @Date, @GeneratedName, @FileName, @ContentType,  @DocumentUrl, getutcdate(), @BillOfLadingServiceId); ",
            billOfLadingDocument
        );
    }

    public void Remove(IEnumerable<BillOfLadingDocument> billOfLadingDocuments) {
        _connection.Execute(
            "UPDATE [BillOfLadingDocument] " +
            "SET [Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [ID] = @Id",
            billOfLadingDocuments);
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE BillOfLadingDocument SET Deleted = 1 WHERE NetUID = NetId",
            new { NetId = netId }
        );
    }

    public void Update(BillOfLadingDocument billOfLadingDocument) {
        _connection.Execute(
            "UPDATE BillOfLadingDocument " +
            "SET Number = @Number, Amount = @Amount, GeneratedName = @GeneratedName, " +
            "[FileName] = @FileName, ContentType = @ContentType,  DocumentUrl = @DocumentUrl, Updated = getutcdate(), " +
            "[BillOfLadingServiceID] = @BillOfLadingServiceId " +
            "WHERE NetUID = @NetUID",
            billOfLadingDocument
        );
    }
}