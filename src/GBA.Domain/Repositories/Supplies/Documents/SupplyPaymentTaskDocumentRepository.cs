using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class SupplyPaymentTaskDocumentRepository : ISupplyPaymentTaskDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyPaymentTaskDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(SupplyPaymentTaskDocument supplyPaymentTaskDocument) {
        _connection.Execute(
            "INSERT INTO [SupplyPaymentTaskDocument] (SupplyPaymentTaskId, DocumentUrl, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES (@SupplyPaymentTaskId, @DocumentUrl, @FileName, @ContentType, @GeneratedName, GETUTCDATE())",
            supplyPaymentTaskDocument
        );
    }

    public void Add(IEnumerable<SupplyPaymentTaskDocument> supplyPaymentTaskDocuments) {
        _connection.Execute(
            "INSERT INTO [SupplyPaymentTaskDocument] (SupplyPaymentTaskId, DocumentUrl, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES (@SupplyPaymentTaskId, @DocumentUrl, @FileName, @ContentType, @GeneratedName, GETUTCDATE())",
            supplyPaymentTaskDocuments
        );
    }

    public List<SupplyPaymentTaskDocument> GetAllByTaskId(long id) {
        return _connection.Query<SupplyPaymentTaskDocument>(
                "SELECT *" +
                "FROM [SupplyPaymentTaskDocument] " +
                "WHERE [SupplyPaymentTaskDocument].Deleted = 0 " +
                "AND [SupplyPaymentTaskDocument].SupplyPaymentTaskID = @Id",
                new { Id = id }
            )
            .ToList();
    }

    public void RemoveBySupplyPaymentTaskId(long id) {
        _connection.Execute(
            "UPDATE [SupplyPaymentTaskDocument] " +
            "SET [Updated] = getutcdate() " +
            ",[Deleted] = 1 " +
            "WHERE [SupplyPaymentTaskID] = @Id; ",
            new { Id = id });
    }

    public void Remove(IEnumerable<SupplyPaymentTaskDocument> documents) {
        _connection.Execute(
            "UPDATE [SupplyPaymentTaskDocument] " +
            "SET [Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [ID] = @Id",
            documents);
    }
}