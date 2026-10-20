using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Dapper;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class StockReservationReconciliationRepository : IStockReservationReconciliationRepository {
    private const int AuditEditType = 1;

    private const int AuditNewPropertyType = 1;

    private const int SaleMovementType = 0;

    internal const string RowsSql = """
        SELECT [OrderItem].[ID] AS [OrderItemId],
               [OrderItem].[NetUID] AS [OrderItemNetUid],
               [OrderItem].[ProductID] AS [ProductId],
               [OrderItem].[Qty] AS [OrderItemQty],
               [OrderItem].[Deleted] AS [OrderItemDeleted],
               ISNULL((SELECT SUM([ProductReservation].[Qty])
                       FROM [dbo].[ProductReservation]
                       WHERE [ProductReservation].[OrderItemID] = [OrderItem].[ID]
                         AND [ProductReservation].[Deleted] = 0), 0) AS [ReservedQty],
               ISNULL((SELECT SUM([ConsignmentItemMovement].[Qty])
                       FROM [dbo].[ConsignmentItemMovement]
                       WHERE [ConsignmentItemMovement].[OrderItemID] = [OrderItem].[ID]
                         AND [ConsignmentItemMovement].[Deleted] = 0
                         AND [ConsignmentItemMovement].[IsIncomeMovement] = 0
                         AND [ConsignmentItemMovement].[MovementType] = @SaleMovementType), 0) AS [ShippedQty]
        FROM [dbo].[OrderItem]
        WHERE [OrderItem].[ID] IN (SELECT CAST([value] AS bigint) FROM OPENJSON(@OrderItemIdsJson))
        ORDER BY [OrderItem].[ID];
        """;

    internal const string OrderItemIdsSql = """
        SELECT [ID]
        FROM [dbo].[OrderItem]
        WHERE [OrderID] = @OrderId;
        """;

    internal const string InsertAuditSql = """
        INSERT INTO [dbo].[AuditEntity] ([Type], [EntityName], [BaseEntityNetUID], [UpdatedByNetUID], [UpdatedBy], [Updated])
        OUTPUT inserted.[ID]
        VALUES (@Type, @EntityName, @BaseEntityNetUid, @UpdatedByNetUid, @UpdatedBy, GETUTCDATE());
        """;

    internal const string InsertAuditPropertySql = """
        INSERT INTO [dbo].[AuditEntityProperty] ([Type], [Name], [Description], [Value], [AuditEntityID], [Updated])
        VALUES (@Type, @Name, @Description, @Value, @AuditEntityId, GETUTCDATE());
        """;

    private readonly IDbConnection _connection;

    public StockReservationReconciliationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<StockReservationReconciliationRow> GetRows(IEnumerable<long> orderItemIds) {
        long[] ids = (orderItemIds ?? Enumerable.Empty<long>()).Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0) return Array.Empty<StockReservationReconciliationRow>();

        return _connection.Query<StockReservationReconciliationRow>(
            RowsSql,
            new {
                OrderItemIdsJson = JsonSerializer.Serialize(ids),
                SaleMovementType
            }).ToList();
    }

    public IEnumerable<long> GetOrderItemIds(long orderId) {
        return orderId <= 0
            ? Array.Empty<long>()
            : _connection.Query<long>(OrderItemIdsSql, new { OrderId = orderId }).ToList();
    }

    public void RecordMismatch(StockReservationMismatch mismatch, string source, Guid operationNetUid, Guid principalNetUid) {
        ArgumentNullException.ThrowIfNull(mismatch?.Row);

        StockReservationReconciliationRow row = mismatch.Row;
        long auditId = _connection.QuerySingle<long>(
            InsertAuditSql,
            new {
                Type = AuditEditType,
                EntityName = StockReservationReconciliationPolicy.AuditEntityName,
                BaseEntityNetUid = row.OrderItemNetUid,
                UpdatedByNetUid = principalNetUid,
                UpdatedBy = source
            });

        (string Name, string Value)[] properties = {
            ("OrderItemId", row.OrderItemId.ToString(CultureInfo.InvariantCulture)),
            ("ProductId", row.ProductId.ToString(CultureInfo.InvariantCulture)),
            ("OrderItemQty", row.OrderItemQty.ToString("R", CultureInfo.InvariantCulture)),
            ("OrderItemDeleted", row.OrderItemDeleted ? "true" : "false"),
            ("ReservedQty", row.ReservedQty.ToString("R", CultureInfo.InvariantCulture)),
            ("ShippedQty", row.ShippedQty.ToString("R", CultureInfo.InvariantCulture)),
            ("Discrepancy", row.Discrepancy.ToString("R", CultureInfo.InvariantCulture)),
            ("PreviousDiscrepancy", mismatch.PreviousDiscrepancy?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty),
            ("IntroducedByOperation", mismatch.IntroducedByOperation ? "true" : "false"),
            ("OperationNetUid", operationNetUid.ToString("D")),
            ("Source", source ?? string.Empty)
        };

        _connection.Execute(
            InsertAuditPropertySql,
            properties.Select(property => new {
                Type = AuditNewPropertyType,
                property.Name,
                Description = StockReservationReconciliationPolicy.Describe(row),
                property.Value,
                AuditEntityId = auditId
            }));
    }
}
