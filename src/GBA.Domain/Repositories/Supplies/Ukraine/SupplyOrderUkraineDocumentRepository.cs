using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkraineDocumentRepository : ISupplyOrderUkraineDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkraineDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void New(IEnumerable<SupplyOrderUkraineDocument> docs) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkraineDocument] ([Updated], [DocumentUrl], [FileName], [ContentType], [GeneratedName], [SupplyOrderUkraineID]) " +
            "VALUES (getutcdate(), @DocumentUrl, @FileName, @ContentType, @GeneratedName, @SupplyOrderUkraineId) ",
            docs);
    }

    public void Remove(IEnumerable<SupplyOrderUkraineDocument> docs) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineDocument] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [SupplyOrderUkraineDocument].[ID] = @Id; ",
            docs);
    }
}