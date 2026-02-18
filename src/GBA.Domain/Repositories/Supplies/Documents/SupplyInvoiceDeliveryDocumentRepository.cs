using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class SupplyInvoiceDeliveryDocumentRepository : ISupplyInvoiceDeliveryDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyInvoiceDeliveryDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public SupplyInvoiceDeliveryDocument GetLastRecord() {
        return _connection.Query<SupplyInvoiceDeliveryDocument>(
            "SELECT TOP 1 * FROM [SupplyInvoiceDeliveryDocument] " +
            "ORDER BY [Created] DESC; ").FirstOrDefault();
    }

    public void Remove(IEnumerable<SupplyInvoiceDeliveryDocument> documents) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceDeliveryDocument] " +
            "SET [Updated] = getutcdate() " +
            ",[Deleted] = 1 " +
            "WHERE [ID] = @Id; ",
            documents);
    }

    public void Add(SupplyInvoiceDeliveryDocument document) {
        _connection.Execute(
            "INSERT INTO [SupplyInvoiceDeliveryDocument](" +
            "[Updated] " +
            ",[SupplyInvoiceID] " +
            ",[SupplyDeliveryDocumentID] " +
            ",[DocumentUrl] " +
            ",[FileName] " +
            ",[GeneratedName] " +
            ",[ContentType] " +
            ",[Number]) " +
            "VALUES (" +
            "getutcdate()" +
            ",@SupplyInvoiceID " +
            ",@SupplyDeliveryDocumentID " +
            ",@DocumentUrl " +
            ",@FileName " +
            ",@GeneratedName " +
            ",@ContentType " +
            ",@Number " +
            ");",
            document);
    }

    public void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceDeliveryDocument] " +
            "SET SupplyInvoiceID = @ToInvoiceId, Updated = GETUTCDATE() " +
            "WHERE SupplyInvoiceID = @FromInvoiceId",
            new { FromInvoiceId = fromInvoiceId, ToInvoiceId = toInvoiceId }
        );
    }

    public void RemoveBySupplyInvoiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceDeliveryDocument] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [SupplyInvoiceDeliveryDocument].[SupplyInvoiceID] = @Id " +
            "AND [SupplyInvoiceDeliveryDocument].[ID] NOT IN @Ids; ",
            new { Id = id, Ids = ids });
    }
}