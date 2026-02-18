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
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.PackingLists;

public sealed class PackingListPackageOrderItemRepository : IPackingListPackageOrderItemRepository {
    private readonly IDbConnection _connection;

    public PackingListPackageOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PackingListPackageOrderItem packingListPackageOrderItem) {
        return _connection.Query<long>(
            "INSERT INTO [PackingListPackageOrderItem] (Qty, SupplyInvoiceOrderItemId, PackingListPackageId, PackingListId, IsPlaced, IsErrorInPlaced, " +
            "IsReadyToPlaced, UnitPrice, GrossWeight, NetWeight, UploadedQty, Placement, UnitPriceEur, GrossUnitPriceEur, ContainerUnitPriceEur, ExchangeRateAmount, " +
            "VatPercent, VatAmount, PlacedQty, Updated, AccountingGrossUnitPriceEur, AccountingContainerUnitPriceEur, [AccountingGeneralGrossUnitPriceEur], [ExchangeRateAmountUahToEur], " +
            "[DeliveryPerItem], [ProductIsImported]) " +
            "VALUES (@Qty, @SupplyInvoiceOrderItemId, @PackingListPackageId, @PackingListId, 0, 0, 0, @UnitPrice, @GrossWeight, @NetWeight, @UploadedQty, @Placement, " +
            "@UnitPriceEur, @GrossUnitPriceEur, @ContainerUnitPriceEur, @ExchangeRateAmount, @VatPercent, @VatAmount, 0, getutcdate(), @AccountingGrossUnitPriceEur, " +
            "@AccountingContainerUnitPriceEur, @AccountingGeneralGrossUnitPriceEur, @ExchangeRateAmountUahToEur, @DeliveryPerItem, @ProductIsImported); " +
            "SELECT SCOPE_IDENTITY()",
            packingListPackageOrderItem
        ).Single();
    }

    public void Add(IEnumerable<PackingListPackageOrderItem> packingListPackageOrderItems) {
        _connection.Execute(
            "INSERT INTO [PackingListPackageOrderItem] (Qty, SupplyInvoiceOrderItemId, PackingListPackageId, PackingListId, IsPlaced, IsErrorInPlaced, " +
            "IsReadyToPlaced, UnitPrice, GrossWeight, NetWeight, UploadedQty, Placement, UnitPriceEur, GrossUnitPriceEur, ContainerUnitPriceEur, ExchangeRateAmount, " +
            "VatPercent, VatAmount, PlacedQty, Updated, AccountingGrossUnitPriceEur, AccountingContainerUnitPriceEur, [AccountingGeneralGrossUnitPriceEur], [ExchangeRateAmountUahToEur], " +
            "[DeliveryPerItem], [ProductIsImported]) " +
            "VALUES (@Qty, @SupplyInvoiceOrderItemId, @PackingListPackageId, @PackingListId, 0, 0, 0, @UnitPrice, @GrossWeight, @NetWeight, @UploadedQty, @Placement, " +
            "@UnitPriceEur, @GrossUnitPriceEur, @ContainerUnitPriceEur, @ExchangeRateAmount, @VatPercent, @VatAmount, 0, getutcdate(), @AccountingGrossUnitPriceEur, " +
            "@AccountingContainerUnitPriceEur, @AccountingGeneralGrossUnitPriceEur, @ExchangeRateAmountUahToEur, @DeliveryPerItem, @ProductIsImported)",
            packingListPackageOrderItems
        );
    }

    public void Update(PackingListPackageOrderItem packingListPackageOrderItem) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Qty = @Qty, SupplyInvoiceOrderItemId = @SupplyInvoiceOrderItemId, PackingListPackageId = @PackingListPackageId, PackingListId = @PackingListId, " +
            "IsPlaced = @IsPlaced, IsErrorInPlaced = @IsErrorInPlaced, IsReadyToPlaced = @IsReadyToPlaced, GrossUnitPriceEur = @GrossUnitPriceEur, " +
            "UploadedQty = @UploadedQty, Placement = @Placement, UnitPriceEur = @UnitPriceEur, NetWeight = @NetWeight, GrossWeight = @GrossWeight, " +
            "ContainerUnitPriceEur = @ContainerUnitPriceEur, ExchangeRateAmount = @ExchangeRateAmount, Updated = getutcdate(), AccountingGrossUnitPriceEur = @AccountingGrossUnitPriceEur, " +
            "AccountingContainerUnitPriceEur = @AccountingContainerUnitPriceEur, [AccountingGeneralGrossUnitPriceEur] = @AccountingGeneralGrossUnitPriceEur, " +
            "[VatAmount] = @VatAmount, [VatPercent] = @VatPercent, [ExchangeRateAmountUahToEur] = @ExchangeRateAmountUahToEur, [DeliveryPerItem] = @DeliveryPerItem, " +
            "[ProductIsImported] = @ProductIsImported " +
            "WHERE [PackingListPackageOrderItem].NetUID = @NetUid",
            packingListPackageOrderItem
        );
    }

    public void Update(IEnumerable<PackingListPackageOrderItem> packingListPackageOrderItems) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Qty = @Qty, SupplyInvoiceOrderItemId = @SupplyInvoiceOrderItemId, PackingListPackageId = @PackingListPackageId, PackingListId = @PackingListId, " +
            "IsPlaced = @IsPlaced, IsErrorInPlaced = @IsErrorInPlaced, IsReadyToPlaced = @IsReadyToPlaced, GrossUnitPriceEur = @GrossUnitPriceEur, " +
            "UploadedQty = @UploadedQty, Placement = @Placement, UnitPriceEur = @UnitPriceEur, NetWeight = @NetWeight, GrossWeight = @GrossWeight, " +
            "ContainerUnitPriceEur = @ContainerUnitPriceEur, ExchangeRateAmount = @ExchangeRateAmount, Updated = getutcdate(), AccountingGrossUnitPriceEur = @AccountingGrossUnitPriceEur, " +
            "AccountingContainerUnitPriceEur = @AccountingContainerUnitPriceEur, [AccountingGeneralGrossUnitPriceEur] = @AccountingGeneralGrossUnitPriceEur, " +
            "[VatAmount] = @VatAmount, [VatPercent] = @VatPercent, [ExchangeRateAmountUahToEur] = @ExchangeRateAmountUahToEur, [DeliveryPerItem] = @DeliveryPerItem, " +
            "[ProductIsImported] = @ProductIsImported " +
            "WHERE [PackingListPackageOrderItem].NetUID = @NetUid",
            packingListPackageOrderItems
        );
    }

    public void UpdateVatPercent(IEnumerable<PackingListPackageOrderItem> packingListPackageOrderItems) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET VatPercent = @VatPercent " +
            "WHERE ID = @Id",
            packingListPackageOrderItems
        );
    }

    public void UpdatePackingListId(IEnumerable<long> ids, long packingListId) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET PackingListID = @PackingListId, Updated = GETUTCDATE() " +
            "WHERE PackingListID IN @Ids",
            new { Ids = ids, PackingListId = packingListId }
        );
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [PackingListPackageOrderItem].ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPackageId(long packageId) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingListPackageOrderItem].PackingListPackageID = @PackageId",
            new { PackageId = packageId }
        );
    }

    public void RemoveAllByPackageIdExceptProvided(long packageId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingListPackageOrderItem].PackingListPackageID = @PackageId AND [PackingListPackageOrderItem].ID NOT IN @Ids",
            new { PackageId = packageId, Ids = ids }
        );
    }

    public void RemoveAllByPackingListId(long packingListId) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingListPackageOrderItem].PackingListID = @PackingListId",
            new { PackingListId = packingListId }
        );
    }

    public void RemoveAllByPackingListIdExceptProvided(long packingListId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingListPackageOrderItem].PackingListID = @PackingListId AND [PackingListPackageOrderItem].ID NOT IN @Ids",
            new { PackingListId = packingListId, Ids = ids }
        );
    }

    public void SetIsReadyToPlacedByNetId(Guid netId, bool value) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET IsReadyToPlaced = @Value " +
            "WHERE NetUID = @NetId",
            new { NetId = netId, Value = value }
        );
    }

    public void SetIsReadyToPlacedByPackingListNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET IsReadyToPlaced = 1 " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "WHERE [PackingList].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void SetIsPlacedByIds(IEnumerable<long> ids, bool value) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET IsPlaced = @Value, RemainingQty = Qty " +
            "WHERE ID IN @Ids",
            new { Ids = ids, Value = value }
        );
    }

    public void SetIsPlacedOnlyByIds(IEnumerable<long> ids, bool value) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET IsPlaced = @Value " +
            "WHERE ID IN @Ids",
            new { Ids = ids, Value = value }
        );
    }

    public void UpdateRemainingQty(PackingListPackageOrderItem packingListPackageOrderItem) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET RemainingQty = @RemainingQty " +
            "WHERE ID = @Id",
            packingListPackageOrderItem
        );
    }

    public void UpdateRemainingQty(long id, double toAddQty) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET RemainingQty = RemainingQty + @ToAddQty " +
            "WHERE ID = @Id",
            new { Id = id, ToAddQty = toAddQty }
        );
    }

    public IEnumerable<PackingListPackageOrderItem> GetAllNotPlacedBySupplyInvoiceOrderItemId(long supplyInvoiceOrderItemId) {
        return _connection.Query<PackingListPackageOrderItem>(
            "SELECT * " +
            "FROM [PackingListPackageOrderItem] " +
            "WHERE [PackingListPackageOrderItem].Deleted = 0 " +
            "AND [PackingListPackageOrderItem].Qty <> [PackingListPackageOrderItem].PlacedQty " +
            "AND [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = @SupplyInvoiceOrderItemId",
            new { SupplyInvoiceOrderItemId = supplyInvoiceOrderItemId }
        );
    }

    public IEnumerable<PackingListPackageOrderItem> GetAllArrivedItemsByProductIdOrderedByWriteOffRuleType(
        long productId,
        long storageId,
        ProductWriteOffRuleType writeOffRuleType,
        string supplierName = ""
    ) {
        string sqlExpression =
            "SELECT [PackingListPackageOrderItem].* " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "AND [ProductIncome].StorageID = @StorageId " +
            "AND [SupplyInvoiceOrderItem].ProductID = @ProductId " +
            "AND [PackingListPackageOrderItem].RemainingQty <> 0 ";

        if (!string.IsNullOrEmpty(supplierName)) {
            sqlExpression +=
                "ORDER BY CASE WHEN [Client].FullName = @SupplierName THEN 0 ELSE 1 END";

            switch (writeOffRuleType) {
                case ProductWriteOffRuleType.ByWeight:
                    sqlExpression += ", [PackingListPackageOrderItem].GrossWeight DESC";
                    break;
                case ProductWriteOffRuleType.ByPrice:
                    sqlExpression += ", [PackingListPackageOrderItem].UnitPrice DESC";
                    break;
                case ProductWriteOffRuleType.ByFromDate:
                    sqlExpression += ", [PackingListPackageOrderItem].Created";
                    break;
            }
        } else {
            switch (writeOffRuleType) {
                case ProductWriteOffRuleType.ByWeight:
                    sqlExpression += "ORDER BY [PackingListPackageOrderItem].GrossWeight DESC";
                    break;
                case ProductWriteOffRuleType.ByPrice:
                    sqlExpression += "ORDER BY [PackingListPackageOrderItem].UnitPrice DESC";
                    break;
                case ProductWriteOffRuleType.ByFromDate:
                    sqlExpression += "ORDER BY [PackingListPackageOrderItem].Created";
                    break;
            }
        }

        return _connection.Query<PackingListPackageOrderItem>(
            sqlExpression,
            new { ProductId = productId, StorageId = storageId, SupplierName = supplierName }
        );
    }

    public IEnumerable<PackingListPackageOrderItem> GetAllArrivedItemsByProductIdWithSupplierOrderedByWriteOffRuleType(
        long productId,
        long storageId,
        ProductWriteOffRuleType writeOffRuleType,
        string supplierName = ""
    ) {
        string sqlExpression =
            "SELECT [PackingListPackageOrderItem].* " +
            ", [Client].* " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "AND [ProductIncome].StorageID = @StorageId " +
            "AND [SupplyInvoiceOrderItem].ProductID = @ProductId " +
            "AND [PackingListPackageOrderItem].RemainingQty <> 0 ";

        if (!string.IsNullOrEmpty(supplierName)) {
            sqlExpression +=
                "ORDER BY CASE WHEN [Client].FullName = @SupplierName THEN 0 ELSE 1 END";

            switch (writeOffRuleType) {
                case ProductWriteOffRuleType.ByWeight:
                    sqlExpression += ", [PackingListPackageOrderItem].GrossWeight DESC";
                    break;
                case ProductWriteOffRuleType.ByPrice:
                    sqlExpression += ", [PackingListPackageOrderItem].UnitPrice DESC";
                    break;
                case ProductWriteOffRuleType.ByFromDate:
                    sqlExpression += ", [PackingListPackageOrderItem].Created";
                    break;
            }
        } else {
            switch (writeOffRuleType) {
                case ProductWriteOffRuleType.ByWeight:
                    sqlExpression += "ORDER BY [PackingListPackageOrderItem].GrossWeight DESC";
                    break;
                case ProductWriteOffRuleType.ByPrice:
                    sqlExpression += "ORDER BY [PackingListPackageOrderItem].UnitPrice DESC";
                    break;
                case ProductWriteOffRuleType.ByFromDate:
                    sqlExpression += "ORDER BY [PackingListPackageOrderItem].Created";
                    break;
            }
        }

        return _connection.Query<PackingListPackageOrderItem, Client, PackingListPackageOrderItem>(
            sqlExpression,
            (item, supplier) => {
                item.Supplier = supplier;

                return item;
            },
            new { ProductId = productId, StorageId = storageId, SupplierName = supplierName }
        );
    }

    public List<PackingListPackageOrderItem> GetAllPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        long limit,
        long offset,
        string value,
        DateTime from,
        DateTime to
    ) {
        List<PackingListPackageOrderItem> items = new();

        string sqlExpression =
            "; WITH [Search_CTE] " +
            "AS (" +
            "SELECT [PackingListPackageOrderItem].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [SupplyInvoice].DateFrom DESC) AS [RowNumber] " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID ";

        sqlExpression +=
            "WHERE [PackingListPackageOrderItem].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId " +
            "AND [SupplyInvoice].DateFrom >= @From " +
            "AND [SupplyInvoice].DateFrom <= @To " +
            "AND [PackingList].IsPlaced = 1 " +
            "AND [Product].SearchVendorCode like N'%' + @Value + N'%' ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "AND [Client].NetUID = @SupplierNetId ";

        sqlExpression +=
            ") " +
            "SELECT * " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [PackingListPackageOrderItem].ID = [ProductPlacement].ID " +
            "AND [ProductPlacement].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "WHERE [PackingListPackageOrderItem].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ") " +
            "AND (" +
            "[Storage].NetUID = @StorageNetId " +
            "OR " +
            "[Storage].NetUID IS NULL " +
            ")";

        Type[] types = {
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(ProductPlacement),
            typeof(Storage)
        };

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        Func<object[], PackingListPackageOrderItem> mapper = objects => {
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[0];
            PackingList packingList = (PackingList)objects[1];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[2];
            Client supplier = (Client)objects[4];
            SupplyInvoiceOrderItem invoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            ProductPlacement productPlacement = (ProductPlacement)objects[8];

            if (!items.Any(i => i.Id.Equals(item.Id))) {
                if (productPlacement != null) product.ProductPlacements.Add(productPlacement);

                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                invoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                invoiceOrderItem.Product = product;

                packingList.SupplyInvoice = supplyInvoice;

                item.PackingList = packingList;
                item.SupplyInvoiceOrderItem = invoiceOrderItem;
                item.Supplier = supplier;

                items.Add(item);
            } else if (productPlacement != null) {
                items.First(i => i.Id.Equals(item.Id)).SupplyInvoiceOrderItem.Product.ProductPlacements.Add(productPlacement);
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

    public List<PackingListPackageOrderItem> GetRemainingInfoByProductId(long productId) {
        List<PackingListPackageOrderItem> items = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [SupplyOrder].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [PackingListPackageOrderItem].Deleted = 0 " +
            "AND [Product].ID = @ProductId " +
            "AND [PackingList].Deleted = 0 " +
            "AND [PackingList].IsPlaced = 1 " +
            "AND [PackingListPackageOrderItem].RemainingQty <> 0";

        Type[] types = {
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(ProductIncomeItem),
            typeof(ProductIncome),
            typeof(Storage),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        Func<object[], PackingListPackageOrderItem> mapper = objects => {
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[0];
            PackingList packingList = (PackingList)objects[1];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[2];
            SupplyOrder supplyOrder = (SupplyOrder)objects[3];
            Client supplier = (Client)objects[4];
            SupplyInvoiceOrderItem invoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[8];
            ProductIncome productIncome = (ProductIncome)objects[9];
            Storage storage = (Storage)objects[10];
            ClientAgreement clientAgreement = (ClientAgreement)objects[11];
            Agreement agreement = (Agreement)objects[12];
            Currency currency = (Currency)objects[13];

            product.Name =
                isPlCulture
                    ? product.NameUA
                    : product.NameUA;

            if (supplyOrderItem != null)
                supplyOrderItem.Product = product;

            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            supplier.ClientAgreements.Add(clientAgreement);

            invoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            invoiceOrderItem.Product = product;

            supplyInvoice.SupplyOrder = supplyOrder;

            packingList.SupplyInvoice = supplyInvoice;

            productIncome.Storage = storage;

            productIncomeItem.ProductIncome = productIncome;

            item.SupplyInvoiceOrderItem = invoiceOrderItem;
            item.ProductIncomeItem = productIncomeItem;
            item.PackingList = packingList;
            item.Supplier = supplier;

            items.Add(item);

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

    public decimal GetTotalEuroAmountForPlacedItemsByStorage(Guid storageNetId) {
        return _connection.Query<decimal>(
                "SELECT " +
                "ROUND(" +
                "SUM(" +
                "ROUND(" +
                " [PackingListPackageOrderItem].GrossUnitPriceEur * [PackingListPackageOrderItem].RemainingQty " +
                ", 2)" +
                ")" +
                ", 2) AS [TotalAmount] " +
                "FROM [PackingListPackageOrderItem] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
                "LEFT JOIN [ProductIncome] " +
                "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
                "WHERE [PackingListPackageOrderItem].Deleted = 0 " +
                "AND [PackingList].Deleted = 0 " +
                "AND [PackingList].IsPlaced = 1 " +
                "AND [Storage].NetUID = @StorageNetId ",
                new { StorageNetId = storageNetId }
            )
            .Single();
    }

    public decimal GetTotalEuroAmountForPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        DateTime from,
        DateTime to
    ) {
        string sqlExpression =
            "SELECT " +
            "ROUND(" +
            "SUM(" +
            "ROUND(" +
            " [PackingListPackageOrderItem].GrossUnitPriceEur * [PackingListPackageOrderItem].RemainingQty " +
            ", 2)" +
            ")" +
            ", 2) AS [TotalAmount] " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID ";

        sqlExpression +=
            "WHERE [PackingListPackageOrderItem].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId " +
            "AND [SupplyInvoice].DateFrom >= @From " +
            "AND [SupplyInvoice].DateFrom <= @To " +
            "AND [PackingList].Deleted = 0 " +
            "AND [PackingList].IsPlaced = 1 " +
            "AND [Product].SearchVendorCode like N'%' + @Value + N'%' ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "AND [Client].NetUID = @SupplierNetId ";

        return
            _connection.Query<decimal>(
                    sqlExpression,
                    new {
                        StorageNetId = storageNetId,
                        SupplierNetId = supplierNetId,
                        Value = value,
                        From = from,
                        To = to
                    }
                )
                .Single();
    }

    public PackingListPackageOrderItem GetById(long id) {
        return _connection.Query<PackingListPackageOrderItem>(
                "SELECT * " +
                "FROM [PackingListPackageOrderItem] " +
                "WHERE [PackingListPackageOrderItem].ID = @Id",
                new { Id = id }
            )
            .Single();
    }

    public PackingListPackageOrderItem GetByIdWithIncludesForProduct(long id) {
        return _connection.Query<PackingListPackageOrderItem, SupplyInvoiceOrderItem, SupplyOrderItem, Product, PackingListPackageOrderItem>(
                "SELECT * " +
                "FROM [PackingListPackageOrderItem] " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
                "WHERE [PackingListPackageOrderItem].ID = @Id",
                (packListItem, invoiceOrderItem, orderItem, product) => {
                    if (orderItem != null)
                        orderItem.Product = product;

                    invoiceOrderItem.SupplyOrderItem = orderItem;
                    invoiceOrderItem.Product = product;

                    packListItem.SupplyInvoiceOrderItem = invoiceOrderItem;

                    return packListItem;
                },
                new { Id = id }
            )
            .Single();
    }

    public void UpdatePlacementInformation(PackingListPackageOrderItem item) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Qty = @Qty, PlacedQty = @PlacedQty, RemainingQty = @RemainingQty " +
            "WHERE ID = @Id",
            item
        );
    }

    public void UpdatePlacementInformation(long id, double qty) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET PlacedQty = PlacedQty + @Qty, RemainingQty = RemainingQty + @Qty " +
            "WHERE ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public PackingListPackageOrderItem GetByNetId(Guid netId) {
        return _connection.Query<PackingListPackageOrderItem>(
                "SELECT * " +
                "FROM [PackingListPackageOrderItem] " +
                "WHERE [PackingListPackageOrderItem].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public PackingListPackageOrderItem GetByIdForPlacement(long id) {
        PackingListPackageOrderItem toReturn = null;

        _connection.Query<PackingListPackageOrderItem, ProductPlacement, SupplyInvoiceOrderItem, SupplyOrderItem, Product, PackingListPackageOrderItem>(
            "SELECT * " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "WHERE [PackingListPackageOrderItem].ID = @Id",
            (item, placement, invoiceItem, orderItem, product) => {
                if (toReturn != null) {
                    if (placement != null) toReturn.ProductPlacements.Add(placement);
                } else {
                    if (placement != null) item.ProductPlacements.Add(placement);

                    orderItem.Product = product;

                    invoiceItem.SupplyOrderItem = orderItem;

                    item.SupplyInvoiceOrderItem = invoiceItem;

                    toReturn = item;
                }

                return item;
            },
            new { Id = id }
        );

        return toReturn;
    }
}