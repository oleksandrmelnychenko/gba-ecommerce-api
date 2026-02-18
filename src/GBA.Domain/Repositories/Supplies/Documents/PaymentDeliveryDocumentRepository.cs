using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class PaymentDeliveryDocumentRepository : IPaymentDeliveryDocumentRepository {
    private readonly IDbConnection _connection;

    public PaymentDeliveryDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<PaymentDeliveryDocument> paymentDeliveryDocuments) {
        _connection.Execute(
            "INSERT INTO PaymentDeliveryDocument (ContentType, DocumentUrl, [FileName], GeneratedName, SupplyOrderPaymentDeliveryProtocolID, Updated) " +
            "VALUES(@ContentType, @DocumentUrl, @FileName, @GeneratedName, @SupplyOrderPaymentDeliveryProtocolID, getutcdate())",
            paymentDeliveryDocuments
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PaymentDeliveryDocument SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(IEnumerable<PaymentDeliveryDocument> paymentDeliveryDocuments) {
        _connection.Execute(
            "UPDATE PaymentDeliveryDocument " +
            "SET ContentType = @ContentType, DocumentUrl = @DocumentUrl, [FileName] = @FileName, GeneratedName = @GeneratedName, " +
            "SupplyOrderPaymentDeliveryProtocolID = @SupplyOrderPaymentDeliveryProtocolID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            paymentDeliveryDocuments
        );
    }
}