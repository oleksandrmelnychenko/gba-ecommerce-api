using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class DeliveryProductProtocolDocumentRepository : IDeliveryProductProtocolDocumentRepository {
    private readonly IDbConnection _connection;

    public DeliveryProductProtocolDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(DeliveryProductProtocolDocument document) {
        _connection.Execute(
            "INSERT INTO [DeliveryProductProtocolDocument] ([Updated], [Number], [DeliveryProductProtocolId], [DocumentUrl], [FileName], [ContentType], [GeneratedName]) " +
            "VALUES (getutcdate(), @Number, @DeliveryProductProtocolId, @DocumentUrl, @FileName, @ContentType, @GeneratedName); ",
            document);
    }
}