using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Supplies.Ukraine.AppliedActions;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class ActReconciliationAppliedActionsRepository : IActReconciliationAppliedActionsRepository {
    private readonly IDbConnection _connection;

    public ActReconciliationAppliedActionsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<ActReconciliationAppliedAction> GetAllAppliedActionsByActReconciliationNetId(Guid netId) {
        List<ActReconciliationAppliedAction> actions = new();

        _connection.Query<ActReconciliationItem, Product, MeasureUnit, ActReconciliationItem>(
            "SELECT [ActReconciliationItem].* " +
            ", [Product].* " +
            ", [MeasureUnit].* " +
            "FROM [ActReconciliation] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "AND [ActReconciliationItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ActReconciliationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "AND [DepreciatedOrderItem].Deleted = 0 " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "AND [ProductTransferItem].Deleted = 0 " +
            "WHERE [ActReconciliation].NetUID = @NetId " +
            "AND ( " +
            "[DepreciatedOrderItem].ID IS NOT NULL " +
            "OR " +
            "[ProductIncomeItem].ID IS NOT NULL " +
            "OR " +
            "[ProductTransferItem].ID IS NOT NULL " +
            ") " +
            "ORDER BY [Product].VendorCode",
            (item, product, measureUnit) => {
                if (!actions.Any(a => a.ActReconciliationItem.Id.Equals(item.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    actions.Add(new ActReconciliationAppliedAction {
                        ActReconciliationItem = item
                    });
                }

                return item;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (actions.Any()) {
            var joinProps = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = actions.Select(a => a.ActReconciliationItem.Id) };

            Type[] depreciatedTypes = {
                typeof(DepreciatedOrder),
                typeof(DepreciatedOrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Storage),
                typeof(User),
                typeof(Organization)
            };

            Func<object[], DepreciatedOrder> depreciatedMapper = objects => {
                DepreciatedOrder depreciatedOrder = (DepreciatedOrder)objects[0];
                DepreciatedOrderItem depreciatedOrderItem = (DepreciatedOrderItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                Storage storage = (Storage)objects[4];
                User user = (User)objects[5];
                Organization organization = (Organization)objects[6];

                product.MeasureUnit = measureUnit;

                depreciatedOrderItem.Product = product;

                depreciatedOrder.Responsible = user;
                depreciatedOrder.Organization = organization;
                depreciatedOrder.Storage = storage;

                depreciatedOrder.DepreciatedOrderItems.Add(depreciatedOrderItem);

                actions
                    .First(a => a.ActReconciliationItem.Id.Equals(depreciatedOrderItem.ActReconciliationItemId ?? 0))
                    .Items.Add(new ActReconciliationAppliedActionItem {
                        ActionType = ActReconciliationAppliedActionType.DepreciatedOrder,
                        DepreciatedOrder = depreciatedOrder
                    });

                return depreciatedOrder;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [DepreciatedOrder] " +
                "LEFT JOIN [DepreciatedOrderItem] " +
                "ON [DepreciatedOrderItem].DepreciatedOrderID = [DepreciatedOrder].ID " +
                "AND [DepreciatedOrderItem].Deleted = 0 " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [DepreciatedOrderItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [DepreciatedOrder].StorageID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [DepreciatedOrder].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [DepreciatedOrderItem].ActReconciliationItemID IN @Ids",
                depreciatedTypes,
                depreciatedMapper,
                joinProps
            );

            Type[] transferTypes = {
                typeof(ProductTransfer),
                typeof(ProductTransferItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(Storage),
                typeof(Storage),
                typeof(Organization)
            };

            Func<object[], ProductTransfer> transferMapper = objects => {
                ProductTransfer productTransfer = (ProductTransfer)objects[0];
                ProductTransferItem productTransferItem = (ProductTransferItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User user = (User)objects[4];
                Storage fromStorage = (Storage)objects[5];
                Storage toStorage = (Storage)objects[6];
                Organization organization = (Organization)objects[7];

                product.MeasureUnit = measureUnit;

                productTransferItem.Product = product;

                productTransfer.Responsible = user;
                productTransfer.Organization = organization;
                productTransfer.FromStorage = fromStorage;
                productTransfer.ToStorage = toStorage;

                productTransfer.ProductTransferItems.Add(productTransferItem);

                actions
                    .First(a => a.ActReconciliationItem.Id.Equals(productTransferItem.ActReconciliationItemId ?? 0))
                    .Items.Add(new ActReconciliationAppliedActionItem {
                        ActionType = ActReconciliationAppliedActionType.ProductTransfer,
                        ProductTransfer = productTransfer
                    });

                return productTransfer;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductTransfer] " +
                "LEFT JOIN [ProductTransferItem] " +
                "ON [ProductTransferItem].ProductTransferID = [ProductTransfer].ID " +
                "AND [ProductTransferItem].Deleted = 0 " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductTransferItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductTransfer].ResponsibleID " +
                "LEFT JOIN [Storage] AS [FromStorage] " +
                "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
                "LEFT JOIN [Storage] AS [ToStorage] " +
                "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ProductTransfer].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductTransferItem].ActReconciliationItemID IN @Ids",
                transferTypes,
                transferMapper,
                joinProps
            );

            Type[] incomesTypes = {
                typeof(ProductIncome),
                typeof(ProductIncomeItem),
                typeof(ActReconciliationItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(Storage)
            };

            Func<object[], ProductIncome> incomesMapper = objects => {
                ProductIncome productIncome = (ProductIncome)objects[0];
                ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[1];
                ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[2];
                Product product = (Product)objects[3];
                MeasureUnit measureUnit = (MeasureUnit)objects[4];
                User user = (User)objects[5];
                Storage storage = (Storage)objects[6];

                product.MeasureUnit = measureUnit;

                actReconciliationItem.Product = product;

                productIncomeItem.ActReconciliationItem = actReconciliationItem;

                productIncome.User = user;
                productIncome.Storage = storage;

                productIncome.ProductIncomeItems.Add(productIncomeItem);

                actions
                    .First(a => a.ActReconciliationItem.Id.Equals(actReconciliationItem.Id))
                    .Items.Add(new ActReconciliationAppliedActionItem {
                        ActionType = ActReconciliationAppliedActionType.ProductIncome,
                        ProductIncome = productIncome
                    });

                return productIncome;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [ActReconciliationItem] " +
                "ON [ActReconciliationItem].ID = [ProductIncomeItem].ActReconciliationItemID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ActReconciliationItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductIncome].UserID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "WHERE [ActReconciliationItem].ID IN @Ids",
                incomesTypes,
                incomesMapper,
                joinProps
            );
        }

        return actions;
    }
}