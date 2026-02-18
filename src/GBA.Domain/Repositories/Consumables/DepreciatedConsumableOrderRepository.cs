using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class DepreciatedConsumableOrderRepository : IDepreciatedConsumableOrderRepository {
    private readonly IDbConnection _connection;

    public DepreciatedConsumableOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DepreciatedConsumableOrder depreciatedConsumableOrder) {
        return _connection.Query<long>(
                "INSERT INTO [DepreciatedConsumableOrder] " +
                "(Number, Comment, CreatedById, DepreciatedToId, CommissionHeadId, ConsumablesStorageId, Updated) " +
                "VALUES (@Number, @Comment, @CreatedById, @DepreciatedToId, @CommissionHeadId, @ConsumablesStorageId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                depreciatedConsumableOrder
            )
            .Single();
    }

    public void Update(DepreciatedConsumableOrder depreciatedConsumableOrder) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrder] " +
            "SET Number = @Number, Comment = @Comment, CreatedById = @CreatedById, DepreciatedToId = @DepreciatedToId, " +
            "CommissionHeadId = @CommissionHeadId, ConsumablesStorageId = @ConsumablesStorageId, UpdatedById = @UpdatedById, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrder].ID = @Id",
            depreciatedConsumableOrder
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrder] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrder].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [DepreciatedConsumableOrder] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [DepreciatedConsumableOrder].ID = @Id",
            new { Id = id }
        );
    }

    public DepreciatedConsumableOrder GetLastRecord() {
        return _connection.Query<DepreciatedConsumableOrder>(
                "SELECT TOP(1) * " +
                "FROM [DepreciatedConsumableOrder] " +
                "WHERE [DepreciatedConsumableOrder].Deleted = 0 " +
                "ORDER BY [DepreciatedConsumableOrder].ID DESC"
            )
            .SingleOrDefault();
    }

    public DepreciatedConsumableOrder GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<DepreciatedConsumableOrder>(
                "SELECT * " +
                "FROM [DepreciatedConsumableOrder] " +
                "WHERE [DepreciatedConsumableOrder].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public DepreciatedConsumableOrder GetById(long id) {
        Type[] types = {
            typeof(DepreciatedConsumableOrder),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(ConsumablesStorage)
        };

        Func<object[], DepreciatedConsumableOrder> mapper = objects => {
            DepreciatedConsumableOrder depreciatedConsumableOrder = (DepreciatedConsumableOrder)objects[0];
            User createdBy = (User)objects[1];
            User updatedBy = (User)objects[2];
            User depreciatedTo = (User)objects[3];
            User commissionHead = (User)objects[4];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[5];

            depreciatedConsumableOrder.CreatedBy = createdBy;
            depreciatedConsumableOrder.UpdatedBy = updatedBy;
            depreciatedConsumableOrder.DepreciatedTo = depreciatedTo;
            depreciatedConsumableOrder.CommissionHead = commissionHead;
            depreciatedConsumableOrder.ConsumablesStorage = consumablesStorage;

            return depreciatedConsumableOrder;
        };

        DepreciatedConsumableOrder toReturn = _connection.Query(
                "SELECT * " +
                "FROM [DepreciatedConsumableOrder] " +
                "LEFT JOIN [User] AS [CreatedBy] " +
                "ON [CreatedBy].ID = [DepreciatedConsumableOrder].CreatedByID " +
                "LEFT JOIN [User] AS [UpdatedBy] " +
                "ON [UpdatedBy].ID = [DepreciatedConsumableOrder].UpdatedByID " +
                "LEFT JOIN [User] AS [DepreciatedTo] " +
                "ON [DepreciatedTo].ID = [DepreciatedConsumableOrder].DepreciatedToID " +
                "LEFT JOIN [User] AS [CommissionHead] " +
                "ON [CommissionHead].ID = [DepreciatedConsumableOrder].CommissionHeadID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [DepreciatedConsumableOrder].ConsumablesStorageID " +
                "WHERE [DepreciatedConsumableOrder].ID = @Id",
                types,
                mapper,
                new { Id = id }
            )
            .SingleOrDefault();

        if (toReturn != null) {
            Type[] itemsTypes = {
                typeof(DepreciatedConsumableOrderItem),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumablesOrder),
                typeof(User),
                typeof(ConsumableProduct),
                typeof(MeasureUnit),
                typeof(SupplyOrganization),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement)
            };

            Func<object[], DepreciatedConsumableOrderItem> itemsMapper = objects => {
                DepreciatedConsumableOrderItem depreciatedConsumableOrderItem = (DepreciatedConsumableOrderItem)objects[0];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[1];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[2];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[3];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[4];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[7];
                MeasureUnit consumableProductMeasureUnit = (MeasureUnit)objects[8];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[9];
                PaymentCostMovementOperation consumablesOrderItemPaymentCostMovementOperation = (PaymentCostMovementOperation)objects[10];
                PaymentCostMovement consumablesOrderItemPaymentCostMovement = (PaymentCostMovement)objects[11];

                if (consumablesOrderItemPaymentCostMovementOperation != null)
                    consumablesOrderItemPaymentCostMovementOperation.PaymentCostMovement = consumablesOrderItemPaymentCostMovement;

                if (consumableProduct != null) consumableProduct.MeasureUnit = consumableProductMeasureUnit;

                paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                consumablesOrder.User = consumablesOrderUser;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;

                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.PaymentCostMovementOperation = consumablesOrderItemPaymentCostMovementOperation;
                consumablesOrderItem.ConsumablesOrder = consumablesOrder;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                depreciatedConsumableOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                depreciatedConsumableOrderItem.ConsumablesOrderItem = consumablesOrderItem;

                toReturn.DepreciatedConsumableOrderItems.Add(depreciatedConsumableOrderItem);

                return depreciatedConsumableOrderItem;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [DepreciatedConsumableOrderItem] " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].DepreciatedConsumableOrderItemID = [DepreciatedConsumableOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ID = [DepreciatedConsumableOrderItem].ConsumablesOrderItemID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[Updated] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [ConsumableProductMeasureUnit] " +
                "ON [ConsumableProductMeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [ConsumableProductMeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [PaymentCostMovementOperation] AS [ConsumablesOrderItemPaymentCostMovementOperation] " +
                "ON [ConsumablesOrderItemPaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [ConsumablesOrderItemPaymentCostMovement] " +
                "ON [ConsumablesOrderItemPaymentCostMovement].ID = [ConsumablesOrderItemPaymentCostMovementOperation].PaymentCostMovementID " +
                "WHERE [DepreciatedConsumableOrderItem].DepreciatedConsumableOrderID = @Id " +
                "AND [DepreciatedConsumableOrderItem].Deleted = 0",
                itemsTypes,
                itemsMapper,
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return toReturn;
    }

    public IEnumerable<DepreciatedConsumableOrder> GetAll() {
        Type[] types = {
            typeof(DepreciatedConsumableOrder),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(ConsumablesStorage)
        };

        Func<object[], DepreciatedConsumableOrder> mapper = objects => {
            DepreciatedConsumableOrder depreciatedConsumableOrder = (DepreciatedConsumableOrder)objects[0];
            User createdBy = (User)objects[1];
            User updatedBy = (User)objects[2];
            User depreciatedTo = (User)objects[3];
            User commissionHead = (User)objects[4];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[5];

            depreciatedConsumableOrder.CreatedBy = createdBy;
            depreciatedConsumableOrder.UpdatedBy = updatedBy;
            depreciatedConsumableOrder.DepreciatedTo = depreciatedTo;
            depreciatedConsumableOrder.CommissionHead = commissionHead;
            depreciatedConsumableOrder.ConsumablesStorage = consumablesStorage;

            return depreciatedConsumableOrder;
        };

        IEnumerable<DepreciatedConsumableOrder> toReturn = _connection.Query(
            "SELECT * " +
            "FROM [DepreciatedConsumableOrder] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [DepreciatedConsumableOrder].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [DepreciatedConsumableOrder].UpdatedByID " +
            "LEFT JOIN [User] AS [DepreciatedTo] " +
            "ON [DepreciatedTo].ID = [DepreciatedConsumableOrder].DepreciatedToID " +
            "LEFT JOIN [User] AS [CommissionHead] " +
            "ON [CommissionHead].ID = [DepreciatedConsumableOrder].CommissionHeadID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [DepreciatedConsumableOrder].ConsumablesStorageID " +
            "WHERE [DepreciatedConsumableOrder].Deleted = 0",
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn.Any()) {
            Type[] itemsTypes = {
                typeof(DepreciatedConsumableOrderItem),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumablesOrder),
                typeof(User),
                typeof(ConsumableProduct),
                typeof(MeasureUnit),
                typeof(SupplyOrganization),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement)
            };

            Func<object[], DepreciatedConsumableOrderItem> itemsMapper = objects => {
                DepreciatedConsumableOrderItem depreciatedConsumableOrderItem = (DepreciatedConsumableOrderItem)objects[0];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[1];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[2];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[3];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[4];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[7];
                MeasureUnit consumableProductMeasureUnit = (MeasureUnit)objects[8];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[9];
                PaymentCostMovementOperation consumablesOrderItemPaymentCostMovementOperation = (PaymentCostMovementOperation)objects[10];
                PaymentCostMovement consumablesOrderItemPaymentCostMovement = (PaymentCostMovement)objects[11];

                if (consumablesOrderItemPaymentCostMovementOperation != null)
                    consumablesOrderItemPaymentCostMovementOperation.PaymentCostMovement = consumablesOrderItemPaymentCostMovement;

                if (consumableProduct != null) consumableProduct.MeasureUnit = consumableProductMeasureUnit;

                paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                consumablesOrder.User = consumablesOrderUser;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;

                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.PaymentCostMovementOperation = consumablesOrderItemPaymentCostMovementOperation;
                consumablesOrderItem.ConsumablesOrder = consumablesOrder;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                depreciatedConsumableOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                depreciatedConsumableOrderItem.ConsumablesOrderItem = consumablesOrderItem;

                toReturn.First(o => o.Id.Equals(depreciatedConsumableOrderItem.DepreciatedConsumableOrderId)).DepreciatedConsumableOrderItems
                    .Add(depreciatedConsumableOrderItem);

                return depreciatedConsumableOrderItem;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [DepreciatedConsumableOrderItem] " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].DepreciatedConsumableOrderItemID = [DepreciatedConsumableOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ID = [DepreciatedConsumableOrderItem].ConsumablesOrderItemID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[Updated] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [ConsumableProductMeasureUnit] " +
                "ON [ConsumableProductMeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [ConsumableProductMeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [PaymentCostMovementOperation] AS [ConsumablesOrderItemPaymentCostMovementOperation] " +
                "ON [ConsumablesOrderItemPaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [ConsumablesOrderItemPaymentCostMovement] " +
                "ON [ConsumablesOrderItemPaymentCostMovement].ID = [ConsumablesOrderItemPaymentCostMovementOperation].PaymentCostMovementID " +
                "WHERE [DepreciatedConsumableOrderItem].DepreciatedConsumableOrderID IN @Ids " +
                "AND [DepreciatedConsumableOrderItem].Deleted = 0",
                itemsTypes,
                itemsMapper,
                new { Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return toReturn;
    }

    public IEnumerable<DepreciatedConsumableOrder> GetAllFiltered(DateTime from, DateTime to, string value, Guid? storageNetId) {
        string sqlExpression =
            "SELECT * " +
            "FROM [DepreciatedConsumableOrder] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [DepreciatedConsumableOrder].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [DepreciatedConsumableOrder].UpdatedByID " +
            "LEFT JOIN [User] AS [DepreciatedTo] " +
            "ON [DepreciatedTo].ID = [DepreciatedConsumableOrder].DepreciatedToID " +
            "LEFT JOIN [User] AS [CommissionHead] " +
            "ON [CommissionHead].ID = [DepreciatedConsumableOrder].CommissionHeadID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [DepreciatedConsumableOrder].ConsumablesStorageID " +
            "WHERE [DepreciatedConsumableOrder].Deleted = 0 " +
            "AND [DepreciatedConsumableOrder].Created >= @From " +
            "AND [DepreciatedConsumableOrder].Created <= @To";

        if (storageNetId.HasValue) sqlExpression += " AND [ConsumablesStorage].NetUID = @StorageNetId";

        sqlExpression += " ORDER BY [DepreciatedConsumableOrder].ID DESC";

        Type[] types = {
            typeof(DepreciatedConsumableOrder),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(ConsumablesStorage)
        };

        Func<object[], DepreciatedConsumableOrder> mapper = objects => {
            DepreciatedConsumableOrder depreciatedConsumableOrder = (DepreciatedConsumableOrder)objects[0];
            User createdBy = (User)objects[1];
            User updatedBy = (User)objects[2];
            User depreciatedTo = (User)objects[3];
            User commissionHead = (User)objects[4];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[5];

            depreciatedConsumableOrder.CreatedBy = createdBy;
            depreciatedConsumableOrder.UpdatedBy = updatedBy;
            depreciatedConsumableOrder.DepreciatedTo = depreciatedTo;
            depreciatedConsumableOrder.CommissionHead = commissionHead;
            depreciatedConsumableOrder.ConsumablesStorage = consumablesStorage;

            return depreciatedConsumableOrder;
        };

        IEnumerable<DepreciatedConsumableOrder> toReturn = _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                From = from,
                To = to,
                StorageNetId = storageNetId ?? Guid.Empty
            }
        );

        if (toReturn.Any()) {
            Type[] itemsTypes = {
                typeof(DepreciatedConsumableOrderItem),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumablesOrder),
                typeof(User),
                typeof(ConsumableProduct),
                typeof(MeasureUnit),
                typeof(SupplyOrganization),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(Currency),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], DepreciatedConsumableOrderItem> itemsMapper = objects => {
                DepreciatedConsumableOrderItem depreciatedConsumableOrderItem = (DepreciatedConsumableOrderItem)objects[0];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[1];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[2];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[3];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[4];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[7];
                MeasureUnit consumableProductMeasureUnit = (MeasureUnit)objects[8];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[9];
                PaymentCostMovementOperation consumablesOrderItemPaymentCostMovementOperation = (PaymentCostMovementOperation)objects[10];
                PaymentCostMovement consumablesOrderItemPaymentCostMovement = (PaymentCostMovement)objects[11];
                Currency currency = (Currency)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                if (consumablesOrderItemPaymentCostMovementOperation != null)
                    consumablesOrderItemPaymentCostMovementOperation.PaymentCostMovement = consumablesOrderItemPaymentCostMovement;

                if (consumableProduct != null) consumableProduct.MeasureUnit = consumableProductMeasureUnit;

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                consumablesOrder.User = consumablesOrderUser;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;

                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.PaymentCostMovementOperation = consumablesOrderItemPaymentCostMovementOperation;
                consumablesOrderItem.ConsumablesOrder = consumablesOrder;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                depreciatedConsumableOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                depreciatedConsumableOrderItem.ConsumablesOrderItem = consumablesOrderItem;

                if (currency != null) {
                    depreciatedConsumableOrderItem.Currency = currency;

                    depreciatedConsumableOrderItem.TotalPrice = Math.Round(consumablesOrderItem.PricePerItem * Convert.ToDecimal(depreciatedConsumableOrderItem.Qty), 2);
                } else {
                    depreciatedConsumableOrderItem.Currency = supplyOrganizationAgreementCurrency;

                    depreciatedConsumableOrderItem.TotalPrice = Math.Round(consumablesOrderItem.PricePerItem * Convert.ToDecimal(depreciatedConsumableOrderItem.Qty), 2);
                }

                DepreciatedConsumableOrder orderFromList = toReturn.First(o => o.Id.Equals(depreciatedConsumableOrderItem.DepreciatedConsumableOrderId));

                if (!orderFromList.DepreciatedConsumableOrderItems.Any(i => i.Id.Equals(depreciatedConsumableOrderItem.Id))) {
                    orderFromList.DepreciatedConsumableOrderItems.Add(depreciatedConsumableOrderItem);

                    if (currency != null) {
                        if (orderFromList.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                            PriceTotal total = orderFromList.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                            total.TotalPrice = Math.Round(total.TotalPrice + consumablesOrderItem.PricePerItem * Convert.ToDecimal(depreciatedConsumableOrderItem.Qty), 2);
                        } else {
                            orderFromList.PriceTotals.Add(new PriceTotal {
                                Currency = currency,
                                TotalPrice = Math.Round(consumablesOrderItem.PricePerItem * Convert.ToDecimal(depreciatedConsumableOrderItem.Qty), 2)
                            });
                        }
                    } else {
                        if (orderFromList.PriceTotals.Any(t => t.Currency.Id.Equals(supplyOrganizationAgreementCurrency.Id))) {
                            PriceTotal total = orderFromList.PriceTotals.First(t => t.Currency.Id.Equals(supplyOrganizationAgreementCurrency.Id));

                            total.TotalPrice = Math.Round(total.TotalPrice + consumablesOrderItem.PricePerItem * Convert.ToDecimal(depreciatedConsumableOrderItem.Qty), 2);
                        } else {
                            orderFromList.PriceTotals.Add(new PriceTotal {
                                Currency = supplyOrganizationAgreementCurrency,
                                TotalPrice = Math.Round(consumablesOrderItem.PricePerItem * Convert.ToDecimal(depreciatedConsumableOrderItem.Qty), 2)
                            });
                        }
                    }
                }

                return depreciatedConsumableOrderItem;
            };

            _connection.Query(
                "SELECT [DepreciatedConsumableOrderItem].* " +
                ", [PaymentCostMovementOperation].*" +
                ", [PaymentCostMovement].*" +
                ", [ConsumablesOrderItem].*" +
                ", [ConsumableProductCategory].*" +
                ", [ConsumablesOrder].*" +
                ", [User].*" +
                ", [ConsumableProduct].*" +
                ", [ConsumableProductMeasureUnit].*" +
                ", [ConsumableProductOrganization].*" +
                ", [ConsumablesOrderItemPaymentCostMovementOperation].*" +
                ", [ConsumablesOrderItemPaymentCostMovement].*" +
                ", [OutcomeCurrency].*" +
                ", [SupplyOrganizationAgreement].*" +
                ", [ConsumablesCurrency].* " +
                "FROM [DepreciatedConsumableOrderItem] " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].DepreciatedConsumableOrderItemID = [DepreciatedConsumableOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ID = [DepreciatedConsumableOrderItem].ConsumablesOrderItemID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[Updated] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [ConsumableProductMeasureUnit] " +
                "ON [ConsumableProductMeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [ConsumableProductMeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [PaymentCostMovementOperation] AS [ConsumablesOrderItemPaymentCostMovementOperation] " +
                "ON [ConsumablesOrderItemPaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [ConsumablesOrderItemPaymentCostMovement] " +
                "ON [ConsumablesOrderItemPaymentCostMovement].ID = [ConsumablesOrderItemPaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [OutcomePaymentOrder] " +
                "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeCurrency] " +
                "ON [OutcomeCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [OutcomeCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesCurrency] " +
                "ON [ConsumablesCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [ConsumablesCurrency].CultureCode = @Culture " +
                "WHERE [DepreciatedConsumableOrderItem].DepreciatedConsumableOrderID IN @Ids " +
                "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
                "AND (" +
                "[ConsumableProduct].[Name] like '%' + @Value + '%' " +
                "OR" +
                "[ConsumableProductOrganization].[Name] like '%' + @Value + '%'" +
                ")",
                itemsTypes,
                itemsMapper,
                new { Ids = toReturn.Select(o => o.Id), Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return toReturn.Where(o => o.DepreciatedConsumableOrderItems.Any());
    }
}