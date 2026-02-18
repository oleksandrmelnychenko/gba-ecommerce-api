using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class PackingListDocumentRepository : IPackingListDocumentRepository {
    private readonly IDbConnection _connection;

    public PackingListDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<PackingListDocument> packingListDocuments) {
        _connection.Execute(
            "INSERT INTO PackingListDocument (DocumentUrl, SupplyOrderID, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES(@DocumentUrl, @SupplyOrderID, @FileName, @ContentType, @GeneratedName, getutcdate())",
            packingListDocuments
        );
    }

    public IEnumerable<PackingListDocument> GetAllBySupplyOrderNetId(Guid supplyOrderNetId) {
        return _connection.Query<PackingListDocument>(
                "SELECT * FROM PackingListDocument " +
                "WHERE SupplyOrderID = (SELECT ID FROM SupplyOrder WHERE NetUID = @SupplyOrderNetId)",
                new { SupplyOrderNetId = supplyOrderNetId }
            )
            .ToList();
    }
}