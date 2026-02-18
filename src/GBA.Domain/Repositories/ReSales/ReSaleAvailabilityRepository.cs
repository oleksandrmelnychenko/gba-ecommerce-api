using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Repositories.ReSales.Contracts;

namespace GBA.Domain.Repositories.ReSales;

public sealed class ReSaleAvailabilityRepository : IReSaleAvailabilityRepository {
    private readonly IDbConnection _connection;

    public ReSaleAvailabilityRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ReSaleAvailability item) {
        return _connection.Query<long>(
            "INSERT INTO [ReSaleAvailability] " +
            "([Qty], [RemainingQty], [ConsignmentItemID], [ProductAvailabilityID], [OrderItemID], [Updated], [ExchangeRate], [PricePerItem], [ProductReservationID], [ProductTransferItemID], [DepreciatedOrderItemID], [SupplyReturnItemID]) " +
            "VALUES " +
            "(@Qty, @RemainingQty, @ConsignmentItemId, @ProductAvailabilityId, @OrderItemId, GETUTCDATE(), @ExchangeRate, @PricePerItem, @ProductReservationId, @ProductTransferItemId, @DepreciatedOrderItemId, @SupplyReturnItemId); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).FirstOrDefault();
    }

    public void Update(ReSaleAvailability item) {
        _connection.Execute(
            "UPDATE [ReSaleAvailability] " +
            "SET [Qty] = @Qty, [RemainingQty] = @RemainingQty, [InvoiceQty] = @InvoiceQty, [Updated] = GETUTCDATE(), [ExchangeRate] = @ExchangeRate, " +
            "[ProductReservationID] = @ProductReservationId " +
            "WHERE ID = @Id",
            item
        );
    }

    public void Delete(long id) {
        _connection.Execute(
            "UPDATE [ReSaleAvailability] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public ReSaleAvailabilityWithTotalsModel GetAllItemsFiltered(
        decimal extraChargePercent,
        IEnumerable<long> includedProductGroups,
        IEnumerable<long> includedStorages,
        IEnumerable<string> includedSpecificationCodes,
        string search,
        Guid? selectStorageNetId = null) {
        ReSaleAvailabilityWithTotalsModel toReturn = new();

        List<GroupingReSaleAvailabilityModel> groupReSaleAvailabilities = new();

        Type[] types = {
            typeof(ConsignmentItem),
            typeof(ReSaleAvailability),
            typeof(ProductSpecification),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Organization),
            typeof(Storage)
        };

        Func<object[], ConsignmentItem> mapper = objects => {
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[0];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[1];
            ProductSpecification productSpecification = (ProductSpecification)objects[2];
            ProductAvailability productAvailability = (ProductAvailability)objects[3];
            Storage storage = (Storage)objects[4];
            Product product = (Product)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];
            Organization organization = (Organization)objects[7];
            Storage fromStorage = (Storage)objects[8];

            consignmentItem.FromStorage = fromStorage;
            consignmentItem.ProductSpecification = productSpecification;
            product.MeasureUnit = measureUnit;
            consignmentItem.Product = product;
            reSaleAvailability.Product = product;
            reSaleAvailability.ConsignmentItem = consignmentItem;
            storage.Organization = organization;
            productAvailability.Storage = storage;
            consignmentItem.ProductAvailability = productAvailability;
            reSaleAvailability.ProductAvailability = productAvailability;

            if (!groupReSaleAvailabilities.Any(x => x.ProductId.Equals(product.Id) &&
                                                    x.FromStorage != null &&
                                                    x.FromStorage.Id.Equals(fromStorage.Id))) {
                decimal salePrice = decimal.Round(
                    reSaleAvailability.PricePerItem
                    +
                    reSaleAvailability.PricePerItem * extraChargePercent / 100,
                    14,
                    MidpointRounding.AwayFromZero
                );

                decimal convertRemainingQty = Convert.ToDecimal(reSaleAvailability.RemainingQty);

                decimal totalAccountingPrice = reSaleAvailability.PricePerItem * convertRemainingQty;
                decimal totalSalePrice = salePrice * convertRemainingQty;

                groupReSaleAvailabilities.Add(
                    new GroupingReSaleAvailabilityModel {
                        Qty = reSaleAvailability.RemainingQty,
                        ConsignmentItems = new List<ConsignmentItem> { consignmentItem },
                        FromStorage = fromStorage,
                        MeasureUnit = measureUnit.Name,
                        ProductGroup = product.ProductGroupNames,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        SalePrice = salePrice,
                        SpecificationCode = consignmentItem.ProductSpecification?.SpecificationCode ?? string.Empty,
                        VendorCode = product.VendorCode,
                        AccountingGrossPrice = reSaleAvailability.PricePerItem,
                        TotalAccountingPrice = totalAccountingPrice,
                        TotalSalePrice = totalSalePrice,
                        Weight = consignmentItem.Weight,
                        ExchangeRate = consignmentItem.ExchangeRate
                    });
            } else {
                GroupingReSaleAvailabilityModel reSaleAvailabilityModel =
                    groupReSaleAvailabilities.First(x => x.ProductId.Equals(product.Id) &&
                                                         x.FromStorage.Id.Equals(fromStorage.Id));

                if (!reSaleAvailabilityModel.ConsignmentItems.Any(x => x.Id.Equals(consignmentItem.Id)))
                    reSaleAvailabilityModel.ConsignmentItems.Add(consignmentItem);
                else
                    consignmentItem = reSaleAvailabilityModel.ConsignmentItems.First(x => x.Id.Equals(consignmentItem.Id));

                double qty = reSaleAvailabilityModel.Qty + reSaleAvailability.RemainingQty;

                decimal convertRemainingQty = Convert.ToDecimal(qty);

                decimal totalAccountingPrice = reSaleAvailabilityModel.AccountingGrossPrice * convertRemainingQty;
                decimal totalSalePrice = reSaleAvailabilityModel.SalePrice * convertRemainingQty;

                reSaleAvailabilityModel.Qty = qty;
                reSaleAvailabilityModel.TotalAccountingPrice = totalAccountingPrice;
                reSaleAvailabilityModel.TotalSalePrice = totalSalePrice;
            }

            if (!consignmentItem.ReSaleAvailabilities.Any(x => x.Id.Equals(reSaleAvailability.Id)))
                consignmentItem.ReSaleAvailabilities.Add(reSaleAvailability);
            else
                reSaleAvailability = consignmentItem.ReSaleAvailabilities.First(x => x.Id.Equals(reSaleAvailability.Id));

            return consignmentItem;
        };

        string specificationCodeCond = "AND ([ProductSpecification].[SpecificationCode] IN @IncludeSpecificationCodes ";

        if (!includedSpecificationCodes.Any())
            return toReturn;

        if (includedSpecificationCodes.Last().Equals(string.Empty))
            specificationCodeCond += "OR ([ProductSpecification].[SpecificationCode] IS NULL " +
                                     "OR [ConsignmentItem].[ProductSpecificationID] IS NULL " +
                                     "OR [ProductSpecification].[SpecificationCode] = '') ";

        specificationCodeCond += ") ";

        string searchCond = string.Empty;

        if (!string.IsNullOrEmpty(search))
            searchCond += "AND ( " +
                          "[Product].[VendorCode] LIKE '%' + @Search + '%' OR " +
                          "[Product].[Name] LIKE '%' + @Search + '%' OR " +
                          "[Product].[NameUA] LIKE '%' + @Search + '%' OR " +
                          "[Product].[SearchName] LIKE '%' + @Search + '%' OR " +
                          "[Product].[SearchNameUA] LIKE '%' + @Search + '%' OR " +
                          "[Product].[Description] LIKE '%' + @Search + '%' OR " +
                          "[Product].[DescriptionUA] LIKE '%' + @Search + '%' OR " +
                          "[Product].[SearchDescription] LIKE '%' + @Search + '%' OR " +
                          "[Product].[SearchDescriptionUA] LIKE '%' + @Search + '%' " +
                          ") ";

        string automaticallyCond = string.Empty;

        if (selectStorageNetId.HasValue) automaticallyCond += " AND [FromStorage].[NetUID] = @SelectStorageNetId ";

        _connection.Query(
            "SELECT " +
            "[ConsignmentItem].* " +
            ", [ReSaleAvailability].* " +
            ", [ProductSpecification].* " +
            ", [ProductAvailability].* " +
            ", [Storage].* " +
            ", [Product].* " +
            ", [ProductGroup].[FullName] AS [ProductGroupNames] " +
            ", [MeasureUnit].* " +
            ", [Organization].* " +
            ", [FromStorage].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = Product.ID " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductProductGroup].ProductGroupID = [ProductGroup].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Storage].[OrganizationID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].[ID] = [Consignment].[StorageID] " +
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
            "WHERE [ReSaleAvailability].Deleted = 0 " +
            "AND [ReSaleAvailability].RemainingQty > 0 " +
            "AND [FromStorage].[ID] IN @IncludedStorages " +
            "AND ProductProductGroup.Deleted = 0 " +
            "AND [ProductGroup].ID IN @IncludedProductGroups " +
            specificationCodeCond +
            searchCond +
            automaticallyCond +
            "ORDER BY " +
            "CASE " +
            "WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN [SupplyOrderUkraine].[InvDate] " +
            "ELSE " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END " +
            "END ",
            types, mapper,
            new {
                IncludedProductGroups = includedProductGroups,
                IncludedStorages = includedStorages,
                IncludeSpecificationCodes = includedSpecificationCodes,
                Search = search,
                SelectStorageNetId = selectStorageNetId
            });

        if (groupReSaleAvailabilities.Any()) {
            IEnumerable<ReSaleAvailabilityUsedQty> existReSaleItems =
                _connection.Query<ReSaleAvailabilityUsedQty>(
                    "SELECT " +
                    "[ReSaleItem].[ProductID] " +
                    ", [ReSale].[FromStorageID] " +
                    ", SUM( " +
                    "CASE " +
                    "WHEN [ReSaleItem].[Qty] IS NOT NULL " +
                    "THEN [ReSaleItem].[Qty] " +
                    "ELSE 0 " +
                    "END) AS [Qty] " +
                    "FROM [ReSale] " +
                    "LEFT JOIN [ReSaleItem] " +
                    "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
                    "WHERE [ReSale].[ChangedToInvoice] IS NULL " +
                    "AND [ReSale].[Deleted] = 0 " +
                    "AND [ReSaleItem].[Deleted] = 0 " +
                    "AND [ReSaleItem].[Qty] > 0 " +
                    "AND [ReSaleItem].[ReSaleAvailabilityID] IS NULL " +
                    "AND [ReSaleItem].[ProductID] IN @ProductIds " +
                    "AND [ReSale].[FromStorageID] IN @FromStorageIds " +
                    "GROUP BY [ReSale].[FromStorageID] " +
                    ", [ReSaleItem].[ProductID] ",
                    new {
                        ProductIds = groupReSaleAvailabilities.Select(x => x.ProductId),
                        FromStorageIds = groupReSaleAvailabilities.Select(x => x.FromStorage.Id)
                    });

            foreach (GroupingReSaleAvailabilityModel groupReSaleAvailability in
                     groupReSaleAvailabilities) {
                ReSaleAvailabilityUsedQty usedQty =
                    existReSaleItems
                        .FirstOrDefault(x => x.ProductId.Equals(groupReSaleAvailability.ProductId) &&
                                             x.FromStorageId.Equals(groupReSaleAvailability.FromStorage.Id));

                if (usedQty != null) {
                    groupReSaleAvailability.Qty -= usedQty.Qty;

                    decimal convertRemainingQty = Convert.ToDecimal(groupReSaleAvailability.Qty);

                    decimal totalAccountingPrice = groupReSaleAvailability.AccountingGrossPrice * convertRemainingQty;
                    decimal totalSalePrice = groupReSaleAvailability.SalePrice * convertRemainingQty;

                    groupReSaleAvailability.Qty = groupReSaleAvailability.Qty;
                    groupReSaleAvailability.TotalAccountingPrice = totalAccountingPrice;
                    groupReSaleAvailability.TotalSalePrice = totalSalePrice;
                }
            }
        }

        toReturn.GroupReSaleAvailabilities = groupReSaleAvailabilities.Where(x => x.Qty > 0);

        return toReturn;
    }

    public ReSaleAvailabilityWithTotalsModel GetAllItemsForExport(DateTime from, DateTime to) {
        ReSaleAvailabilityWithTotalsModel toReturn = new();

        List<GroupingReSaleAvailabilityModel> groupReSaleAvailabilities = new();

        Type[] types = {
            typeof(ConsignmentItem),
            typeof(ReSaleAvailability),
            typeof(ProductSpecification),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Organization),
            typeof(Storage)
        };

        Func<object[], ConsignmentItem> mapper = objects => {
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[0];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[1];
            ProductSpecification productSpecification = (ProductSpecification)objects[2];
            ProductAvailability productAvailability = (ProductAvailability)objects[3];
            Storage storage = (Storage)objects[4];
            Product product = (Product)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];
            Organization organization = (Organization)objects[7];
            Storage fromStorage = (Storage)objects[8];

            consignmentItem.FromStorage = fromStorage;
            consignmentItem.ProductSpecification = productSpecification;
            product.MeasureUnit = measureUnit;
            consignmentItem.Product = product;
            reSaleAvailability.Product = product;
            reSaleAvailability.ConsignmentItem = consignmentItem;
            storage.Organization = organization;
            productAvailability.Storage = storage;
            consignmentItem.ProductAvailability = productAvailability;
            reSaleAvailability.ProductAvailability = productAvailability;

            if (!groupReSaleAvailabilities.Any(x => x.ProductId.Equals(product.Id) &&
                                                    x.FromStorage != null &&
                                                    x.FromStorage.Id.Equals(fromStorage.Id))) {
                decimal salePrice = decimal.Round(
                    reSaleAvailability.PricePerItem,
                    14,
                    MidpointRounding.AwayFromZero
                );

                decimal convertRemainingQty = Convert.ToDecimal(reSaleAvailability.RemainingQty);

                decimal totalAccountingPrice = reSaleAvailability.PricePerItem * convertRemainingQty;
                decimal totalSalePrice = salePrice * convertRemainingQty;

                groupReSaleAvailabilities.Add(
                    new GroupingReSaleAvailabilityModel {
                        Qty = reSaleAvailability.RemainingQty,
                        ConsignmentItems = new List<ConsignmentItem> { consignmentItem },
                        FromStorage = fromStorage,
                        MeasureUnit = measureUnit.Name,
                        ProductGroup = product.ProductGroupNames,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        SalePrice = salePrice,
                        SpecificationCode = consignmentItem.ProductSpecification?.SpecificationCode ?? string.Empty,
                        VendorCode = product.VendorCode,
                        AccountingGrossPrice = reSaleAvailability.PricePerItem,
                        TotalAccountingPrice = totalAccountingPrice,
                        TotalSalePrice = totalSalePrice,
                        Weight = consignmentItem.Weight,
                        ExchangeRate = consignmentItem.ExchangeRate
                    });
            } else {
                GroupingReSaleAvailabilityModel reSaleAvailabilityModel =
                    groupReSaleAvailabilities.First(x => x.ProductId.Equals(product.Id) &&
                                                         x.FromStorage.Id.Equals(fromStorage.Id));

                if (!reSaleAvailabilityModel.ConsignmentItems.Any(x => x.Id.Equals(consignmentItem.Id)))
                    reSaleAvailabilityModel.ConsignmentItems.Add(consignmentItem);
                else
                    consignmentItem = reSaleAvailabilityModel.ConsignmentItems.First(x => x.Id.Equals(consignmentItem.Id));

                double qty = reSaleAvailabilityModel.Qty + reSaleAvailability.RemainingQty;

                decimal convertRemainingQty = Convert.ToDecimal(qty);

                decimal totalAccountingPrice = reSaleAvailabilityModel.AccountingGrossPrice * convertRemainingQty;
                decimal totalSalePrice = reSaleAvailabilityModel.SalePrice * convertRemainingQty;

                reSaleAvailabilityModel.Qty = qty;
                reSaleAvailabilityModel.TotalAccountingPrice = totalAccountingPrice;
                reSaleAvailabilityModel.TotalSalePrice = totalSalePrice;
            }

            if (!consignmentItem.ReSaleAvailabilities.Any(x => x.Id.Equals(reSaleAvailability.Id)))
                consignmentItem.ReSaleAvailabilities.Add(reSaleAvailability);
            else
                reSaleAvailability = consignmentItem.ReSaleAvailabilities.First(x => x.Id.Equals(reSaleAvailability.Id));

            return consignmentItem;
        };

        _connection.Query(
            "SELECT " +
            "[ConsignmentItem].* " +
            ", [ReSaleAvailability].* " +
            ", [ProductSpecification].* " +
            ", [ProductAvailability].* " +
            ", [Storage].* " +
            ", [Product].* " +
            ", [ProductGroup].[FullName] AS [ProductGroupNames] " +
            ", [MeasureUnit].* " +
            ", [Organization].* " +
            ", [FromStorage].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = Product.ID " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductProductGroup].ProductGroupID = [ProductGroup].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Storage].[OrganizationID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].[ID] = [Consignment].[StorageID] " +
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
            "WHERE [ReSaleAvailability].Deleted = 0 " +
            "AND [ReSaleAvailability].RemainingQty > 0 " +
            "AND [ReSaleAvailability].Created >= @From " +
            "AND [ReSaleAvailability].Created <= @To " +
            "AND ProductProductGroup.Deleted = 0 " +
            "ORDER BY " +
            "CASE " +
            "WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN [SupplyOrderUkraine].[InvDate] " +
            "ELSE " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END " +
            "END ",
            types, mapper,
            new {
                From = from,
                To = to
            });

        if (groupReSaleAvailabilities.Any()) {
            IEnumerable<ReSaleAvailabilityUsedQty> existReSaleItems =
                _connection.Query<ReSaleAvailabilityUsedQty>(
                    "SELECT " +
                    "[ReSaleItem].[ProductID] " +
                    ", [ReSale].[FromStorageID] " +
                    ", SUM( " +
                    "CASE " +
                    "WHEN [ReSaleItem].[Qty] IS NOT NULL " +
                    "THEN [ReSaleItem].[Qty] " +
                    "ELSE 0 " +
                    "END) AS [Qty] " +
                    "FROM [ReSale] " +
                    "LEFT JOIN [ReSaleItem] " +
                    "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
                    "WHERE [ReSale].[ChangedToInvoice] IS NULL " +
                    "AND [ReSale].[Deleted] = 0 " +
                    "AND [ReSaleItem].[Deleted] = 0 " +
                    "AND [ReSaleItem].[Qty] > 0 " +
                    "AND [ReSaleItem].[ReSaleAvailabilityID] IS NULL " +
                    "AND [ReSaleItem].[ProductID] IN @ProductIds " +
                    "AND [ReSale].[FromStorageID] IN @FromStorageIds " +
                    "GROUP BY [ReSale].[FromStorageID] " +
                    ", [ReSaleItem].[ProductID] ",
                    new {
                        ProductIds = groupReSaleAvailabilities.Select(x => x.ProductId),
                        FromStorageIds = groupReSaleAvailabilities.Select(x => x.FromStorage.Id)
                    });

            foreach (GroupingReSaleAvailabilityModel groupReSaleAvailability in
                     groupReSaleAvailabilities) {
                ReSaleAvailabilityUsedQty usedQty =
                    existReSaleItems
                        .FirstOrDefault(x => x.ProductId.Equals(groupReSaleAvailability.ProductId) &&
                                             x.FromStorageId.Equals(groupReSaleAvailability.FromStorage.Id));

                if (usedQty != null) {
                    groupReSaleAvailability.Qty -= usedQty.Qty;

                    decimal convertRemainingQty = Convert.ToDecimal(groupReSaleAvailability.Qty);

                    decimal totalAccountingPrice = groupReSaleAvailability.AccountingGrossPrice * convertRemainingQty;
                    decimal totalSalePrice = groupReSaleAvailability.SalePrice * convertRemainingQty;

                    groupReSaleAvailability.Qty = groupReSaleAvailability.Qty;
                    groupReSaleAvailability.TotalAccountingPrice = totalAccountingPrice;
                    groupReSaleAvailability.TotalSalePrice = totalSalePrice;
                }
            }
        }

        toReturn.GroupReSaleAvailabilities = groupReSaleAvailabilities.Where(x => x.Qty > 0);

        return toReturn;
    }

    public ReSaleAvailabilityWithTotalsModel GetActualReSaleAvailabilityByProductId(long productId) {
        ReSaleAvailabilityWithTotalsModel toReturn = new();

        List<GroupingReSaleAvailabilityModel> groupReSaleAvailabilities = new();

        Type[] types = {
            typeof(ConsignmentItem),
            typeof(ReSaleAvailability),
            typeof(ProductSpecification),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Organization),
            typeof(Storage)
        };

        Func<object[], ConsignmentItem> mapper = objects => {
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[0];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[1];
            ProductSpecification productSpecification = (ProductSpecification)objects[2];
            ProductAvailability productAvailability = (ProductAvailability)objects[3];
            Storage storage = (Storage)objects[4];
            Product product = (Product)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];
            Organization organization = (Organization)objects[7];
            Storage fromStorage = (Storage)objects[8];

            consignmentItem.FromStorage = fromStorage;
            consignmentItem.ProductSpecification = productSpecification;
            product.MeasureUnit = measureUnit;
            consignmentItem.Product = product;
            reSaleAvailability.Product = product;
            reSaleAvailability.ConsignmentItem = consignmentItem;
            storage.Organization = organization;
            productAvailability.Storage = storage;
            consignmentItem.ProductAvailability = productAvailability;
            reSaleAvailability.ProductAvailability = productAvailability;

            if (!groupReSaleAvailabilities.Any(x => x.ProductId.Equals(product.Id) &&
                                                    x.FromStorage != null &&
                                                    x.FromStorage.Id.Equals(fromStorage.Id))) {
                groupReSaleAvailabilities.Add(
                    new GroupingReSaleAvailabilityModel {
                        Qty = reSaleAvailability.RemainingQty,
                        ConsignmentItems = new List<ConsignmentItem> { consignmentItem },
                        FromStorage = fromStorage,
                        MeasureUnit = measureUnit.Name,
                        ProductGroup = product.ProductGroupNames,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        SpecificationCode = consignmentItem.ProductSpecification?.SpecificationCode ?? string.Empty,
                        VendorCode = product.VendorCode,
                        AccountingGrossPrice = reSaleAvailability.PricePerItem,
                        Weight = consignmentItem.Weight,
                        ExchangeRate = consignmentItem.ExchangeRate
                    });
            } else {
                GroupingReSaleAvailabilityModel reSaleAvailabilityModel =
                    groupReSaleAvailabilities.First(x => x.ProductId.Equals(product.Id) &&
                                                         x.FromStorage.Id.Equals(fromStorage.Id));

                if (!reSaleAvailabilityModel.ConsignmentItems.Any(x => x.Id.Equals(consignmentItem.Id)))
                    reSaleAvailabilityModel.ConsignmentItems.Add(consignmentItem);
                else
                    consignmentItem = reSaleAvailabilityModel.ConsignmentItems.First(x => x.Id.Equals(consignmentItem.Id));

                double qty = reSaleAvailabilityModel.Qty + reSaleAvailability.RemainingQty;

                decimal convertRemainingQty = Convert.ToDecimal(qty);

                decimal totalAccountingPrice = reSaleAvailabilityModel.AccountingGrossPrice * convertRemainingQty;
                decimal totalSalePrice = reSaleAvailabilityModel.SalePrice * convertRemainingQty;

                reSaleAvailabilityModel.Qty = qty;
                reSaleAvailabilityModel.TotalAccountingPrice = totalAccountingPrice;
                reSaleAvailabilityModel.TotalSalePrice = totalSalePrice;
            }

            if (!consignmentItem.ReSaleAvailabilities.Any(x => x.Id.Equals(reSaleAvailability.Id)))
                consignmentItem.ReSaleAvailabilities.Add(reSaleAvailability);
            else
                reSaleAvailability = consignmentItem.ReSaleAvailabilities.First(x => x.Id.Equals(reSaleAvailability.Id));

            return consignmentItem;
        };

        _connection.Query(
            "SELECT " +
            "[ConsignmentItem].* " +
            ", [ReSaleAvailability].* " +
            ", [ProductSpecification].* " +
            ", [ProductAvailability].* " +
            ", [Storage].* " +
            ", [Product].* " +
            ", [ProductGroup].[FullName] AS [ProductGroupNames] " +
            ", [MeasureUnit].* " +
            ", [Organization].* " +
            ", [FromStorage].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = Product.ID " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductProductGroup].ProductGroupID = [ProductGroup].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Storage].[OrganizationID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].[ID] = [Consignment].[StorageID] " +
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
            "WHERE [ReSaleAvailability].Deleted = 0 " +
            "AND Product.ID = @ProductId " +
            "AND [ReSaleAvailability].RemainingQty > 0 " +
            "AND ProductProductGroup.Deleted = 0 " +
            "ORDER BY " +
            "CASE " +
            "WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN [SupplyOrderUkraine].[InvDate] " +
            "ELSE " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END " +
            "END ",
            types, mapper,
            new {
                ProductId = productId
            });

        if (groupReSaleAvailabilities.Any()) {
            IEnumerable<ReSaleAvailabilityUsedQty> existReSaleItems =
                _connection.Query<ReSaleAvailabilityUsedQty>(
                    "SELECT " +
                    "[ReSaleItem].[ProductID] " +
                    ", [ReSale].[FromStorageID] " +
                    ", SUM( " +
                    "CASE " +
                    "WHEN [ReSaleItem].[Qty] IS NOT NULL " +
                    "THEN [ReSaleItem].[Qty] " +
                    "ELSE 0 " +
                    "END) AS [Qty] " +
                    "FROM [ReSale] " +
                    "LEFT JOIN [ReSaleItem] " +
                    "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
                    "WHERE [ReSale].[ChangedToInvoice] IS NULL " +
                    "AND [ReSale].[Deleted] = 0 " +
                    "AND [ReSaleItem].[Deleted] = 0 " +
                    "AND [ReSaleItem].[Qty] > 0 " +
                    "AND [ReSaleItem].[ReSaleAvailabilityID] IS NULL " +
                    "AND [ReSaleItem].[ProductID] IN @ProductIds " +
                    "AND [ReSale].[FromStorageID] IN @FromStorageIds " +
                    "GROUP BY [ReSale].[FromStorageID] " +
                    ", [ReSaleItem].[ProductID] ",
                    new {
                        ProductIds = groupReSaleAvailabilities.Select(x => x.ProductId),
                        FromStorageIds = groupReSaleAvailabilities.Select(x => x.FromStorage.Id)
                    });

            foreach (GroupingReSaleAvailabilityModel groupReSaleAvailability in
                     groupReSaleAvailabilities) {
                ReSaleAvailabilityUsedQty usedQty =
                    existReSaleItems
                        .FirstOrDefault(x => x.ProductId.Equals(groupReSaleAvailability.ProductId) &&
                                             x.FromStorageId.Equals(groupReSaleAvailability.FromStorage.Id));

                if (usedQty != null) {
                    groupReSaleAvailability.Qty -= usedQty.Qty;

                    decimal convertRemainingQty = Convert.ToDecimal(groupReSaleAvailability.Qty);

                    decimal totalAccountingPrice = groupReSaleAvailability.AccountingGrossPrice * convertRemainingQty;
                    decimal totalSalePrice = groupReSaleAvailability.SalePrice * convertRemainingQty;

                    groupReSaleAvailability.Qty = groupReSaleAvailability.Qty;
                    groupReSaleAvailability.TotalAccountingPrice = totalAccountingPrice;
                    groupReSaleAvailability.TotalSalePrice = totalSalePrice;
                }
            }
        }

        toReturn.GroupReSaleAvailabilities = groupReSaleAvailabilities.Where(x => x.Qty > 0);

        return toReturn;
    }

    public IEnumerable<ReSaleAvailability> GetAllForSignal() {
        return _connection.Query<ReSaleAvailability, ConsignmentItem, ProductAvailability, Storage, Product, ReSaleAvailability>(
            "SELECT * " +
            "FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "WHERE [ReSaleAvailability].Deleted = 0 " +
            "AND [ReSaleAvailability].Qty > 0 ",
            (reSaleAvailability, consignmentItem, productAvailability, storage, product) => {
                consignmentItem.Product = product;
                productAvailability.Product = product;
                reSaleAvailability.Product = product;
                reSaleAvailability.ConsignmentItem = consignmentItem;
                productAvailability.Storage = storage;
                reSaleAvailability.ProductAvailability = productAvailability;
                reSaleAvailability.PricePerItem = consignmentItem.AccountingPrice;

                return reSaleAvailability;
            }
        );
    }

    public int? GetTotalProductQtyFromReSaleAvailabilitiesByProductId(long productId) {
        return _connection.Query<int?>(
            "SELECT SUM([ReSaleAvailability].[RemainingQty]) AS [TotalQty] FROM [ReSaleAvailability] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ReSaleAvailability].ProductAvailabilityID = [ProductAvailability].ID " +
            "WHERE [ReSaleAvailability].Deleted = 0 " +
            "AND [ProductAvailability].ProductID = @ProductId ",
            new { ProductId = productId }).FirstOrDefault();
    }

    public void UpdateRemainingQty(ReSaleAvailability item) {
        _connection.Execute(
            "UPDATE [ReSaleAvailability] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [RemainingQty] = @RemainingQty " +
            ", [InvoiceQty] = @InvoiceQty " +
            "WHERE [ReSaleAvailability].[ID] = @Id; ",
            item);
    }

    public IEnumerable<string> GetAllReSaleAvailabilitySpecificationCodes() {
        return _connection.Query<string>(
            "SELECT " +
            "[ProductSpecification].[SpecificationCode] " +
            "FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [ReSaleAvailability].[Deleted] = 0 " +
            "AND [ReSaleAvailability].[RemainingQty] > 0 " +
            "AND [ProductSpecification].[SpecificationCode] IS NOT NULL " +
            "AND [ProductSpecification].[SpecificationCode] != '' " +
            "GROUP BY [ProductSpecification].[SpecificationCode] ");
    }

    public ReSaleAvailability GetById(long id) {
        return _connection.Query<ReSaleAvailability>(
            "SELECT * FROM [ReSaleAvailability] " +
            "WHERE [ReSaleAvailability].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public void RestoreReSaleAvailability(long id) {
        _connection.Execute(
            "UPDATE [ReSaleAvailability] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Deleted] = 0 " +
            "WHERE [ReSaleAvailability].[ID] = @Id; ",
            new { Id = id });
    }

    public ReSaleAvailability GetByProductReservationId(long productReservationId) {
        return _connection.Query<ReSaleAvailability>(
            "SELECT [ReSaleAvailability].* FROM [ReSaleAvailability] " +
            "LEFT JOIN [ProductReservation] " +
            "ON [ReSaleAvailability].ProductReservationID = [ProductReservation].ID " +
            "WHERE [ProductReservation].ID = @ProductReservationId ",
            new { ProductReservationId = productReservationId }).FirstOrDefault();
    }

    public IEnumerable<ReSaleAvailability> GetByProductAndStorageIds(
        long productId,
        long[] storageIds) {
        return _connection.Query<ReSaleAvailability, ConsignmentItem, Consignment, ProductSpecification, ReSaleAvailability>(
            "SELECT " +
            "[ReSaleAvailability].* " +
            ", [ConsignmentItem].* " +
            ", [Consignment].* " +
            ", [ProductSpecification].* " +
            "FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [Consignment].[StorageID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [ReSaleAvailability].[Deleted] = 0 " +
            "AND [ReSaleAvailability].[RemainingQty] > 0 " +
            "AND [Storage].[ForVatProducts] = 1 " +
            "AND [ConsignmentItem].[ProductID] = @ProductId " +
            "AND [Consignment].[StorageID] IN @StorageIds " +
            "ORDER BY " +
            "CASE " +
            "WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN [SupplyOrderUkraine].[InvDate] " +
            "ELSE " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL " +
            "THEN [SupplyInvoice].[Created] " +
            "ELSE [SupplyInvoice].[DateCustomDeclaration] " +
            "END " +
            "END ",
            (reSaleAvailability, consignmentItem, consignment, productSpecification) => {
                consignmentItem.ProductSpecification = productSpecification;
                consignmentItem.Consignment = consignment;
                reSaleAvailability.ConsignmentItem = consignmentItem;
                return reSaleAvailability;
            },
            new { ProductId = productId, StorageIds = storageIds });
    }

    public IEnumerable<ReSaleAvailability> GetByProductAndStorageId(
        long productId,
        long storageId) {
        return _connection.Query<ReSaleAvailability, ConsignmentItem, Consignment, ProductSpecification, ReSaleAvailability>(
            "SELECT " +
            "[ReSaleAvailability].* " +
            ", [ConsignmentItem].* " +
            ", [Consignment].* " +
            ", [ProductSpecification].* " +
            "FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [Consignment].[StorageID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [ReSaleAvailability].[Deleted] = 0 " +
            "AND [ReSaleAvailability].[RemainingQty] > 0 " +
            "AND [Storage].[ForVatProducts] = 1 " +
            "AND [ConsignmentItem].[ProductID] = @ProductId " +
            "AND [Consignment].[StorageID] = @StorageId " +
            "ORDER BY " +
            "CASE " +
            "WHEN [SupplyInvoice].[ID] IS NULL " +
            "THEN [SupplyOrderUkraine].[InvDate] " +
            "ELSE " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NULL " +
            "THEN [SupplyInvoice].[Created] " +
            "ELSE [SupplyInvoice].[DateCustomDeclaration] " +
            "END " +
            "END ",
            (reSaleAvailability, consignmentItem, consignment, productSpecification) => {
                consignmentItem.ProductSpecification = productSpecification;
                consignmentItem.Consignment = consignment;
                reSaleAvailability.ConsignmentItem = consignmentItem;
                return reSaleAvailability;
            },
            new { ProductId = productId, StorageId = storageId });
    }

    public IEnumerable<ReSaleAvailability> GetByConsignmentItemIds(long[] consignmentItemIds) {
        return _connection.Query<ReSaleAvailability>(
            "SELECT * FROM [ReSaleAvailability] " +
            "WHERE [ReSaleAvailability].[ConsignmentItemID] IN @ConsignmentItemIds; ",
            new { ConsignmentItemIds = consignmentItemIds });
    }

    public IEnumerable<ReSaleAvailability> GetExistByProductId(
        long productId) {
        return _connection.Query<ReSaleAvailability, ConsignmentItem, ReSaleAvailability>(
            "SELECT * FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "WHERE [ConsignmentItem].[ProductID]  = @ProductId " +
            "AND [ReSaleAvailability].[Deleted] = 0 " +
            "AND [ReSaleAvailability].[RemainingQty] > 0; ",
            (reSaleAvailability, consignmentItem) => {
                reSaleAvailability.ConsignmentItem = consignmentItem;
                return reSaleAvailability;
            },
            new { ProductId = productId });
    }
}