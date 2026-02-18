using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Domain.Repositories.Consignments;

public sealed class ConsignmentItemRepository : IConsignmentItemRepository {
    private readonly IDbConnection _connection;

    public ConsignmentItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ConsignmentItem consignmentItem) {
        return _connection.Query<long>(
            "INSERT INTO [ConsignmentItem] " +
            "(Qty, RemainingQty, Weight, Price, NetPrice, DutyPercent, ProductId, ConsignmentId, RootConsignmentItemId, ProductIncomeItemId, " +
            "ProductSpecificationId, Updated, AccountingPrice, [ExchangeRate]) " +
            "VALUES " +
            "(@Qty, @RemainingQty, @Weight, @Price, @NetPrice, @DutyPercent, @ProductId, @ConsignmentId, @RootConsignmentItemId, @ProductIncomeItemId, " +
            "@ProductSpecificationId, GETUTCDATE(), @AccountingPrice, @ExchangeRate); " +
            "SELECT SCOPE_IDENTITY()",
            consignmentItem
        ).Single();
    }

    public void Add(IEnumerable<ConsignmentItem> consignmentItems) {
        _connection.Execute(
            "INSERT INTO [ConsignmentItem] " +
            "(Qty, RemainingQty, Weight, Price, NetPrice, DutyPercent, ProductId, ConsignmentId, RootConsignmentItemId, ProductIncomeItemId, " +
            "ProductSpecificationId, Updated, AccountingPrice, [ExchangeRate) " +
            "VALUES " +
            "(@Qty, @RemainingQty, @Weight, @Price, @NetPrice, @DutyPercent, @ProductId, @ConsignmentId, @RootConsignmentItemId, @ProductIncomeItemId, " +
            "@ProductSpecificationId, GETUTCDATE(), @AccountingPrice, @ExchangeRate)",
            consignmentItems
        );
    }

    public void UpdateRemainingQty(ConsignmentItem item) {
        _connection.Execute(
            "UPDATE [ConsignmentItem] " +
            "SET RemainingQty = @RemainingQty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            item
        );
    }

    public IEnumerable<ConsignmentItem> GetAllAvailable(long organizationId, long storageId, long productId, ProductWriteOffRuleType ruleType) {
        string sqlExpression =
            "SELECT " +
            "[ConsignmentItem].* " +
            ", [Consignment].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "WHERE [ConsignmentItem].Deleted = 0 " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ConsignmentItem].ProductID = @ProductId " +
            "AND [Consignment].StorageID = @StorageId ";
        //"AND [Consignment].OrganizationID = @OrganizationId ";

        switch (ruleType) {
            case ProductWriteOffRuleType.ByWeight:
                sqlExpression += "ORDER BY [ConsignmentItem].Weight DESC";

                break;
            case ProductWriteOffRuleType.ByPrice:
                sqlExpression += "ORDER BY [ConsignmentItem].Price DESC";

                break;
            case ProductWriteOffRuleType.ByFromDate:
                sqlExpression += "ORDER BY " +
                                 "CASE " +
                                 "WHEN [SupplyInvoice].[ID] IS NULL " +
                                 "THEN " +
                                 "CASE " +
                                 "WHEN [SupplyOrderUkraine].[ID] IS NULL " +
                                 "THEN [Consignment].[FromDate] " +
                                 "ELSE [SupplyOrderUkraine].[InvDate] " +
                                 "END " +
                                 "ELSE " +
                                 "CASE " +
                                 "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL " +
                                 "THEN [SupplyInvoice].[Created] " +
                                 "ELSE [SupplyInvoice].[DateCustomDeclaration] " +
                                 "END " +
                                 "END ";

                break;
            case ProductWriteOffRuleType.ByDutyRate:
                sqlExpression += "ORDER BY [ConsignmentItem].DutyPercent DESC";

                break;
        }

        return _connection.Query<ConsignmentItem, Consignment, ConsignmentItem>(
            sqlExpression,
            (consignmentItem, consignment) => {
                consignmentItem.Consignment = consignment;

                return consignmentItem;
            },
            new {
                ProductId = productId,
                StorageId = storageId,
                OrganizationId = organizationId
            }
        );
    }

    public IEnumerable<ConsignmentItem> GetAllAvailable(long organizationId, long productId, ProductWriteOffRuleType ruleType, string storageLocale) {
        string sqlExpression =
            "SELECT [ConsignmentItem].* " +
            ", [Consignment].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "WHERE [ConsignmentItem].Deleted = 0 " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ConsignmentItem].ProductID = @ProductId " +
            "AND [Storage].Locale = @StorageLocale " +
            "AND [Consignment].OrganizationID = @OrganizationId ";

        switch (ruleType) {
            case ProductWriteOffRuleType.ByWeight:
                sqlExpression += "ORDER BY [ConsignmentItem].Weight DESC";

                break;
            case ProductWriteOffRuleType.ByPrice:
                sqlExpression += "ORDER BY [ConsignmentItem].Price DESC";

                break;
            case ProductWriteOffRuleType.ByFromDate:
                sqlExpression += "ORDER BY [Consignment].FromDate";

                break;
            case ProductWriteOffRuleType.ByDutyRate:
                sqlExpression += "ORDER BY [ConsignmentItem].DutyPercent DESC";

                break;
        }

        return _connection.Query<ConsignmentItem, Consignment, ConsignmentItem>(
            sqlExpression,
            (consignmentItem, consignment) => {
                consignmentItem.Consignment = consignment;

                return consignmentItem;
            },
            new {
                ProductId = productId,
                StorageLocale = storageLocale,
                OrganizationId = organizationId
            }
        );
    }

    public IEnumerable<ConsignmentItem> GetAllAvailable(long organizationId, long productId, ProductWriteOffRuleType ruleType, bool vatStorage, bool forDefective = false,
        long? storageId = null) {
        string sqlExpression =
            "SELECT [ConsignmentItem].* " +
            ", 0 AS [IsReSaleAvailability] " +
            ", [Consignment].* " +
            ", [ProductSpecification].* " +
            ", CASE WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN CASE WHEN [SupplyOrderUkraine].[ID] IS NULL THEN [Consignment].[FromDate] ELSE [SupplyOrderUkraine].[InvDate] " +
            "END ELSE CASE WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL THEN [SupplyInvoice].[Created] ELSE [SupplyInvoice].[DateCustomDeclaration] " +
            "END END AS [InvoiceFromDate] " +
            ", [Storage].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = [ConsignmentItem].ProductSpecificationID " +
            "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
            "ON [RootConsignmentItem].[ID] = [ConsignmentItem].[RootConsignmentItemID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = " +
            "CASE " +
            "WHEN [RootConsignmentItem].[ID] IS NULL " +
            "THEN [ConsignmentItem].[ProductIncomeItemID] " +
            "ELSE [RootConsignmentItem].[ProductIncomeItemID] " +
            "END " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "WHERE [ConsignmentItem].Deleted = 0 " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ConsignmentItem].ProductID = @ProductId " +
            "AND [Storage].ForVatProducts = @VatStorage " +
            "AND [Storage].ForDefective = @ForDefective " +
            "AND [Consignment].OrganizationID = @OrganizationId ";

        if (storageId != null)
            sqlExpression +=
                "UNION " +
                "SELECT [ConsignmentItem].* " +
                ", 0 AS [IsReSaleAvailability] " +
                ", [Consignment].* " +
                ", [ProductSpecification].* " +
                ", CASE WHEN [SupplyInvoice].[ID] IS NULL " +
                "THEN CASE WHEN [SupplyOrderUkraine].[ID] IS NULL THEN [Consignment].[FromDate] ELSE [SupplyOrderUkraine].[InvDate] " +
                "END ELSE CASE WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL THEN [SupplyInvoice].[Created] ELSE [SupplyInvoice].[DateCustomDeclaration] " +
                "END END AS [InvoiceFromDate] " +
                ", [Storage].* " +
                "FROM [ConsignmentItem] " +
                "LEFT JOIN [Consignment] " +
                "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
                "LEFT JOIN Organization " +
                "ON [Consignment].OrganizationID = [Organization].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [Consignment].StorageID " +
                "LEFT JOIN [ProductSpecification] " +
                "ON [ProductSpecification].ID = [ConsignmentItem].ProductSpecificationID " +
                "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
                "ON [RootConsignmentItem].[ID] = [ConsignmentItem].[RootConsignmentItemID] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].[ID] = " +
                "CASE " +
                "WHEN [RootConsignmentItem].[ID] IS NULL " +
                "THEN [ConsignmentItem].[ProductIncomeItemID] " +
                "ELSE [RootConsignmentItem].[ProductIncomeItemID] " +
                "END " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
                "WHERE [ConsignmentItem].Deleted = 0 " +
                "AND [ConsignmentItem].RemainingQty <> 0 " +
                "AND [ConsignmentItem].ProductID = @ProductId " +
                "AND [Storage].ForVatProducts = @VatStorage " +
                "AND [Storage].ForDefective = @ForDefective " +
                "AND [Storage].ID = @StorageId ";

        if (!vatStorage)
            sqlExpression +=
                " UNION " +
                "SELECT [ConsignmentItem].* " +
                ", 1 AS [IsReSaleAvailability] " +
                ", [Consignment].* " +
                ", [ProductSpecification].* " +
                ", CASE WHEN [SupplyInvoice].[ID] IS NULL " +
                "THEN CASE WHEN [SupplyOrderUkraine].[ID] IS NULL THEN [Consignment].[FromDate] ELSE [SupplyOrderUkraine].[InvDate] " +
                "END ELSE CASE WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL THEN [SupplyInvoice].[Created] ELSE [SupplyInvoice].[DateCustomDeclaration] " +
                "END END AS [InvoiceFromDate] " +
                ", [Storage].* " +
                "FROM [ConsignmentItem] " +
                "LEFT JOIN [Consignment] " +
                "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [Consignment].StorageID " +
                "LEFT JOIN [ProductSpecification] " +
                "ON [ProductSpecification].ID = [ConsignmentItem].ProductSpecificationID " +
                "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
                "ON [RootConsignmentItem].[ID] = [ConsignmentItem].[RootConsignmentItemID] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].[ID] = " +
                "CASE " +
                "WHEN [RootConsignmentItem].[ID] IS NULL " +
                "THEN [ConsignmentItem].[ProductIncomeItemID] " +
                "ELSE [RootConsignmentItem].[ProductIncomeItemID] " +
                "END " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
                "WHERE [ConsignmentItem].Deleted = 0 " +
                "AND [ConsignmentItem].RemainingQty <> 0 " +
                "AND [ConsignmentItem].ProductID = @ProductId " +
                "AND [Storage].ForVatProducts = 1 " +
                "AND [Storage].AvailableForReSale = 1 " +
                "AND [Storage].ForDefective = 0 ";

        sqlExpression += "ORDER BY [IsReSaleAvailability] ASC ";

        switch (ruleType) {
            case ProductWriteOffRuleType.ByWeight:
                sqlExpression += ", [ConsignmentItem].Weight;";

                break;
            case ProductWriteOffRuleType.ByPrice:
                sqlExpression += ", [ConsignmentItem].Price;";

                break;
            case ProductWriteOffRuleType.ByFromDate:
                sqlExpression += ", [InvoiceFromDate];";

                break;
            case ProductWriteOffRuleType.ByDutyRate:
                sqlExpression += ", [ConsignmentItem].DutyPercent;";

                break;
        }

        return _connection.Query<ConsignmentItem, Consignment, ProductSpecification, DateTime, Storage, ConsignmentItem>(
            sqlExpression,
            (consignmentItem, consignment, specification, invoiceFromDate, storage) => {
                consignment.Storage = storage;
                consignmentItem.Consignment = consignment;
                consignmentItem.ProductSpecification = specification;

                return consignmentItem;
            },
            new {
                ProductId = productId,
                VatStorage = vatStorage,
                ForDefective = forDefective,
                OrganizationId = organizationId,
                StorageId = storageId
            }, splitOn: "Id,InvoiceFromDate,Id"
        );
    }

    public IEnumerable<ConsignmentItem> GetAvailableItemsCreatedFromSpecificRootItemOnSpecificStorage(long rootItemId, long storageId) {
        return _connection.Query<ConsignmentItem, Consignment, ConsignmentItem>(
            ";WITH [FullHierarchyConsignmentItems_CTE] " +
            "AS ( " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "WHERE [ConsignmentItem].ID = @RootItemId " +
            "AND [Consignment].IsVirtual = 0 " +
            "UNION ALL " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [ConsignmentItem] " +
            "INNER JOIN [FullHierarchyConsignmentItems_CTE] AS [Parents] " +
            "ON [Parents].ID = [ConsignmentItem].RootConsignmentItemID " +
            ") " +
            "SELECT * " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "WHERE [Consignment].StorageID = @StorageId " +
            "AND [ConsignmentItem].ID IN ( " +
            "SELECT ID " +
            "FROM [FullHierarchyConsignmentItems_CTE] " +
            ") " +
            "AND [ConsignmentItem].RemainingQty <> 0",
            (consignmentItem, consignment) => {
                consignmentItem.Consignment = consignment;

                return consignmentItem;
            },
            new { RootItemId = rootItemId, StorageId = storageId }
        );
    }

    public void UpdateGrossPriceAfterIncomes(ConsignmentItem item) {
        _connection.Execute(
            "UPDATE [ConsignmentItem] " +
            "SET [Updated] = getutcdate() " +
            ", [Price] = @Price " +
            ", [AccountingPrice] = @AccountingPrice " +
            ", [NetPrice] = @NetPrice " +
            ", [ExchangeRate] = @ExchangeRate " +
            "WHERE [ConsignmentItem].[ID] = @Id; ",
            item);
    }

    public IEnumerable<ConsignmentItem> GetAllAvailableWithIncludes(long organizationId, long storageId, long productId, ProductWriteOffRuleType ruleType) {
        IEnumerable<ConsignmentItem> toReturn = new List<ConsignmentItem>();

        string sqlExpression =
            "SELECT * " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "WHERE [ConsignmentItem].Deleted = 0 " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ConsignmentItem].ProductID = @ProductId " +
            "AND [Consignment].StorageID = @StorageId " +
            "AND [Consignment].OrganizationID = @OrganizationId ";

        switch (ruleType) {
            case ProductWriteOffRuleType.ByWeight:
                sqlExpression += "ORDER BY [ConsignmentItem].Weight DESC";

                break;
            case ProductWriteOffRuleType.ByPrice:
                sqlExpression += "ORDER BY [ConsignmentItem].Price DESC";

                break;
            case ProductWriteOffRuleType.ByFromDate:
                sqlExpression += "ORDER BY [Consignment].FromDate";

                break;
            case ProductWriteOffRuleType.ByDutyRate:
                sqlExpression += "ORDER BY [ConsignmentItem].DutyPercent DESC";

                break;
        }

        toReturn = _connection.Query<ConsignmentItem, Consignment, ConsignmentItem>(
            sqlExpression,
            (consignmentItem, consignment) => {
                consignmentItem.Consignment = consignment;

                return consignmentItem;
            },
            new {
                ProductId = productId,
                StorageId = storageId,
                OrganizationId = organizationId
            }
        );

        if (!toReturn.Any())
            return toReturn;

        Type[] types = {
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SupplyOrderUkraineItem),
            typeof(PackingListPackageOrderItem),
            typeof(ProductCapitalizationItem),
            typeof(OrderItem),
            typeof(ConsignmentItemMovement)
        };

        Func<object[], ProductIncomeItem> mapper = objects => {
            ProductIncomeItem incomeItem = (ProductIncomeItem)objects[0];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[2];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[3];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            ConsignmentItemMovement consignmentItemMovement = (ConsignmentItemMovement)objects[6];

            if (saleReturnItem != null) {
                if (!toReturn.Any(x => x.ProductIncomeItem.SaleReturnItemId.Equals(saleReturnItem.Id))) {
                    saleReturnItem.OrderItem = orderItem;
                    incomeItem.SaleReturnItem = saleReturnItem;
                } else {
                    saleReturnItem = toReturn.First(x => x.ProductIncomeItem.SaleReturnItemId.Equals(saleReturnItem.Id)).ProductIncomeItem.SaleReturnItem;
                }

                if (!saleReturnItem.OrderItem.ConsignmentItemMovements.Any(x => x.Id.Equals(consignmentItemMovement.Id)))
                    orderItem.ConsignmentItemMovements.Add(consignmentItemMovement);
            }

            incomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;
            incomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;
            incomeItem.ProductCapitalizationItem = productCapitalizationItem;

            if (toReturn.Any(x => x.ProductIncomeItemId.Equals(incomeItem.Id)))
                toReturn.First(x => x.ProductIncomeItemId.Equals(incomeItem.Id)).ProductIncomeItem = incomeItem;

            return incomeItem;
        };

        _connection.Query(
            "SELECT * FROM [ProductIncomeItem] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].[ID] = [ProductIncomeItem].[SaleReturnItemID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].[ID] = [ProductIncomeItem].[ProductCapitalizationItemID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [SaleReturnItem].[OrderItemID] " +
            "LEFT JOIN [ConsignmentItemMovement] " +
            "ON [ConsignmentItemMovement].[OrderItemID] = [OrderItem].[ID] " +
            "WHERE [ProductIncomeItem].[Deleted] = 0 " +
            "AND [ProductIncomeItem].[ID] IN @Ids ",
            types, mapper,
            new { Ids = toReturn.Select(x => x.ProductIncomeItemId) });

        return toReturn;
    }

    public decimal GetPriceForReSaleByConsignmentItemId(long id) {
        return _connection.Query<decimal>(
            "SELECT " +
            "TOP 1 [dbo].GetGovExchangedToUahValue( " +
            "[ConsignmentItem].[AccountingPrice] " +
            ", (SELECT TOP 1 [Currency].[ID] " +
            "FROM [Currency] " +
            "WHERE [Currency].[Code] = 'EUR' " +
            "AND [Currency].[Deleted] = 0) " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN [SupplyOrderUkraine].[InvDate] " +
            "ELSE " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL " +
            "THEN [SupplyInvoice].[Created] " +
            "ELSE [SupplyInvoice].[DateCustomDeclaration] " +
            "END " +
            "END " +
            ")) " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "WHERE [ConsignmentItem].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public ConsignmentItem GetId(long consignmentItemId) {
        return _connection.Query<ConsignmentItem>(
            "SELECT * " +
            "FROM [ConsignmentItem] " +
            "WHERE [ConsignmentItem].ID = @ID",
            new {
                ID = consignmentItemId
            }
        ).SingleOrDefault();
    }
}