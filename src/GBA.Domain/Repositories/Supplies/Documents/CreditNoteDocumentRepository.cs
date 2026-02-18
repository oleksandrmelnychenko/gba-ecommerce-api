using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class CreditNoteDocumentRepository : ICreditNoteDocumentRepository {
    private readonly IDbConnection _connection;

    public CreditNoteDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CreditNoteDocument creditNoteDocument) {
        return _connection.Query<long>(
                "INSERT INTO CreditNoteDocument (ContentType, [FileName], Number, Comment, FromDate, Amount, SupplyOrderID, DocumentUrl, GeneratedName, Updated) " +
                "VALUES(@ContentType, @FileName, @Number, @Comment, @FromDate, @Amount, @SupplyOrderID, @DocumentUrl, @GeneratedName, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                creditNoteDocument
            )
            .Single();
    }

    public CreditNoteDocument GetByNetId(Guid netId) {
        return _connection.Query<CreditNoteDocument>(
                "SELECT * FROM CreditNoteDocument WHERE NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Update(CreditNoteDocument creditNoteDocument) {
        _connection.Execute(
            "UPDATE CreditNoteDocument " +
            "SET Number = @Number, Comment = @Comment, FromDate = @FromDate, Amount = @Amount, ContentType = @ContentType, DocumentUrl = @DocumentUrl, [FileName] = @FileName, GeneratedName = @GeneratedName, SupplyOrderID = @SupplyOrderID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            creditNoteDocument
        );
    }
}