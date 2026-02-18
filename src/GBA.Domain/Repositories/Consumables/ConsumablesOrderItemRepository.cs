using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumablesOrderItemRepository : IConsumablesOrderItemRepository {
    private readonly IDbConnection _connection;

    public ConsumablesOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ConsumablesOrderItem consumablesOrderItem) {
        return _connection.Query<long>(
                "INSERT INTO [ConsumablesOrderItem] " +
                "(TotalPrice, PricePerItem, Qty, ConsumableProductCategoryId, ConsumablesOrderId, ConsumableProductId, ConsumableProductOrganizationId, " +
                "VAT, VatPercent, IsService, SupplyOrganizationAgreementId, Updated, TotalPriceWithVAT) " +
                "VALUES (@TotalPrice, @PricePerItem, @Qty, @ConsumableProductCategoryId, @ConsumablesOrderId, @ConsumableProductId, @ConsumableProductOrganizationId, " +
                "@VAT, @VatPercent, @IsService, @SupplyOrganizationAgreementId, getutcdate(), @TotalPriceWithVAT); " +
                "SELECT SCOPE_IDENTITY()",
                consumablesOrderItem
            )
            .Single();
    }

    public void Add(IEnumerable<ConsumablesOrderItem> consumablesOrderItems) {
        _connection.Execute(
            "INSERT INTO [ConsumablesOrderItem] " +
            "(TotalPrice, PricePerItem, Qty, ConsumableProductCategoryId, ConsumablesOrderId, ConsumableProductId, ConsumableProductOrganizationId, " +
            "VAT, VatPercent, IsService, SupplyOrganizationAgreementId, Updated, TotalPriceWithVAT) " +
            "VALUES (@TotalPrice, @PricePerItem, @Qty, @ConsumableProductCategoryId, @ConsumablesOrderId, @ConsumableProductId, @ConsumableProductOrganizationId, " +
            "@VAT, @VatPercent, @IsService, @SupplyOrganizationAgreementId, getutcdate(), @TotalPriceWithVAT)",
            consumablesOrderItems
        );
    }

    public void Update(IEnumerable<ConsumablesOrderItem> consumablesOrderItems) {
        _connection.Execute(
            "UPDATE [ConsumablesOrderItem] " +
            "SET TotalPrice = @TotalPrice, PricePerItem = @PricePerItem, Qty = @Qty, ConsumableProductCategoryId = @ConsumableProductCategoryId, " +
            "ConsumablesOrderId = @ConsumablesOrderId, ConsumableProductId = @ConsumableProductId, ConsumableProductOrganizationId = @ConsumableProductOrganizationId, " +
            "VAT = @VAT, VatPercent = @VatPercent, IsService = @IsService, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, Updated = getutcdate(), TotalPriceWithVAT = @TotalPriceWithVAT " +
            "WHERE [ConsumablesOrderItem].NetUID = @NetUid",
            consumablesOrderItems
        );
    }

    public void Remove(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ConsumablesOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ConsumablesOrderItem].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllExceptProvided(long orderId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ConsumablesOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ConsumablesOrderItem].ConsumablesOrderId = @OrderId " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "AND [ConsumablesOrderItem].ID NOT IN @Ids",
            new { OrderId = orderId, Ids = ids }
        );
    }

    public ConsumablesOrderItem GetByIdWithCalculatedUnDepreciatedQty(long id, long storageId) {
        return _connection.Query<ConsumablesOrderItem>(
                "SELECT [ConsumablesOrderItem].[ID] " +
                ",[ConsumablesOrderItem].[ConsumableProductCategoryID] " +
                ",[ConsumablesOrderItem].[ConsumableProductID] " +
                ",[ConsumablesOrderItem].[ConsumableProductOrganizationID] " +
                ",[ConsumablesOrderItem].[ConsumablesOrderID] " +
                ",[ConsumablesOrderItem].[Created] " +
                ",[ConsumablesOrderItem].[Deleted] " +
                ",[ConsumablesOrderItem].[NetUID] " +
                ",[ConsumablesOrderItem].[TotalPrice] " +
                ",( " +
                "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
                "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
                "LEFT JOIN [DepreciatedConsumableOrderItem] " +
                "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
                "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
                ") AS [Qty] " +
                ",[ConsumablesOrderItem].[Updated] " +
                ",[ConsumablesOrderItem].[PricePerItem] " +
                ",[ConsumablesOrderItem].[VAT] " +
                ",[ConsumablesOrderItem].[VatPercent] " +
                ",[ConsumablesOrderItem].[IsService] " +
                "FROM [ConsumablesOrderItem] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "WHERE [ConsumablesOrderItem].ID = @Id " +
                "AND [ConsumablesStorage].ID = @StorageId",
                new { Id = id, StorageId = storageId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIds(long productId, long storageId) {
        return _connection.Query<ConsumablesOrderItem>(
            "SELECT [ConsumablesOrderItem].[ID] " +
            ",[ConsumablesOrderItem].[ConsumableProductCategoryID] " +
            ",[ConsumablesOrderItem].[ConsumableProductID] " +
            ",[ConsumablesOrderItem].[ConsumableProductOrganizationID] " +
            ",[ConsumablesOrderItem].[ConsumablesOrderID] " +
            ",[ConsumablesOrderItem].[Created] " +
            ",[ConsumablesOrderItem].[Deleted] " +
            ",[ConsumablesOrderItem].[NetUID] " +
            ",[ConsumablesOrderItem].[TotalPrice] " +
            ",( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") AS [Qty] " +
            ",[ConsumablesOrderItem].[Updated] " +
            ",[ConsumablesOrderItem].[PricePerItem] " +
            ",[ConsumablesOrderItem].[VAT] " +
            ",[ConsumablesOrderItem].[VatPercent] " +
            ",[ConsumablesOrderItem].[IsService] " +
            "FROM [ConsumablesOrderItem] " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "WHERE ( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") > 0 " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "AND [ConsumablesOrderItem].ConsumableProductID = @ProductId " +
            "AND [ConsumablesStorage].ID = @StorageId",
            new { ProductId = productId, StorageId = storageId }
        );
    }

    public IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIdsMostExpensiveFirst(long productId, long storageId) {
        return _connection.Query<ConsumablesOrderItem>(
            "SELECT [ConsumablesOrderItem].[ID] " +
            ",[ConsumablesOrderItem].[ConsumableProductCategoryID] " +
            ",[ConsumablesOrderItem].[ConsumableProductID] " +
            ",[ConsumablesOrderItem].[ConsumableProductOrganizationID] " +
            ",[ConsumablesOrderItem].[ConsumablesOrderID] " +
            ",[ConsumablesOrderItem].[Created] " +
            ",[ConsumablesOrderItem].[Deleted] " +
            ",[ConsumablesOrderItem].[NetUID] " +
            ",[ConsumablesOrderItem].[TotalPrice] " +
            ",( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") AS [Qty] " +
            ",[ConsumablesOrderItem].[Updated] " +
            ",[ConsumablesOrderItem].[PricePerItem] " +
            ",[ConsumablesOrderItem].[VAT] " +
            ",[ConsumablesOrderItem].[VatPercent] " +
            ",[ConsumablesOrderItem].[IsService] " +
            "FROM [ConsumablesOrderItem] " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "WHERE ( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") > 0 " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "AND [ConsumablesOrderItem].ConsumableProductID = @ProductId " +
            "AND [ConsumablesStorage].ID = @StorageId " +
            "ORDER BY [ConsumablesOrderItem].PricePerItem DESC " +
            ", [ConsumablesOrderItem].ID",
            new { ProductId = productId, StorageId = storageId }
        );
    }

    public IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemId(long productId, long storageId, long itemId) {
        return _connection.Query<ConsumablesOrderItem>(
            "SELECT [ConsumablesOrderItem].[ID] " +
            ",[ConsumablesOrderItem].[ConsumableProductCategoryID] " +
            ",[ConsumablesOrderItem].[ConsumableProductID] " +
            ",[ConsumablesOrderItem].[ConsumableProductOrganizationID] " +
            ",[ConsumablesOrderItem].[ConsumablesOrderID] " +
            ",[ConsumablesOrderItem].[Created] " +
            ",[ConsumablesOrderItem].[Deleted] " +
            ",[ConsumablesOrderItem].[NetUID] " +
            ",[ConsumablesOrderItem].[TotalPrice] " +
            ",( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") AS [Qty] " +
            ",[ConsumablesOrderItem].[Updated] " +
            ",[ConsumablesOrderItem].[PricePerItem] " +
            ",[ConsumablesOrderItem].[VAT] " +
            ",[ConsumablesOrderItem].[VatPercent] " +
            ",[ConsumablesOrderItem].[IsService] " +
            "FROM [ConsumablesOrderItem] " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "WHERE ( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") > 0 " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "AND [ConsumablesOrderItem].ConsumableProductID = @ProductId " +
            "AND [ConsumablesOrderItem].ID <> @ItemId " +
            "AND [ConsumablesStorage].ID = @StorageId",
            new { ProductId = productId, ItemId = itemId, StorageId = storageId }
        );
    }

    public IEnumerable<ConsumablesOrderItem> GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemIdMostExpensiveFirst(long productId, long storageId, long itemId) {
        return _connection.Query<ConsumablesOrderItem>(
            "SELECT [ConsumablesOrderItem].[ID] " +
            ",[ConsumablesOrderItem].[ConsumableProductCategoryID] " +
            ",[ConsumablesOrderItem].[ConsumableProductID] " +
            ",[ConsumablesOrderItem].[ConsumableProductOrganizationID] " +
            ",[ConsumablesOrderItem].[ConsumablesOrderID] " +
            ",[ConsumablesOrderItem].[Created] " +
            ",[ConsumablesOrderItem].[Deleted] " +
            ",[ConsumablesOrderItem].[NetUID] " +
            ",[ConsumablesOrderItem].[TotalPrice] " +
            ",( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") AS [Qty] " +
            ",[ConsumablesOrderItem].[Updated] " +
            ",[ConsumablesOrderItem].[PricePerItem] " +
            ",[ConsumablesOrderItem].[VAT] " +
            ",[ConsumablesOrderItem].[VatPercent] " +
            ",[ConsumablesOrderItem].[IsService] " +
            "FROM [ConsumablesOrderItem] " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "WHERE ( " +
            "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
            "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
            "LEFT JOIN [DepreciatedConsumableOrderItem] " +
            "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
            "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
            ") > 0 " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "AND [ConsumablesOrderItem].ConsumableProductID = @ProductId " +
            "AND [ConsumablesOrderItem].ID <> @ItemId " +
            "AND [ConsumablesStorage].ID = @StorageId " +
            "ORDER BY [ConsumablesOrderItem].PricePerItem DESC " +
            ", [ConsumablesOrderItem].ID",
            new { ProductId = productId, ItemId = itemId, StorageId = storageId }
        );
    }
}