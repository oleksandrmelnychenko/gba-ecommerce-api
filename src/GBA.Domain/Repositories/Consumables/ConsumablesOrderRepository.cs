using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Consumables.Orders;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumablesOrderRepository : IConsumablesOrderRepository {
    private readonly IDbConnection _connection;

    public ConsumablesOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ConsumablesOrder consumablesOrder) {
        return _connection.Query<long>(
                "INSERT INTO [ConsumablesOrder] (Number, Comment, OrganizationNumber, OrganizationFromDate, IsPayed, UserId, ConsumablesStorageId, SupplyPaymentTaskId, Updated) " +
                "VALUES (@Number, @Comment, @OrganizationNumber, @OrganizationFromDate, @IsPayed, @UserId, @ConsumablesStorageId, @SupplyPaymentTaskId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumablesOrder
            )
            .Single();
    }

    public void Update(ConsumablesOrder consumablesOrder) {
        _connection.Execute(
            "UPDATE [ConsumablesOrder] " +
            "SET Comment = @Comment, OrganizationNumber = @OrganizationNumber, OrganizationFromDate = @OrganizationFromDate, " +
            "IsPayed = @IsPayed, UserId = @UserId, ConsumablesStorageId = @ConsumablesStorageId, SupplyPaymentTaskId = @SupplyPaymentTaskId, Updated = getutcdate() " +
            "WHERE [ConsumablesOrder].ID = @Id",
            consumablesOrder
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [ConsumablesOrder] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ConsumablesOrder].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public decimal GetUnpaidAmountByOrderId(long id) {
        return _connection.Query<decimal>(
                "SELECT ROUND( " +
                "( " +
                "SELECT ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0) " +
                "FROM [ConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "WHERE [ConsumablesOrder].ID = @Id " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([OutcomePaymentOrderConsumablesOrder].PaidAmount), 0) " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = @Id " +
                ") " +
                ", 2)",
                new { Id = id }
            )
            .Single();
    }

    public decimal GetPaidAmountByOrderId(long id) {
        return _connection.Query<decimal>(
                "SELECT ROUND( " +
                "( " +
                "SELECT ISNULL(SUM([OutcomePaymentOrderConsumablesOrder].PaidAmount), 0) " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = @Id " +
                ") " +
                ", 2)",
                new { Id = id }
            )
            .Single();
    }

    public ConsumablesOrder GetLastRecord() {
        return _connection.Query<ConsumablesOrder>(
                "SELECT TOP(1) * " +
                "FROM [ConsumablesOrder] " +
                "WHERE [ConsumablesOrder].Deleted = 0 " +
                "AND [ConsumablesOrder].[Number] != N'Ввід боргів з 1С' " +
                "ORDER BY [ConsumablesOrder].ID DESC "
            )
            .SingleOrDefault();
    }

    public ConsumablesOrder GetById(long id) {
        ConsumablesOrder toReturn = null;

        Type[] types = {
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ConsumablesOrderDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], ConsumablesOrder> mapper = objects => {
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
            User user = (User)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[6];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[7];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[10];
            User SupplyPaymentTaskUser = (User)objects[11];
            ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[12];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];

            if (toReturn == null) {
                if (consumablesOrderItem != null) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (supplyPaymentTask != null) supplyPaymentTask.User = SupplyPaymentTaskUser;

                if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                consumablesOrder.User = user;
                consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                consumablesOrder.ConsumablesStorage = consumablesStorage;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn = consumablesOrder;
            } else {
                if (consumablesOrderItem != null && !toReturn.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    toReturn.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    toReturn.TotalAmount = Math.Round(toReturn.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    toReturn.TotalAmountWithoutVAT = Math.Round(toReturn.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (consumablesOrderDocument != null && !toReturn.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                    toReturn.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
            }

            return consumablesOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ConsumablesOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [ConsumablesOrderDocument] " +
            "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ConsumablesOrder].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public ConsumablesOrder GetByNetId(Guid netId) {
        ConsumablesOrder toReturn = null;

        Type[] types = {
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ConsumablesOrderDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], ConsumablesOrder> mapper = objects => {
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
            User user = (User)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[6];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[7];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[10];
            User SupplyPaymentTaskUser = (User)objects[11];
            ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[12];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];

            if (toReturn == null) {
                if (consumablesOrderItem != null) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (supplyPaymentTask != null) supplyPaymentTask.User = SupplyPaymentTaskUser;

                if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                consumablesOrder.User = user;
                consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                consumablesOrder.ConsumablesStorage = consumablesStorage;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn = consumablesOrder;
            } else {
                if (consumablesOrderItem != null && !toReturn.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    toReturn.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    toReturn.TotalAmount = Math.Round(toReturn.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    toReturn.TotalAmountWithoutVAT = Math.Round(toReturn.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (consumablesOrderDocument != null && !toReturn.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                    toReturn.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
            }

            return consumablesOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ConsumablesOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [ConsumablesOrderDocument] " +
            "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ConsumablesOrder].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<ConsumablesOrder> GetAll(DateTime from, DateTime to) {
        List<ConsumablesOrder> toReturn = new();

        Type[] types = {
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(OutcomePaymentOrder),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(Organization),
            typeof(User),
            typeof(User),
            typeof(PaymentCurrencyRegister),
            typeof(PaymentRegister),
            typeof(Currency),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(MeasureUnit),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ConsumablesOrderDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], ConsumablesOrder> mapper = objects => {
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
            User user = (User)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[8];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[9];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[10];
            Organization organization = (Organization)objects[11];
            User outcomePaymentOrderUser = (User)objects[12];
            User outcomePaymentOrderColleague = (User)objects[13];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[14];
            PaymentRegister paymentRegister = (PaymentRegister)objects[15];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[16];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[17];
            PaymentMovement paymentMovement = (PaymentMovement)objects[18];
            MeasureUnit measureUnit = (MeasureUnit)objects[19];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[20];
            User supplyPaymentTaskUser = (User)objects[21];
            ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[22];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[23];
            Organization consumableProductOrganizationOrganization = (Organization)objects[24];
            Currency currency = (Currency)objects[25];

            if (!toReturn.Any(o => o.Id.Equals(consumablesOrder.Id))) {
                if (consumablesOrderItem != null) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = consumableProductOrganizationOrganization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (outcomePaymentOrderConsumablesOrder != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentCurrencyRegister.PaymentRegister = paymentRegister;
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    outcomePaymentOrder.Organization = organization;
                    outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;

                    outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                    consumablesOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

                consumablesOrder.User = user;
                consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                consumablesOrder.ConsumablesStorage = consumablesStorage;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.Add(consumablesOrder);
            } else {
                ConsumablesOrder fromList = toReturn.First(o => o.Id.Equals(consumablesOrder.Id));

                if (consumablesOrderItem != null && !fromList.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = consumableProductOrganizationOrganization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.ConsumablesOrderItems.Add(consumablesOrderItem);

                    fromList.TotalAmount = Math.Round(fromList.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.TotalAmountWithoutVAT = Math.Round(fromList.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (outcomePaymentOrderConsumablesOrder != null &&
                    !fromList.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentCurrencyRegister.PaymentRegister = paymentRegister;
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    outcomePaymentOrder.Organization = organization;
                    outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;

                    outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                    fromList.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (consumablesOrderDocument != null && !fromList.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                    fromList.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
            }

            return consumablesOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ConsumablesOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
            "SELECT [PaymentMovement].ID " +
            ", [PaymentMovement].[Created] " +
            ", [PaymentMovement].[Deleted] " +
            ", [PaymentMovement].[NetUID] " +
            ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
            ", [PaymentMovement].[Updated] " +
            "FROM [PaymentMovement] " +
            "LEFT JOIN [PaymentMovementTranslation] " +
            "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
            "AND [PaymentMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentMovement] " +
            "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [ConsumablesOrderDocument] " +
            "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ConsumablesOrder].Deleted = 0 " +
            "AND [ConsumablesOrder].Created >= @From " +
            "AND [ConsumablesOrder].Created <= @To " +
            "AND [ConsumableProductOrganization].ID IS NOT NULL " +
            "ORDER BY [ConsumablesOrder].ID DESC",
            types,
            mapper,
            new { From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<ConsumablesOrder> GetAllServices(DateTime from, DateTime to, string value, Guid? organizationNetId) {
        List<ConsumablesOrder> toReturn = new();

        Type[] types = {
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(OutcomePaymentOrder),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(Organization),
            typeof(User),
            typeof(User),
            typeof(PaymentCurrencyRegister),
            typeof(PaymentRegister),
            typeof(Currency),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(MeasureUnit),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ConsumablesOrderDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], ConsumablesOrder> mapper = objects => {
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
            User user = (User)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[8];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[9];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[10];
            Organization organization = (Organization)objects[11];
            User outcomePaymentOrderUser = (User)objects[12];
            User outcomePaymentOrderColleague = (User)objects[13];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[14];
            PaymentRegister paymentRegister = (PaymentRegister)objects[15];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[16];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[17];
            PaymentMovement paymentMovement = (PaymentMovement)objects[18];
            MeasureUnit measureUnit = (MeasureUnit)objects[19];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[20];
            User supplyPaymentTaskUser = (User)objects[21];
            ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[22];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[23];
            Organization consumableProductOrganizationOrganization = (Organization)objects[24];
            Currency currency = (Currency)objects[25];

            if (!toReturn.Any(o => o.Id.Equals(consumablesOrder.Id))) {
                if (consumablesOrderItem != null) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = consumableProductOrganizationOrganization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (outcomePaymentOrderConsumablesOrder != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentCurrencyRegister.PaymentRegister = paymentRegister;
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    outcomePaymentOrder.Organization = organization;
                    outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;

                    outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                    consumablesOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

                consumablesOrder.User = user;
                consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                consumablesOrder.ConsumablesStorage = consumablesStorage;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.Add(consumablesOrder);
            } else {
                ConsumablesOrder fromList = toReturn.First(o => o.Id.Equals(consumablesOrder.Id));

                if (consumablesOrderItem != null && !fromList.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = consumableProductOrganizationOrganization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.ConsumablesOrderItems.Add(consumablesOrderItem);

                    fromList.TotalAmount = Math.Round(fromList.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.TotalAmountWithoutVAT = Math.Round(fromList.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (outcomePaymentOrderConsumablesOrder != null &&
                    !fromList.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentCurrencyRegister.PaymentRegister = paymentRegister;
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    outcomePaymentOrder.Organization = organization;
                    outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;

                    outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                    fromList.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (consumablesOrderDocument != null && !fromList.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                    fromList.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
            }

            return consumablesOrder;
        };

        string sqlExpression =
            "SELECT * " +
            "FROM [ConsumablesOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
            "SELECT [PaymentMovement].ID " +
            ", [PaymentMovement].[Created] " +
            ", [PaymentMovement].[Deleted] " +
            ", [PaymentMovement].[NetUID] " +
            ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
            ", [PaymentMovement].[Updated] " +
            "FROM [PaymentMovement] " +
            "LEFT JOIN [PaymentMovementTranslation] " +
            "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
            "AND [PaymentMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentMovement] " +
            "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [ConsumablesOrderDocument] " +
            "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ConsumablesOrder].Deleted = 0 " +
            "AND [ConsumablesOrder].Created >= @From " +
            "AND [ConsumablesOrder].Created <= @To " +
            "AND [ConsumableProductOrganization].ID IS NULL " +
            "AND (" +
            "[ConsumableProduct].[Name] like '%' + @Value + '%' " +
            "OR [OutcomePaymentOrderColleague].[LastName] like '%' + @Value + '%' " +
            "OR [OutcomePaymentOrder].[AdvanceNumber] like '%' + @Value + '%' " +
            "OR ([ConsumablesOrderItem].[TotalPrice] + [ConsumablesOrderItem].[VAT]) like '%' + @Value + '%'" +
            ") ";

        if (organizationNetId.HasValue) sqlExpression += "AND [Organization].NetUID = @OrganizationNetId ";

        sqlExpression += "ORDER BY [ConsumablesOrder].ID DESC";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { From = from, To = to, Value = value, OrganizationNetId = organizationNetId ?? Guid.Empty, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<ConsumablesOrder> GetAllUnpaidByConsumableOrganizationNetId(Guid organizationNetId) {
        List<ConsumablesOrder> toReturn = new();

        Type[] types = {
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(OutcomePaymentOrder),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ConsumablesOrderDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], ConsumablesOrder> mapper = objects => {
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
            User user = (User)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[6];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[7];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[8];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[9];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[10];
            MeasureUnit measureUnit = (MeasureUnit)objects[11];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
            User supplyPaymentTaskUser = (User)objects[13];
            ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[14];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
            Organization consumableProductOrganizationOrganization = (Organization)objects[16];
            Currency currency = (Currency)objects[17];

            if (!toReturn.Any(o => o.Id.Equals(consumablesOrder.Id))) {
                if (consumablesOrderItem != null) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = consumableProductOrganizationOrganization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (outcomePaymentOrderConsumablesOrder != null) {
                    outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                    consumablesOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

                consumablesOrder.User = user;
                consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                consumablesOrder.ConsumablesStorage = consumablesStorage;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.Add(consumablesOrder);
            } else {
                ConsumablesOrder fromList = toReturn.First(o => o.Id.Equals(consumablesOrder.Id));

                if (consumablesOrderItem != null && !fromList.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = consumableProductOrganizationOrganization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.ConsumablesOrderItems.Add(consumablesOrderItem);

                    fromList.TotalAmount = Math.Round(fromList.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.TotalAmountWithoutVAT = Math.Round(fromList.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (outcomePaymentOrderConsumablesOrder != null &&
                    !fromList.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                    outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                    fromList.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (consumablesOrderDocument != null && !fromList.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                    fromList.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
            }

            return consumablesOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ConsumablesOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [ConsumablesOrderDocument] " +
            "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ConsumablesOrder].Deleted = 0 " +
            "AND [ConsumablesOrder].IsPayed = 0 " +
            "AND [ConsumableProductOrganization].NetUID = @OrganizationNetId " +
            "ORDER BY [ConsumablesOrder].ID DESC",
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, OrganizationNetId = organizationNetId }
        );

        return toReturn;
    }

    public List<ConsumablesOrder> GetAllFromSearch(string value) {
        List<ConsumablesOrder> toReturn = new();

        Type[] types = {
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ConsumablesOrderDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], ConsumablesOrder> mapper = objects => {
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
            User user = (User)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[6];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[7];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[10];
            User supplyPaymentTaskUser = (User)objects[11];
            ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[12];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];

            if (!toReturn.Any(o => o.Id.Equals(consumablesOrder.Id))) {
                if (consumablesOrderItem != null) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                }

                if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

                consumablesOrder.User = user;
                consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                consumablesOrder.ConsumablesStorage = consumablesStorage;
                consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.Add(consumablesOrder);
            } else {
                ConsumablesOrder fromList = toReturn.First(o => o.Id.Equals(consumablesOrder.Id));

                if (consumablesOrderItem != null && !fromList.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.ConsumablesOrderItems.Add(consumablesOrderItem);

                    fromList.TotalAmount = Math.Round(fromList.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    fromList.TotalAmountWithoutVAT = Math.Round(fromList.TotalAmount + consumablesOrderItem.TotalPrice, 2);
                }

                if (consumablesOrderDocument != null && !fromList.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                    fromList.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
            }

            return consumablesOrder;
        };

        _connection.Query(
            "SELECT [ConsumablesOrder].* " +
            ", [User].* " +
            ", [ConsumablesOrderItem].* " +
            ", [ConsumableProductCategory].* " +
            ", [ConsumableProduct].* " +
            ", [ConsumableProductOrganization].* " +
            ", [ConsumablesStorage].* " +
            ", [PaymentCostMovementOperation].* " +
            ", [PaymentCostMovement].* " +
            ", [MeasureUnit].* " +
            ", [SupplyPaymentTask].* " +
            ", [SupplyPaymentTaskUser].* " +
            ", [ConsumablesOrderDocument].* " +
            ", [SupplyOrganizationAgreement].* " +
            ", [ConsumableProductOrganizationOrganization].* " +
            ", [Currency].* " +
            "FROM [ConsumablesOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [ConsumablesOrderDocument] " +
            "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ConsumablesOrder].Deleted = 0 " +
            "AND (" +
            "[ConsumablesOrder].Number like '%' + @Value + '%' " +
            "OR " +
            "[ConsumablesOrder].OrganizationNumber like '%' + @Value + '%' " +
            ") " +
            "AND [OutcomePaymentOrderConsumablesOrder].ID IS NULL",
            types,
            mapper,
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }
}