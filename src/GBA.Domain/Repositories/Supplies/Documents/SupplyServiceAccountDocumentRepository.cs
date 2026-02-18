using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class SupplyServiceAccountDocumentRepository : ISupplyServiceAccountDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyServiceAccountDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long New(SupplyServiceAccountDocument document) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyServiceAccountDocument] ([Updated], [DocumentUrl], [FileName], [ContentType], [GeneratedName], [Number]) " +
            "VALUES (getutcdate(), @DocumentUrl, @FileName, @ContentType, @GeneratedName, @Number); " +
            "SELECT SCOPE_IDENTITY(); ", document).SingleOrDefault();
    }

    public void Update(SupplyServiceAccountDocument document) {
        _connection.Execute(
            "UPDATE [SupplyServiceAccountDocument] " +
            "SET [Updated] = getutcdate() " +
            ", [DocumentUrl] = @DocumentUrl " +
            ", [FileName] = @FileName " +
            ", [ContentType] = @ContentType " +
            ", [GeneratedName] = @GeneratedName " +
            ", [Number] = @Number " +
            "WHERE [SupplyServiceAccountDocument].[ID] = @Id; ",
            document);
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [SupplyServiceAccountDocument] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [SupplyServiceAccountDocument].[ID] = @Id; ",
            new { Id = id });
    }

    public SupplyServiceAccountDocument GetLastRecord() {
        return _connection.Query<SupplyServiceAccountDocument>(
                "SELECT TOP(1) [SupplyServiceAccountDocument].* " +
                "FROM [SupplyServiceAccountDocument] " +
                "ORDER BY [SupplyServiceAccountDocument].ID DESC"
            )
            .SingleOrDefault();
    }
}