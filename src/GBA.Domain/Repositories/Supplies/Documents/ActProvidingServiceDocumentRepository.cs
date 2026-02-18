using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class ActProvidingServiceDocumentRepository : IActProvidingServiceDocumentRepository {
    private readonly IDbConnection _connection;

    public ActProvidingServiceDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long New(ActProvidingServiceDocument document) {
        return _connection.Query<long>(
            "INSERT INTO [ActProvidingServiceDocument] ([Updated], [DocumentUrl], [FileName], [ContentType], [GeneratedName], [Number]) " +
            "VALUES (getutcdate(), @DocumentUrl, @FileName, @ContentType, @GeneratedName, @Number); " +
            "SELECT SCOPE_IDENTITY(); ", document).SingleOrDefault();
    }

    public void Update(ActProvidingServiceDocument document) {
        _connection.Execute(
            "UPDATE [ActProvidingServiceDocument] " +
            "SET [Updated] = getutcdate() " +
            ", [DocumentUrl] = @DocumentUrl " +
            ", [FileName] = @FileName " +
            ", [ContentType] = @ContentType " +
            ", [GeneratedName] = @GeneratedName " +
            ", [Number] = @Number " +
            "WHERE [ActProvidingServiceDocument].[ID] = @Id; ",
            document);
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [ActProvidingServiceDocument] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [ActProvidingServiceDocument].[ID] = @Id; ",
            new { Id = id });
    }

    public ActProvidingServiceDocument GetLastRecord() {
        return _connection.Query<ActProvidingServiceDocument>(
                "SELECT TOP(1) [ActProvidingServiceDocument].* " +
                "FROM [ActProvidingServiceDocument] " +
                "ORDER BY [ActProvidingServiceDocument].ID DESC"
            )
            .SingleOrDefault();
    }
}