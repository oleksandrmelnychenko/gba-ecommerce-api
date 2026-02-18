using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Documents;

public sealed class SupplyOrderDeliveryDocumentRepository : ISupplyOrderDeliveryDocumentRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderDeliveryDocumentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<SupplyOrderDeliveryDocument> supplyOrderDeliveryDocuments) {
        _connection.Execute(
            "INSERT INTO SupplyOrderDeliveryDocument " +
            "(Comment, IsReceived, ProcessedDate, SupplyDeliveryDocumentID, SupplyOrderID, UserID, IsProcessed, DocumentUrl, FileName, ContentType, GeneratedName, Updated) " +
            "VALUES " +
            "(@Comment, @IsReceived, @ProcessedDate, @SupplyDeliveryDocumentID, @SupplyOrderID, @UserID, @IsProcessed, @DocumentUrl, @FileName, @ContentType, " +
            "@GeneratedName, getutcdate())",
            supplyOrderDeliveryDocuments
        );
    }

    public void Update(IEnumerable<SupplyOrderDeliveryDocument> supplyOrderDeliveryDocuments) {
        _connection.Execute(
            "UPDATE SupplyOrderDeliveryDocument " +
            "SET Comment = @Comment, IsReceived = @IsReceived, ProcessedDate = @ProcessedDate, " +
            "SupplyDeliveryDocumentID = @SupplyDeliveryDocumentID, SupplyOrderID = @SupplyOrderID, UserID = @UserID, IsProcessed = @IsProcessed, IsNotified = @IsNotified, " +
            "DocumentUrl = @DocumentUrl, FileName = @FileName, ContentType = @ContentType, GeneratedName = @GeneratedName, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            supplyOrderDeliveryDocuments
        );
    }

    public void UpdateDocumentData(SupplyOrderDeliveryDocument supplyOrderDeliveryDocument) {
        _connection.Execute(
            "UPDATE SupplyOrderDeliveryDocument " +
            "SET DocumentUrl = @DocumentUrl, FileName = @FileName, ContentType = @ContentType, GeneratedName = @GeneratedName, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            supplyOrderDeliveryDocument
        );
    }

    public void RemoveAllBySupplyOrderId(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrderDeliveryDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrderDeliveryDocument].SupplyOrderID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllBySupplyOrderIdExceptProvided(long id, IEnumerable<long> notRemoveIds) {
        _connection.Execute(
            "UPDATE [SupplyOrderDeliveryDocument] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrderDeliveryDocument].SupplyOrderID = @Id " +
            "AND [SupplyOrderDeliveryDocument].ID NOT IN @NotRemoveIds",
            new { Id = id, NotRemoveIds = notRemoveIds }
        );
    }

    public List<SupplyOrderDeliveryDocument> GetAllFromSearch(string documentType, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<SupplyOrderDeliveryDocument> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [SupplyOrderDeliveryDocument].ID DESC) AS RowNumber " +
            ", [SupplyOrderDeliveryDocument].ID " +
            "FROM [SupplyOrderDeliveryDocument] " +
            "LEFT JOIN [SupplyDeliveryDocument] " +
            "ON [SupplyDeliveryDocument].ID = [SupplyOrderDeliveryDocument].SupplyDeliveryDocumentID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyOrderDeliveryDocument].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [SupplyOrderDeliveryDocument].Deleted = 0 " +
            "AND [SupplyOrderDeliveryDocument].Created >= @From " +
            "AND [SupplyOrderDeliveryDocument].Created <= @To " +
            "AND [SupplyDeliveryDocument].Name = @DocumentType ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") " +
            "SELECT * " +
            "FROM [SupplyOrderDeliveryDocument] " +
            "LEFT JOIN [SupplyDeliveryDocument] " +
            "ON [SupplyDeliveryDocument].ID = [SupplyOrderDeliveryDocument].SupplyDeliveryDocumentID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyOrderDeliveryDocument].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "WHERE [SupplyOrderDeliveryDocument].ID IN (" +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(SupplyOrderDeliveryDocument),
            typeof(SupplyDeliveryDocument),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber)
        };

        var props = new { DocumentType = documentType, Limit = limit, Offset = offset, From = from, To = to, ClientNetId = clientNetId };

        TimeZoneInfo currentTimeZone = null;

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk"))
            currentTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
        else
            currentTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        Func<object[], SupplyOrderDeliveryDocument> mapper = objects => {
            SupplyOrderDeliveryDocument supplyOrderDeliveryDocument = (SupplyOrderDeliveryDocument)objects[0];
            SupplyDeliveryDocument supplyDeliveryDocument = (SupplyDeliveryDocument)objects[1];
            SupplyOrder supplyOrder = (SupplyOrder)objects[2];
            Client client = (Client)objects[3];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[4];

            if (!toReturn.Any(o => o.Id.Equals(supplyOrderDeliveryDocument.Id))) {
                supplyOrder.Client = client;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                supplyOrderDeliveryDocument.SupplyDeliveryDocument = supplyDeliveryDocument;
                supplyOrderDeliveryDocument.SupplyOrder = supplyOrder;

//                    supplyOrderDeliveryDocument.Created = TimeZoneInfo.ConvertTimeFromUtc(supplyOrderDeliveryDocument.Created, currentTimeZone);

                toReturn.Add(supplyOrderDeliveryDocument);
            }

            return supplyOrderDeliveryDocument;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }
}