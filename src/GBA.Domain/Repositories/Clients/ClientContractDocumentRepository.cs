using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Clients.Documents;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientContractDocumentRepository : IClientContractDocumentRepository {
    private readonly IDbConnection _connection;

    public ClientContractDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ClientContractDocument> documents) {
        _connection.Execute(
            "INSERT INTO ClientContractDocument (ClientID, [FileName], ContentType, Updated) " +
            "VALUES(@ClientID, @FileName, @ContentType, getutcdate())",
            documents
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ClientContractDocument SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Remove(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE ClientContractDocument SET Deleted = 1 WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void Update(IEnumerable<ClientContractDocument> documents) {
        _connection.Execute(
            "UPDATE ClientContractDocument " +
            "SET ClientID = @ClientID, ContentType = @ContentType, DocumentUrl = @DocumentUrl, FileName = @FileName, GeneratedName = @GeneratedName, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            documents
        );
    }
}