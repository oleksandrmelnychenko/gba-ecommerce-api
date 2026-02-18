using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumablesStorageRepository : IConsumablesStorageRepository {
    private readonly IDbConnection _connection;

    public ConsumablesStorageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ConsumablesStorage consumablesStorage) {
        return _connection.Query<long>(
                "INSERT INTO [ConsumablesStorage] (Name, Description, ResponsibleUserId, OrganizationId, Updated) " +
                "VALUES (@Name, @Description, @ResponsibleUserId, @OrganizationId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumablesStorage
            )
            .Single();
    }

    public void Update(ConsumablesStorage consumablesStorage) {
        _connection.Execute(
            "UPDATE [ConsumablesStorage] " +
            "SET Name = @Name, Description = @Description, ResponsibleUserId = @ResponsibleUserId, OrganizationId = @OrganizationId, Updated = getutcdate() " +
            "WHERE [ConsumablesStorage].ID = @Id",
            consumablesStorage
        );
    }

    public ConsumablesStorage GetById(long id) {
        return _connection.Query<ConsumablesStorage, User, Organization, ConsumablesStorage>(
                "SELECT * " +
                "FROM [ConsumablesStorage] " +
                "LEFT JOIN [User] AS [ResponsibleUser] " +
                "ON [ResponsibleUser].ID = [ConsumablesStorage].ResponsibleUserID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ConsumablesStorage].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ConsumablesStorage].ID = @Id",
                (storage, responsible, organization) => {
                    storage.ResponsibleUser = responsible;
                    storage.Organization = organization;

                    return storage;
                },
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public ConsumablesStorage GetByNetId(Guid netId) {
        return _connection.Query<ConsumablesStorage, User, Organization, ConsumablesStorage>(
                "SELECT * " +
                "FROM [ConsumablesStorage] " +
                "LEFT JOIN [User] AS [ResponsibleUser] " +
                "ON [ResponsibleUser].ID = [ConsumablesStorage].ResponsibleUserID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ConsumablesStorage].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ConsumablesStorage].NetUID = @NetId",
                (storage, responsible, organization) => {
                    storage.ResponsibleUser = responsible;
                    storage.Organization = organization;

                    return storage;
                },
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public IEnumerable<ConsumablesStorage> GetAll() {
        List<ConsumablesStorage> toReturn = new();

        Type[] types = {
            typeof(ConsumablesStorage),
            typeof(User),
            typeof(Organization),
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(OutcomePaymentOrder),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement)
        };

        Func<object[], ConsumablesStorage> mapper = objects => {
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[0];
            User responsibleUser = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[3];
            User user = (User)objects[4];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[5];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[6];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[7];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[8];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[9];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[10];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[11];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[12];

            if (!toReturn.Any(o => o.Id.Equals(consumablesStorage.Id))) {
                if (consumablesOrder != null && consumablesOrderItem != null) {
                    if (outcomePaymentOrderConsumablesOrder != null) {
                        outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                        consumablesOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.TotalPriceWithVAT = Math.Round(Convert.ToDecimal(consumablesOrderItem.Qty) * consumablesOrderItem.PricePerItem, 2);

                    consumablesOrderItem.TotalPrice = Math.Round(consumablesOrderItem.TotalPriceWithVAT - consumablesOrderItem.VAT, 2);

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);

                    consumablesOrder.User = user;
                    consumablesOrder.ConsumablesStorage = consumablesStorage;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;

                    consumablesStorage.ConsumablesOrders.Add(consumablesOrder);
                }

                consumablesStorage.Organization = organization;
                consumablesStorage.ResponsibleUser = responsibleUser;

                toReturn.Add(consumablesStorage);
            } else {
                if (consumablesOrder != null && consumablesOrderItem != null) {
                    ConsumablesStorage fromList = toReturn.First(o => o.Id.Equals(consumablesStorage.Id));

                    if (fromList.ConsumablesOrders.Any(o => o.Id.Equals(consumablesOrder.Id))) {
                        ConsumablesOrder orderFromList = fromList.ConsumablesOrders.First(o => o.Id.Equals(consumablesOrder.Id));

                        if (!orderFromList.ConsumablesOrderItems.Any(i => i.Id.Equals(orderFromList.Id))) {
                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(Convert.ToDecimal(consumablesOrderItem.Qty) * consumablesOrderItem.PricePerItem, 2);

                            consumablesOrderItem.TotalPrice = Math.Round(consumablesOrderItem.TotalPriceWithVAT - consumablesOrderItem.VAT, 2);

                            orderFromList.ConsumablesOrderItems.Add(consumablesOrderItem);

                            orderFromList.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice, 2);
                        }

                        if (outcomePaymentOrderConsumablesOrder != null &&
                            !orderFromList.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                            outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                            orderFromList.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                        }
                    } else {
                        if (outcomePaymentOrderConsumablesOrder != null) {
                            outcomePaymentOrderConsumablesOrder.OutcomePaymentOrder = outcomePaymentOrder;

                            consumablesOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                        }

                        if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(Convert.ToDecimal(consumablesOrderItem.Qty) * consumablesOrderItem.PricePerItem, 2);

                        consumablesOrderItem.TotalPrice = Math.Round(consumablesOrderItem.TotalPriceWithVAT - consumablesOrderItem.VAT, 2);

                        consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);

                        consumablesOrder.User = user;
                        consumablesOrder.ConsumablesStorage = consumablesStorage;
                        consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;

                        fromList.ConsumablesOrders.Add(consumablesOrder);
                    }
                }
            }

            return consumablesStorage;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ConsumablesStorage] " +
            "LEFT JOIN [User] AS [ResponsibleUser] " +
            "ON [ResponsibleUser].ID = [ConsumablesStorage].ResponsibleUserID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ConsumablesStorage].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ConsumablesStorageID = [ConsumablesStorage].ID " +
            "AND [ConsumablesOrder].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN (" +
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
            ") AS [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "AND [ConsumablesOrderItem].Qty > 0 " +
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
            "ON [PaymentCostMovementOperation].PaymentCostMovementID = [PaymentCostMovement].ID " +
            "WHERE [ConsumablesStorage].Deleted = 0 ",
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn.Any()) {
            List<long> productIds = new();

            _connection.Query<ConsumablesStorage, ConsumableProduct, ConsumableProductCategory, MeasureUnit, ConsumablesStorage>(
                "SELECT [ConsumablesStorage].ID " +
                ",[ConsumableProduct].ID " +
                ",[ConsumableProduct].[ConsumableProductCategoryID] " +
                ",[ConsumableProduct].[Created] " +
                ",[ConsumableProduct].[Deleted] " +
                ",(CASE WHEN [ConsumableProductTranslation].[Name] IS NOT NULL THEN [ConsumableProductTranslation].[Name] ELSE [ConsumableProduct].[Name] END) AS [Name] " +
                ",[ConsumableProduct].[NetUID] " +
                ",[ConsumableProduct].[Updated] " +
                ",[ConsumableProduct].[VendorCode] " +
                ",[ConsumableProduct].[MeasureUnitID] " +
                ",ROUND(SUM([ConsumablesOrderItem].TotalPrice), 2) AS [TotalPrice] " +
                ",(SUM([ConsumablesOrderItem].Qty)) AS [TotalQty] " +
                ",[ConsumableProductCategory].[ID] " +
                ",[ConsumableProductCategory].[Created] " +
                ",[ConsumableProductCategory].[Deleted] " +
                ",(CASE WHEN [ConsumableProductCategoryTranslation].[Description] IS NOT NULL THEN [ConsumableProductCategoryTranslation].[Description] ELSE [ConsumableProductCategory].[Description] END) AS [Description] " +
                ",(CASE WHEN [ConsumableProductCategoryTranslation].[Name] IS NOT NULL THEN [ConsumableProductCategoryTranslation].[Name] ELSE [ConsumableProductCategory].[Name] END) AS [Name] " +
                ",[ConsumableProductCategory].[NetUID] " +
                ",[ConsumableProductCategory].[Updated] " +
                ",[MeasureUnit].[ID] " +
                ",[MeasureUnit].[Created] " +
                ",[MeasureUnit].[Deleted] " +
                ",(CASE WHEN [MeasureUnitTranslation].[Description] IS NOT NULL THEN [MeasureUnitTranslation].[Description] ELSE [MeasureUnit].[Description] END) AS [Description] " +
                ",(CASE WHEN [MeasureUnitTranslation].[Name] IS NOT NULL THEN [MeasureUnitTranslation].[Name] ELSE [MeasureUnit].[Name] END) AS [Name] " +
                ",[MeasureUnit].[NetUID] " +
                ",[MeasureUnit].[Updated] " +
                "FROM [ConsumablesStorage] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ConsumablesStorageID = [ConsumablesStorage].ID " + "LEFT JOIN (" +
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
                ") AS [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                "LEFT JOIN [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumableProduct].ConsumableProductCategoryID " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                "LEFT JOIN [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "LEFT JOIN [MeasureUnitTranslation] " +
                "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
                "AND [MeasureUnitTranslation].CultureCode = @Culture " +
                "AND [MeasureUnitTranslation].Deleted = 0 " +
                "WHERE [ConsumablesStorage].Deleted = 0 " +
                "AND [ConsumablesOrderItem].Qty > 0 " +
                "GROUP BY [ConsumablesStorage].ID " +
                ",[ConsumableProduct].ID " +
                ",[ConsumableProduct].[ConsumableProductCategoryID] " +
                ",[ConsumableProduct].[Created] " +
                ",[ConsumableProduct].[Deleted] " +
                ",[ConsumableProduct].[Name] " +
                ",[ConsumableProduct].[NetUID] " +
                ",[ConsumableProduct].[Updated] " +
                ",[ConsumableProduct].[VendorCode] " +
                ",[ConsumableProduct].[MeasureUnitID] " +
                ",[ConsumableProductTranslation].[Name] " +
                ",[ConsumableProductCategory].[ID] " +
                ",[ConsumableProductCategory].[Created] " +
                ",[ConsumableProductCategory].[Deleted] " +
                ",[ConsumableProductCategory].[Description] " +
                ",[ConsumableProductCategory].[Name] " +
                ",[ConsumableProductCategory].[NetUID] " +
                ",[ConsumableProductCategory].[Updated] " +
                ",[ConsumableProductCategoryTranslation].[Name] " +
                ",[ConsumableProductCategoryTranslation].[Description] " +
                ",[MeasureUnit].[ID] " +
                ",[MeasureUnit].[Created] " +
                ",[MeasureUnit].[Deleted] " +
                ",[MeasureUnit].[Description] " +
                ",[MeasureUnit].[Name] " +
                ",[MeasureUnit].[NetUID] " +
                ",[MeasureUnit].[Updated] " +
                ",[MeasureUnitTranslation].[Name] " +
                ",[MeasureUnitTranslation].[Description]",
                (storage, product, category, measureUnit) => {
                    if (product != null) {
                        product.ConsumableProductCategory = category;
                        product.MeasureUnit = measureUnit;

                        toReturn.First(s => s.Id.Equals(storage.Id)).ConsumableProducts.Add(product);

                        productIds.Add(product.Id);
                    }

                    return storage;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

            if (productIds.Any())
                _connection.Query<ConsumablesOrderItem, ConsumablesStorage, Currency, Currency, ConsumablesOrderItem>(
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
                    ",[ConsumablesStorage].ID " +
                    ",[OrganizationCurrency].* " +
                    ",[Currency].* " +
                    "FROM [ConsumablesOrderItem] " +
                    "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                    "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [views].[CurrencyView] AS [OrganizationCurrency] " +
                    "ON [OrganizationCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "AND [OrganizationCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ConsumablesOrder] " +
                    "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                    "LEFT JOIN [ConsumablesStorage] " +
                    "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                    "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                    "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                    "LEFT JOIN [OutcomePaymentOrder] " +
                    "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID " +
                    "LEFT JOIN [PaymentCurrencyRegister] " +
                    "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                    "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                    "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                    "AND [Currency].CultureCode = @Culture " +
                    "WHERE ConsumablesOrderItem.Deleted = 0 " +
                    "AND ( " +
                    "SELECT (MIN([ForCalculationConsumablesOrderItem].Qty) - SUM(ISNULL([DepreciatedConsumableOrderItem].Qty, 0))) " +
                    "FROM [ConsumablesOrderItem] AS [ForCalculationConsumablesOrderItem] " +
                    "LEFT JOIN [DepreciatedConsumableOrderItem] " +
                    "ON [DepreciatedConsumableOrderItem].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                    "AND [DepreciatedConsumableOrderItem].Deleted = 0 " +
                    "WHERE [ForCalculationConsumablesOrderItem].ID = [ConsumablesOrderItem].ID " +
                    ") > 0 " +
                    "AND [ConsumablesOrderItem].ConsumableProductID IN @ProductIds",
                    (orderItem, storage, organizationCurrency, currency) => {
                        if (storage == null || orderItem.ConsumableProductId == null) return orderItem;

                        ConsumablesStorage fromList = toReturn.First(s => s.Id.Equals(storage.Id));

                        ConsumableProduct productFromList = fromList.ConsumableProducts.First(p => p.Id.Equals(orderItem.ConsumableProductId.Value));

                        if (currency != null) {
                            if (productFromList.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                                PriceTotal total = productFromList.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                                total.TotalPrice = Math.Round(total.TotalPrice + Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2);
                            } else {
                                productFromList.PriceTotals.Add(new PriceTotal {
                                    Currency = currency,
                                    TotalPrice = Math.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2)
                                });
                            }

                            if (fromList.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                                PriceTotal total = fromList.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                                total.TotalPrice = Math.Round(total.TotalPrice + Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2);
                            } else {
                                fromList.PriceTotals.Add(new PriceTotal {
                                    Currency = currency,
                                    TotalPrice = Math.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2)
                                });
                            }
                        } else if (organizationCurrency != null) {
                            if (productFromList.PriceTotals.Any(t => t.Currency.Id.Equals(organizationCurrency.Id))) {
                                PriceTotal total = productFromList.PriceTotals.First(t => t.Currency.Id.Equals(organizationCurrency.Id));

                                total.TotalPrice = Math.Round(total.TotalPrice + Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2);
                            } else {
                                productFromList.PriceTotals.Add(new PriceTotal {
                                    Currency = organizationCurrency,
                                    TotalPrice = Math.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2)
                                });
                            }

                            if (fromList.PriceTotals.Any(t => t.Currency.Id.Equals(organizationCurrency.Id))) {
                                PriceTotal total = fromList.PriceTotals.First(t => t.Currency.Id.Equals(organizationCurrency.Id));

                                total.TotalPrice = Math.Round(total.TotalPrice + Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2);
                            } else {
                                fromList.PriceTotals.Add(new PriceTotal {
                                    Currency = organizationCurrency,
                                    TotalPrice = Math.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem, 2)
                                });
                            }
                        }

                        return orderItem;
                    },
                    new { ProductIds = productIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                );
        }

        return toReturn;
    }

    public IEnumerable<ConsumablesStorage> GetAllFromSearch(string value) {
        return _connection.Query<ConsumablesStorage, User, Organization, ConsumablesStorage>(
            "SELECT * " +
            "FROM [ConsumablesStorage] " +
            "LEFT JOIN [User] AS [ResponsibleUser] " +
            "ON [ResponsibleUser].ID = [ConsumablesStorage].ResponsibleUserID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ConsumablesStorage].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ConsumablesStorage].Deleted = 0 " +
            "AND (" +
            "[ConsumablesStorage].Name like '%' + @Value + '%' " +
            "OR " +
            "[ConsumablesStorage].Description like '%' + @Value + '%'" +
            ")",
            (storage, responsible, organization) => {
                storage.ResponsibleUser = responsible;
                storage.Organization = organization;

                return storage;
            },
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [ConsumablesStorage] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ConsumablesStorage].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}