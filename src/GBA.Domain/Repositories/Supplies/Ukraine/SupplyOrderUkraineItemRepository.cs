using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkraineItemRepository : ISupplyOrderUkraineItemRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkraineItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public SupplyOrderUkraineItem GetById(long id) {
        return _connection.Query<SupplyOrderUkraineItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineItem] " +
            "WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineItem GetNotOrderedItemByActReconciliationItemIdIfExists(long id) {
        return _connection.Query<SupplyOrderUkraineItem>(
            "SELECT TOP(1) [SupplyOrderUkraineItem].* " +
            "FROM [ActReconciliationItem] " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [SupplyOrderUkraineItem].ProductID = [ActReconciliationItem].ProductID " +
            "AND [SupplyOrderUkraineItem].Deleted = 0 " +
            "WHERE [ActReconciliationItem].ID = @Id " +
            "AND [SupplyOrderUkraineItem].NotOrdered = 1",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineItem GetByRefIdsIfExists(long productId, long orderUkraineId, long supplierId) {
        return _connection.Query<SupplyOrderUkraineItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineItem] " +
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineItem].NotOrdered = 0 " +
            "AND [SupplyOrderUkraineItem].ProductID = @ProductId " +
            "AND [SupplyOrderUkraineItem].SupplyOrderUkraineID = @OrderUkraineId " +
            "AND [SupplyOrderUkraineItem].SupplierID = @SupplierId",
            new { ProductId = productId, OrderUkraineId = orderUkraineId, SupplierId = supplierId }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineItem GetNotOrderedByRefIdsIfExists(long productId, long orderUkraineId, long supplierId) {
        return _connection.Query<SupplyOrderUkraineItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineItem] " +
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineItem].NotOrdered = 1 " +
            "AND [SupplyOrderUkraineItem].ProductID = @ProductId " +
            "AND [SupplyOrderUkraineItem].SupplyOrderUkraineID = @OrderUkraineId " +
            "AND [SupplyOrderUkraineItem].SupplierID = @SupplierId",
            new { ProductId = productId, OrderUkraineId = orderUkraineId, SupplierId = supplierId }
        ).SingleOrDefault();
    }

    public long Add(SupplyOrderUkraineItem item) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyOrderUkraineItem] " +
            "(IsFullyPlaced, Qty, PlacedQty, NetWeight, UnitPrice, ProductId, SupplyOrderUkraineId, RemainingQty, NotOrdered, SupplierId, GrossUnitPrice, " +
            "UnitPriceLocal, GrossUnitPriceLocal, PackingListPackageOrderItemId, ExchangeRateAmount, ConsignmentItemId, Updated, AccountingGrossUnitPrice, " +
            "[GrossWeight], [ProductSpecificationID], [VatPercent], [VatAmount], [VatAmountLocal], [AccountingGrossUnitPriceLocal], [UnitDeliveryAmount], " +
            "[UnitDeliveryAmountLocal], [ProductIsImported]) " +
            "VALUES " +
            "(@IsFullyPlaced, @Qty, @PlacedQty, @NetWeight, @UnitPrice, @ProductId, @SupplyOrderUkraineId, 0.00, @NotOrdered, @SupplierId, @GrossUnitPrice, " +
            "@UnitPriceLocal, @GrossUnitPriceLocal, @PackingListPackageOrderItemId, @ExchangeRateAmount, @ConsignmentItemId, GETUTCDATE(), @AccountingGrossUnitPrice," +
            "@GrossWeight, @ProductSpecificationId, @VatPercent, @VatAmount, @VatAmountLocal, @AccountingGrossUnitPriceLocal, @UnitDeliveryAmount, " +
            "@UnitDeliveryAmountLocal, @ProductIsImported); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public void Add(IEnumerable<SupplyOrderUkraineItem> items) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkraineItem] " +
            "(IsFullyPlaced, Qty, PlacedQty, NetWeight, UnitPrice, ProductId, SupplyOrderUkraineId, RemainingQty, NotOrdered, SupplierId, GrossUnitPrice, " +
            "UnitPriceLocal, GrossUnitPriceLocal, PackingListPackageOrderItemId, ExchangeRateAmount, ConsignmentItemId, Updated, AccountingGrossUnitPrice, " +
            "[GrossWeight], [ProductSpecificationID], [VatPercent], [VatAmount], [VatAmountLocal], [AccountingGrossUnitPriceLocal], [UnitDeliveryAmount], " +
            "[UnitLocal], [ProductIsImported]) " +
            "VALUES " +
            "(@IsFullyPlaced, @Qty, @PlacedQty, @NetWeight, @UnitPrice, @ProductId, @SupplyOrderUkraineId, 0.00, @NotOrdered, @SupplierId, @GrossUnitPrice, " +
            "@UnitPriceLocal, @GrossUnitPriceLocal, @PackingListPackageOrderItemId, @ExchangeRateAmount, @ConsignmentItemId, GETUTCDATE(), @AccountingGrossUnitPrice, " +
            "@GrossWeight, @ProductSpecificationId, @VatPercent, @VatAmount, @VatAmountLocal, @AccountingGrossUnitPriceLocal, @UnitDeliveryAmount, " +
            "@UnitDeliveryAmountLocal, @ProductIsImported)",
            items
        );
    }

    public void Update(SupplyOrderUkraineItem item) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET Qty = @Qty, NetWeight = @NetWeight, UnitPrice = @UnitPrice, GrossUnitPrice = @GrossUnitPrice, UnitPriceLocal = @UnitPriceLocal, " +
            "GrossUnitPriceLocal = @GrossUnitPriceLocal, ExchangeRateAmount = @ExchangeRateAmount, Updated = GETUTCDATE(), AccountingGrossUnitPrice = @AccountingGrossUnitPrice, " +
            "[GrossWeight] = @GrossWeight, [ProductSpecificationID] = @ProductSpecificationId, [VatPercent] = @VatPercent, [VatAmount] = @VatAmount, " +
            "[VatAmountLocal] = @VatAmountLocal, [AccountingGrossUnitPriceLocal] = @AccountingGrossUnitPriceLocal, [UnitDeliveryAmount] = @UnitDeliveryAmount, " +
            "[UnitDeliveryAmountLocal] = @UnitDeliveryAmountLocal, [ProductIsImported] = @ProductIsImported " +
            "WHERE ID = @Id",
            item
        );
    }

    public void Update(IEnumerable<SupplyOrderUkraineItem> items) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET Qty = @Qty, NetWeight = @NetWeight, UnitPrice = @UnitPrice, GrossUnitPrice = @GrossUnitPrice, UnitPriceLocal = @UnitPriceLocal, " +
            "GrossUnitPriceLocal = @GrossUnitPriceLocal, ExchangeRateAmount = @ExchangeRateAmount, Updated = GETUTCDATE(), AccountingGrossUnitPrice = @AccountingGrossUnitPrice, " +
            "[GrossWeight] = @GrossWeight, [ProductSpecificationID] = @ProductSpecificationId, [VatPercent] = @VatPercent, [VatAmount] = @VatAmount, " +
            "[VatAmountLocal] = @VatAmountLocal, [AccountingGrossUnitPriceLocal] = @AccountingGrossUnitPriceLocal, [UnitDeliveryAmount] = @UnitDeliveryAmount, " +
            "[UnitDeliveryAmountLocal] = @UnitDeliveryAmountLocal, [ProductIsImported] = @ProductIsImported " +
            "WHERE ID = @Id",
            items
        );
    }

    public void UpdateWeightAndPrice(IEnumerable<SupplyOrderUkraineItem> items) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET NetWeight = @NetWeight, UnitPrice = @UnitPrice, GrossUnitPrice = @GrossUnitPrice, UnitPriceLocal = @UnitPriceLocal, " +
            "GrossUnitPriceLocal = @GrossUnitPriceLocal, Updated = GETUTCDATE(), AccountingGrossUnitPrice = @AccountingGrossUnitPrice, " +
            "[GrossWeight] = @GrossWeight " +
            "WHERE ID = @Id",
            items
        );
    }

    public void UpdatePlacementInformation(SupplyOrderUkraineItem item) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET IsFullyPlaced = @IsFullyPlaced, Qty = @Qty, PlacedQty = @PlacedQty, RemainingQty = @RemainingQty " +
            "WHERE ID = @Id",
            item
        );
    }

    public void IncreasePlacementInfoById(long id, double qty) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET PlacedQty = PlacedQty + @Qty, RemainingQty = RemainingQty + @Qty " +
            "WHERE ID = @Id; " +
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET IsFullyPlaced = IIF(PlacedQty = Qty, 1, 0) " +
            "WHERE ID = @Id ",
            new { Id = id, Qty = qty }
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByOrderUkraineIdExceptProvided(long orderUkraineId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE SupplyOrderUkraineID = @OrderUkraineId " +
            "AND ID NOT IN @Ids " +
            "AND NotOrdered = 1",
            new { OrderUkraineId = orderUkraineId, Ids = ids }
        );
    }

    public decimal GetTotalEuroAmountForPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        DateTime from,
        DateTime to) {
        string sqlExpression =
            "; WITH [Total_CTE] " +
            "AS (" +
            "SELECT " +
            "ROUND([SupplyOrderUkraineItem].GrossUnitPrice * SUM([ProductIncomeItem].RemainingQty), 2) AS [TotalAmount] " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID ";

        sqlExpression +=
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraine].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "AND [Supplier].NetUID = @SupplierNetId ";

        sqlExpression +=
            "AND [SupplyOrderUkraine].FromDate >= @From " +
            "AND [SupplyOrderUkraine].FromDate <= @To " +
            "AND [Product].SearchVendorCode like N'%' + @Value + N'%' " +
            "GROUP BY [SupplyOrderUkraineItem].ID, [SupplyOrderUkraineItem].GrossUnitPrice" +
            ")" +
            "SELECT " +
            "ISNULL(" +
            "ROUND(" +
            "SUM([Total_CTE].TotalAmount)" +
            ", 2)" +
            ", 0) " +
            "FROM [Total_CTE]";

        return _connection.Query<decimal>(
            sqlExpression,
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId,
                Value = value,
                From = from,
                To = to
            }
        ).Single();
    }

    public decimal GetTotalEuroAmountForPlacedItemsByStorage(Guid storageNetId) {
        return _connection.Query<decimal>(
            "; WITH [Total_CTE] " +
            "AS (" +
            "SELECT " +
            "ROUND([SupplyOrderUkraineItem].GrossUnitPrice * SUM([ProductIncomeItem].RemainingQty), 2) AS [TotalAmount] " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraine].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId " +
            "GROUP BY [SupplyOrderUkraineItem].ID, [SupplyOrderUkraineItem].GrossUnitPrice" +
            ")" +
            "SELECT " +
            "ISNULL(" +
            "ROUND(" +
            "SUM([Total_CTE].TotalAmount)" +
            ", 2) " +
            ", 0) " +
            "FROM [Total_CTE]",
            new { StorageNetId = storageNetId }
        ).Single();
    }

    public IEnumerable<SupplyOrderUkraineItem> GetAllPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        long limit,
        long offset,
        string value,
        DateTime from,
        DateTime to) {
        List<SupplyOrderUkraineItem> items = new();

        string sqlExpression =
            "; WITH [Search_CTE] " +
            "AS (" +
            "SELECT [SupplyOrderUkraineItem].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [SupplyOrderUkraine].FromDate DESC) AS [RowNumber] " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID ";

        sqlExpression +=
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraine].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "AND [Supplier].NetUID = @SupplierNetId ";

        sqlExpression +=
            "AND [SupplyOrderUkraine].FromDate >= @From " +
            "AND [SupplyOrderUkraine].FromDate <= @To " +
            "AND [Product].SearchVendorCode like N'%' + @Value + N'%' " +
            "GROUP BY [SupplyOrderUkraineItem].ID, [SupplyOrderUkraine].FromDate" +
            ") " +
            "SELECT * " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [SupplyOrderUkraineItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "WHERE [SupplyOrderUkraineItem].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ") " +
            "AND (" +
            "[Storage].NetUID = @StorageNetId " +
            "OR " +
            "[Storage].NetUID IS NULL" +
            ")";

        Type[] types = {
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Client),
            typeof(PackingListPackageOrderItem),
            typeof(Product),
            typeof(ProductIncomeItem),
            typeof(ProductPlacement),
            typeof(Storage)
        };

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        Func<object[], SupplyOrderUkraineItem> mapper = objects => {
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[0];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[1];
            Client supplier = (Client)objects[2];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[3];
            Product product = (Product)objects[4];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[5];
            ProductPlacement productPlacement = (ProductPlacement)objects[6];
            Storage storage = (Storage)objects[7];

            if (!items.Any(i => i.Id.Equals(item.Id))) {
                if (productPlacement != null) {
                    productPlacement.Storage = storage;

                    product.ProductPlacements.Add(productPlacement);
                }

                if (productIncomeItem != null) item.ProductIncomeItems.Add(productIncomeItem);

                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                item.Product = product;
                item.SupplyOrderUkraine = supplyOrderUkraine;
                item.PackingListPackageOrderItem = packingListPackageOrderItem;
                item.Supplier = supplier;

                items.Add(item);
            } else {
                SupplyOrderUkraineItem itemFromDb = items.First(i => i.Id.Equals(item.Id));

                if (productPlacement != null && !itemFromDb.Product.ProductPlacements.Any(p => p.Id.Equals(productPlacement.Id)))
                    itemFromDb.Product.ProductPlacements.Add(productPlacement);

                if (productIncomeItem != null && !itemFromDb.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) itemFromDb.ProductIncomeItems.Add(productIncomeItem);
            }

            return item;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId,
                Limit = limit,
                Offset = offset,
                Value = value,
                From = from,
                To = to
            }
        );

        return items;
    }

    public IEnumerable<SupplyOrderUkraineItem> GetRemainingInfoByProductId(long productId) {
        List<SupplyOrderUkraineItem> items = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [SupplyOrderUkraineItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [SupplyOrderUkraine].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [Product].ID = @ProductId " +
            "AND [ProductIncomeItem].RemainingQty <> 0";

        Type[] types = {
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Client),
            typeof(PackingListPackageOrderItem),
            typeof(Product),
            typeof(ProductIncomeItem),
            typeof(ProductIncome),
            typeof(Storage),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        Func<object[], SupplyOrderUkraineItem> mapper = objects => {
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[0];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[1];
            Client supplier = (Client)objects[2];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[3];
            Product product = (Product)objects[4];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[5];
            ProductIncome productIncome = (ProductIncome)objects[6];
            Storage storage = (Storage)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Currency currency = (Currency)objects[10];

            if (!items.Any(i => i.Id.Equals(item.Id))) {
                productIncome.Storage = storage;

                productIncomeItem.ProductIncome = productIncome;

                item.ProductIncomeItems.Add(productIncomeItem);

                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                supplyOrderUkraine.ClientAgreement = clientAgreement;

                item.Product = product;
                item.SupplyOrderUkraine = supplyOrderUkraine;
                item.PackingListPackageOrderItem = packingListPackageOrderItem;
                item.Supplier = supplier;

                items.Add(item);
            } else {
                SupplyOrderUkraineItem itemFromDb = items.First(i => i.Id.Equals(item.Id));

                if (!itemFromDb.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) {
                    productIncome.Storage = storage;

                    productIncomeItem.ProductIncome = productIncome;

                    itemFromDb.ProductIncomeItems.Add(productIncomeItem);
                }
            }

            return item;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { ProductId = productId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return items;
    }

    public void UpdateGrossPrice(IEnumerable<SupplyOrderUkraineItem> items) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET [Updated] = getutcdate() " +
            ", GrossUnitPrice = @GrossUnitPrice" +
            ", GrossUnitPriceLocal = @GrossUnitPriceLocal" +
            ", AccountingGrossUnitPrice = @AccountingGrossUnitPrice " +
            ", AccountingGrossUnitPriceLocal = @AccountingGrossUnitPriceLocal " +
            ", UnitDeliveryAmount = @UnitDeliveryAmount " +
            ", UnitDeliveryAmountLocal = @UnitDeliveryAmountLocal " +
            "WHERE ID = @Id",
            items);
    }

    public Currency GetCurrencyFromOrderByItemId(long id) {
        return _connection.Query<Currency>(
            "SELECT TOP 1 [Currency].* FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [SupplyOrderUkraineItem].[ID] = @Id ",
            new { Id = id }).FirstOrDefault();
    }

    public SupplyOrderUkraine GetOrderByItemId(long id) {
        return _connection.Query<SupplyOrderUkraine>(
            "SELECT TOP 1 [SupplyOrderUkraine].* FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "WHERE [SupplyOrderUkraineItem].[ID] = @Id ",
            new { Id = id }).FirstOrDefault();
    }

    public void UpdateRemainingQty(SupplyOrderUkraineItem item) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineItem] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [RemainingQty] = @RemainingQty " +
            "WHERE [SupplyOrderUkraineItem].[ID] = @Id ",
            item);
    }
}