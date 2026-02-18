using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.TotalDashboards.SupplyInvoices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyInvoiceRepository : ISupplyInvoiceRepository {
    private readonly IDbConnection _connection;

    public SupplyInvoiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyInvoice supplyInvoice) {
        return _connection.Query<long>(
                "INSERT INTO SupplyInvoice (SupplyOrderID, Number, NetPrice, IsShipped, DateFrom, PaymentTo, ServiceNumber, Comment, IsPartiallyPlaced, " +
                "IsFullyPlaced, Updated, DeliveryAmount, DiscountAmount) " +
                "VALUES(@SupplyOrderID, @Number, @NetPrice, @IsShipped, @DateFrom, @PaymentTo, @ServiceNumber, @Comment, 0, 0, getutcdate(), @DeliveryAmount, " +
                "@DiscountAmount); " +
                "SELECT SCOPE_IDENTITY()",
                supplyInvoice
            )
            .Single();
    }

    public void Add(IEnumerable<SupplyInvoice> supplyInvoices) {
        _connection.Execute(
            "INSERT INTO SupplyInvoice (SupplyOrderID, Number, NetPrice, IsShipped, DateFrom, PaymentTo, ServiceNumber, Comment, IsPartiallyPlaced, " +
            "IsFullyPlaced, Updated, DeliveryAmount, DiscountAmount) " +
            "VALUES(@SupplyOrderID, @Number, @NetPrice, @IsShipped, @DateFrom, @PaymentTo, @ServiceNumber, @Comment, 0, 0, getutcdate(), " +
            "@DeliveryAmount, @DiscountAmount)",
            supplyInvoices
        );
    }

    public void Update(SupplyInvoice supplyInvoice) {
        _connection.Execute(
            "UPDATE SupplyInvoice SET SupplyOrderID = @SupplyOrderID, Number = @Number, NetPrice = @NetPrice, IsShipped = @IsShipped, DateFrom = @DateFrom, " +
            "PaymentTo = @PaymentTo, Comment = @Comment, Updated = getutcdate(), " +
            "DeliveryAmount = @DeliveryAmount, DiscountAmount = @DiscountAmount " +
            "WHERE NetUID = @NetUID",
            supplyInvoice
        );
    }

    public void Update(IEnumerable<SupplyInvoice> supplyInvoices) {
        _connection.Execute(
            "UPDATE SupplyInvoice SET SupplyOrderID = @SupplyOrderID, Number = @Number, NetPrice = @NetPrice, IsShipped = @IsShipped, DateFrom = @DateFrom, " +
            "PaymentTo = @PaymentTo, Comment = @Comment, Updated = getutcdate(), " +
            "DeliveryAmount = @DeliveryAmount, DiscountAmount = @DiscountAmount " +
            "WHERE NetUID = @NetUID",
            supplyInvoices
        );
    }

    public void UpdatePlacementInfo(IEnumerable<SupplyInvoice> supplyInvoices) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET IsPartiallyPlaced = @IsPartiallyPlaced, IsFullyPlaced = @IsFullyPlaced, Updated = getutcdate() " +
            "WHERE ID = @Id",
            supplyInvoices
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE SupplyInvoice SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Merge(Guid netId, long rootId) {
        _connection.Execute(
            "UPDATE SupplyInvoice SET Deleted = 1, RootSupplyInvoiceID = @RootId WHERE NetUID = @NetId",
            new { NetId = netId, RootId = rootId }
        );
    }

    public List<SupplyInvoice> GetAllIncomedInvoicesFiltered(DateTime from, DateTime to) {
        List<SupplyInvoice> invoices = new();

        _connection.Query<SupplyInvoice, PackingList, PackingListPackageOrderItem, SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "WHERE [SupplyInvoice].DateFrom >= @From " +
            "AND [SupplyInvoice].DateFrom <= @To " +
            "AND [PackingList].IsPlaced = 1 " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL",
            (invoice, packList, packListItem) => {
                if (!invoices.Any(i => i.Id.Equals(invoice.Id))) {
                    packList.PackingListPackageOrderItems.Add(packListItem);

                    invoice.PackingLists.Add(packList);

                    invoices.Add(invoice);
                } else {
                    SupplyInvoice invoiceFromList = invoices.First(i => i.Id.Equals(invoice.Id));

                    if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packList.Id))) {
                        packList.PackingListPackageOrderItems.Add(packListItem);

                        invoiceFromList.PackingLists.Add(packList);
                    } else {
                        invoiceFromList.PackingLists.First(p => p.Id.Equals(packList.Id)).PackingListPackageOrderItems.Add(packListItem);
                    }
                }

                return invoice;
            },
            new { From = from, To = to }
        );

        return invoices;
    }

    public List<SupplyInvoice> GetAllIncomeInvoicesFiltered(DateTime from, DateTime to) {
        List<SupplyInvoice> invoices = new();

        _connection.Query<SupplyInvoice, PackingList, PackingListPackageOrderItem, SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "WHERE [SupplyInvoice].DateFrom >= @From " +
            "AND [SupplyInvoice].DateFrom <= @To " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL",
            (invoice, packList, packListItem) => {
                if (!invoices.Any(i => i.Id.Equals(invoice.Id))) {
                    packList.PackingListPackageOrderItems.Add(packListItem);

                    invoice.PackingLists.Add(packList);

                    invoices.Add(invoice);
                } else {
                    SupplyInvoice invoiceFromList = invoices.First(i => i.Id.Equals(invoice.Id));

                    if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packList.Id))) {
                        packList.PackingListPackageOrderItems.Add(packListItem);

                        invoiceFromList.PackingLists.Add(packList);
                    } else {
                        invoiceFromList.PackingLists.First(p => p.Id.Equals(packList.Id)).PackingListPackageOrderItems.Add(packListItem);
                    }
                }

                return invoice;
            },
            new { From = from, To = to }
        );

        return invoices;
    }

    public List<SupplyInvoice> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<SupplyInvoice> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [SupplyInvoice].ID DESC) AS RowNumber " +
            ", [SupplyInvoice].ID " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "WHERE [SupplyInvoice].Deleted = 0 " +
            "AND [SupplyInvoice].Created >= @From " +
            "AND [SupplyInvoice].Created <= @To " +
            "AND [SupplyInvoice].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ")" +
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "WHERE [SupplyInvoice].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[2];
            Client client = (Client)objects[3];

            if (!toReturn.Any(i => i.Id.Equals(supplyInvoice.Id))) {
                supplyOrder.Client = client;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                supplyInvoice.SupplyOrder = supplyOrder;

                toReturn.Add(supplyInvoice);
            }

            return supplyInvoice;
        };

        var props = new { Value = value, Limit = limit, Offset = offset, From = from, To = to, ClientNetId = clientNetId };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<SupplyInvoice> GetAllByContainerNetId(Guid containerNetId) {
        List<SupplyInvoice> supplyInvoices = new();

        string sqlExpression = "SELECT * FROM SupplyInvoice " +
                               "WHERE ContainerService.NetUID = @ContainerNetId";

        Type[] types = {
            typeof(SupplyInvoice)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];

            if (!supplyInvoices.Any(o => o.Id.Equals(supplyInvoice.Id))) supplyInvoices.Add(supplyInvoice);

            return supplyInvoice;
        };

        var props = new { ContainerNetId = containerNetId };

        _connection.Query(sqlExpression, types, mapper, props);

        return supplyInvoices;
    }

    public List<SupplyInvoice> GetAllIncomedInvoicesFiltered(
        DateTime from,
        DateTime to,
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        long limit,
        long offset
    ) {
        List<SupplyInvoice> invoices = new();

        string sqlExpression =
            "; WITH [Search_CTE] " +
            "AS (" +
            "SELECT [SupplyInvoice].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [SupplyInvoice].DateFrom DESC) AS [RowNumber] " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID ";

        sqlExpression +=
            "WHERE [SupplyInvoice].DateFrom >= @From " +
            "AND [SupplyInvoice].DateFrom <= @To " +
            "AND [PackingList].IsPlaced = 1 " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL " +
            "AND [Storage].NetUID = @StorageNetId " +
            "AND (" +
            "[SupplyInvoice].[Number] like N'%' + @Value + N'%' " +
            "OR " +
            "[SupplyOrderNumber].[Number] like N'%' + @Value + N'%'" +
            ")";

        if (supplierNetId.HasValue)
            sqlExpression +=
                " AND [Client].NetUID = @SupplierNetId ";

        sqlExpression +=
            "GROUP BY [SupplyInvoice].ID, [SupplyInvoice].DateFrom" +
            ")" +
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [SupplyInvoice].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ")";

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[2];
            Client suppler = (Client)objects[3];
            PackingList packList = (PackingList)objects[4];
            PackingListPackageOrderItem packListItem = (PackingListPackageOrderItem)objects[5];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[6];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];

            if (!invoices.Any(i => i.Id.Equals(invoice.Id))) {
                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                product.MeasureUnit = measureUnit;

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                packListItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                packListItem.TotalGrossPrice =
                    decimal.Round(packListItem.UnitPriceEur * Convert.ToDecimal(packListItem.RemainingQty), 2, MidpointRounding.AwayFromZero);

                packListItem.TotalGrossWeight = Math.Round(packListItem.GrossWeight * packListItem.RemainingQty, 3);

                packList.PackingListPackageOrderItems.Add(packListItem);

                packList.TotalGrossWeight = packListItem.TotalGrossWeight;

                invoice.TotalGrossWeight = packListItem.TotalGrossWeight;

                invoice.TotalGrossPrice = packListItem.TotalGrossPrice;

                supplyOrder.Client = suppler;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                invoice.SupplyOrder = supplyOrder;

                invoice.PackingLists.Add(packList);

                invoices.Add(invoice);
            } else {
                SupplyInvoice invoiceFromList = invoices.First(i => i.Id.Equals(invoice.Id));

                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                product.MeasureUnit = measureUnit;

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                packListItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                packListItem.TotalGrossPrice =
                    decimal.Round(packListItem.UnitPriceEur * Convert.ToDecimal(packListItem.RemainingQty), 2, MidpointRounding.AwayFromZero);

                packListItem.TotalGrossWeight = Math.Round(packListItem.GrossWeight * packListItem.RemainingQty, 3);

                invoiceFromList.TotalGrossWeight =
                    Math.Round(invoiceFromList.TotalGrossWeight + packListItem.TotalGrossWeight, 3);

                invoiceFromList.TotalGrossPrice =
                    decimal.Round(invoiceFromList.TotalGrossPrice + packListItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packList.Id))) {
                    packList.PackingListPackageOrderItems.Add(packListItem);

                    packList.PackingListPackageOrderItems.Add(packListItem);

                    packList.TotalGrossWeight = packListItem.TotalGrossWeight;

                    invoiceFromList.PackingLists.Add(packList);
                } else {
                    PackingList packListFromList = invoiceFromList.PackingLists.First(p => p.Id.Equals(packList.Id));

                    packListFromList.TotalGrossWeight =
                        Math.Round(packListFromList.TotalGrossWeight + packListItem.TotalGrossWeight, 3);

                    packListFromList.TotalGrossPrice =
                        decimal.Round(packListFromList.TotalGrossPrice + packListItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                    packListFromList.PackingListPackageOrderItems.Add(packListItem);
                }
            }


            return invoice;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Value = value,
                SupplierNetId = supplierNetId,
                StorageNetId = storageNetId,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return invoices;
    }

    public List<SupplyInvoice> GetAllBySupplyOrderIdWithPackingLists(long supplyOrderId) {
        List<SupplyInvoice> invoices = new();

        _connection.Query<SupplyInvoice, PackingList, PackingListPackageOrderItem, SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "WHERE [SupplyInvoice].Deleted = 0 " +
            "AND [SupplyInvoice].SupplyOrderID = @SupplyOrderId",
            (invoice, packList, packListItem) => {
                if (!invoices.Any(i => i.Id.Equals(invoice.Id))) {
                    if (packList != null) {
                        if (packListItem != null) packList.PackingListPackageOrderItems.Add(packListItem);

                        invoice.PackingLists.Add(packList);
                    }

                    invoices.Add(invoice);
                } else if (packList != null) {
                    SupplyInvoice invoiceFromList = invoices.First(i => i.Id.Equals(invoice.Id));

                    if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packList.Id))) {
                        if (packListItem != null) packList.PackingListPackageOrderItems.Add(packListItem);

                        invoiceFromList.PackingLists.Add(packList);
                    } else if (packListItem != null) {
                        invoiceFromList.PackingLists.First(p => p.Id.Equals(packList.Id)).PackingListPackageOrderItems.Add(packListItem);
                    }
                }

                return invoice;
            },
            new { SupplyOrderId = supplyOrderId }
        );

        return invoices;
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
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].Id = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID ";

        sqlExpression +=
            "WHERE [PackingListPackageOrderItem].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId " +
            "AND [SupplyInvoice].DateFrom >= @From " +
            "AND [SupplyInvoice].DateFrom <= @To " +
            "AND [PackingList].Deleted = 0 " +
            "AND [PackingList].IsPlaced = 1 " +
            "AND (" +
            "[SupplyOrderNumber].[Number] like N'%' + @Value + N'%' " +
            "OR " +
            "[SupplyInvoice].[Number] like N'%' + @Value + N'%' " +
            ")";

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

    public long GetCountByContainerNetId(Guid containerNetId) {
        return _connection.Query<long>("SELECT COUNT(*) FROM supplyInvoice " +
                                       "WHERE supplyInvoice.ContainerServiceID = (" +
                                       "SELECT ID FROM ContainerService WHERE NetUID = @ContainerNetId" +
                                       ")",
                new { ContainerNetId = containerNetId }
            )
            .SingleOrDefault();
    }

    public void SetIsShipped(SupplyInvoice supplyInvoice) {
        _connection.Execute(
            "UPDATE SupplyInvoice SET IsShipped = 1 WHERE NetUID = @NetUID",
            supplyInvoice
        );
    }

    public SupplyInvoice GetById(long id) {
        SupplyInvoice supplyInvoiceToReturn = null;

        string sqlExpression = "SELECT * FROM SupplyInvoice " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.SupplyInvoiceID = SupplyInvoice.ID " +
                               "AND InvoiceDocument.Deleted = 0 " +
                               "LEFT OUTER JOIN SupplyOrderPaymentDeliveryProtocol " +
                               "ON SupplyOrderPaymentDeliveryProtocol.SupplyInvoiceID = SupplyInvoice.ID " +
                               "AND SupplyOrderPaymentDeliveryProtocol.Deleted = 0 " +
                               "LEFT OUTER JOIN SupplyOrderPaymentDeliveryProtocolKey " +
                               "ON SupplyOrderPaymentDeliveryProtocolKey.ID = SupplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyID " +
                               "AND SupplyOrderPaymentDeliveryProtocolKey.Deleted = 0 " +
                               "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocol.User] " +
                               "ON [SupplyOrderPaymentDeliveryProtocol.User].ID = SupplyOrderPaymentDeliveryProtocol.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask AS SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = SupplyOrderPaymentDeliveryProtocol.SupplyPaymentTaskID " +
                               "AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User] " +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "AND [SupplyPaymentTask.User].Deleted = 0 " +
                               "LEFT OUTER JOIN SupplyInformationDeliveryProtocol " +
                               "ON SupplyInformationDeliveryProtocol.SupplyInvoiceID = SupplyInvoice.ID " +
                               "AND SupplyInformationDeliveryProtocol.Deleted = 0 " +
                               "LEFT OUTER JOIN SupplyInformationDeliveryProtocolKey " +
                               "ON SupplyInformationDeliveryProtocolKey.ID = SupplyInformationDeliveryProtocol.SupplyInformationDeliveryProtocolKeyID " +
                               "AND SupplyInformationDeliveryProtocolKey.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyInformationDeliveryProtocol.User] " +
                               "ON [SupplyInformationDeliveryProtocol.User].ID =SupplyInformationDeliveryProtocol.UserID " +
                               "WHERE SupplyInvoice.ID = @Id";


        Type[] types = {
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[1];
            SupplyOrderPaymentDeliveryProtocol paymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey paymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            User paymentProtocolUser = (User)objects[4];
            SupplyPaymentTask paymentProtocolSupplyPaymentTask = (SupplyPaymentTask)objects[5];
            User paymentProtocolSupplyPaymentTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol informationProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey informationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            User informationProtocolUser = (User)objects[9];

            if (supplyInvoiceToReturn != null) {
                if (invoiceDocument != null && !supplyInvoiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    supplyInvoiceToReturn.InvoiceDocuments.Add(invoiceDocument);

                if (paymentProtocol != null && !supplyInvoiceToReturn.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentProtocol.Id))) {
                    if (paymentProtocolSupplyPaymentTask != null) {
                        paymentProtocolSupplyPaymentTask.User = paymentProtocolSupplyPaymentTaskUser;

                        paymentProtocol.SupplyPaymentTask = paymentProtocolSupplyPaymentTask;
                    }

                    paymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentProtocolKey;
                    paymentProtocol.User = paymentProtocolUser;

                    supplyInvoiceToReturn.PaymentDeliveryProtocols.Add(paymentProtocol);
                }

                if (informationProtocol != null && !supplyInvoiceToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationProtocol.Id))) {
                    informationProtocol.SupplyInformationDeliveryProtocolKey = informationProtocolKey;
                    informationProtocol.User = informationProtocolUser;

                    supplyInvoiceToReturn.InformationDeliveryProtocols.Add(informationProtocol);
                }
            } else {
                if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                if (paymentProtocol != null) {
                    if (paymentProtocolSupplyPaymentTask != null) {
                        paymentProtocolSupplyPaymentTask.User = paymentProtocolSupplyPaymentTaskUser;

                        paymentProtocol.SupplyPaymentTask = paymentProtocolSupplyPaymentTask;
                    }

                    paymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentProtocolKey;
                    paymentProtocol.User = paymentProtocolUser;

                    supplyInvoice.PaymentDeliveryProtocols.Add(paymentProtocol);
                }

                if (informationProtocol != null) {
                    informationProtocol.SupplyInformationDeliveryProtocolKey = informationProtocolKey;
                    informationProtocol.User = informationProtocolUser;

                    supplyInvoice.InformationDeliveryProtocols.Add(informationProtocol);
                }

                //                    if (supplyInvoice.DateFrom.HasValue) {
                //                        supplyInvoice.DateFrom = TimeZoneInfo.ConvertTimeFromUtc(supplyInvoice.DateFrom.Value, timeZone);
                //                    }
                //                    if (supplyInvoice.PaymentTo.HasValue) {
                //                        supplyInvoice.PaymentTo = TimeZoneInfo.ConvertTimeFromUtc(supplyInvoice.PaymentTo.Value, timeZone);
                //                    }

                supplyInvoiceToReturn = supplyInvoice;
            }

            return supplyInvoice;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (supplyInvoiceToReturn != null) {
            types = new[] {
                typeof(SupplyInvoice),
                typeof(SupplyInvoiceOrderItem),
                typeof(SupplyOrderItem),
                typeof(Product),
                typeof(MeasureUnit)
            };

            Func<object[], SupplyInvoice> orderItemsMapper = objects => {
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
                SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
                SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
                Product product = (Product)objects[3];
                MeasureUnit measureUnit = (MeasureUnit)objects[4];

                if (supplyInvoiceOrderItem != null)
                    if (!supplyInvoiceToReturn.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;

                        supplyInvoiceOrderItem.Product = product;

                        supplyInvoiceToReturn.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
                    }

                return supplyInvoice;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyInvoice] " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [SupplyInvoice].ID = @Id",
                types,
                orderItemsMapper,
                props
            );
        }

        return supplyInvoiceToReturn;
    }

    public SupplyInvoice GetByIdWithoutIncludes(long id) {
        return _connection.Query<SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyInvoice GetByNetIdAndProductIdWithSupplyOrderIncludes(Guid netId, long productId) {
        return _connection.Query<SupplyInvoice, SupplyOrder, Organization, SupplyInvoiceOrderItem, SupplyOrderItem, SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [SupplyInvoice].NetUID = @NetId " +
            "AND [SupplyInvoiceOrderItem].ProductID = @ProductId",
            (invoice, order, organization, invoiceItem, orderItem) => {
                if (invoiceItem != null) {
                    invoiceItem.SupplyOrderItem = orderItem;

                    invoice.SupplyInvoiceOrderItems.Add(invoiceItem);
                }

                order.Organization = organization;

                invoice.SupplyOrder = order;

                return invoice;
            },
            new { NetId = netId, ProductId = productId }
        ).SingleOrDefault();
    }

    public SupplyInvoice GetByNetIdWithProducts(Guid netId) {
        SupplyInvoice toReturn = null;

        _connection.Query<SupplyInvoice, SupplyOrder, Organization, SupplyInvoiceOrderItem, SupplyOrderItem, SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            (invoice, order, organization, invoiceItem, orderItem) => {
                if (toReturn == null) {
                    order.Organization = organization;

                    invoice.SupplyOrder = order;

                    toReturn = invoice;
                }

                if (invoiceItem == null)
                    return invoice;

                invoiceItem.SupplyOrderItem = orderItem;

                toReturn.SupplyInvoiceOrderItems.Add(invoiceItem);

                return invoice;
            },
            new { NetId = netId }
        );

        return toReturn;
    }

    public SupplyInvoice GetByNetId(Guid netId) {
        SupplyInvoice supplyInvoiceToReturn = null;

        _connection.Query<SupplyInvoice, InvoiceDocument, SupplyInvoice>(
            "SELECT * FROM SupplyInvoice " +
            "LEFT OUTER JOIN InvoiceDocument " +
            "ON SupplyInvoice.ID = SupplyInvoice.ID " +
            "AND InvoiceDocument.Deleted = 0 " +
            "WHERE SupplyInvoice.NetUID = @NetId",
            (supplyInvoice, invoiceDocument) => {
                if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                if (supplyInvoiceToReturn != null) {
                    if (invoiceDocument != null && !supplyInvoiceToReturn.InvoiceDocuments.Any(i => i.Id.Equals(invoiceDocument.Id)))
                        supplyInvoiceToReturn.InvoiceDocuments.Add(invoiceDocument);
                } else {
                    supplyInvoiceToReturn = supplyInvoice;
                }

                return supplyInvoice;
            },
            new { NetId = netId }
        );


        return supplyInvoiceToReturn;
    }

    public SupplyInvoice GetByNetIdWithItemsAndSpecification(Guid netId) {
        SupplyInvoice toReturn = null;

        _connection.Query<SupplyInvoice, SupplyInvoiceOrderItem, SupplyOrderItem, ProductSpecification, SupplyOrder, Organization, SupplyInvoice>(
            "SELECT [SupplyInvoice].* " +
            ",[SupplyInvoiceOrderItem].* " +
            ",[SupplyOrderItem].* " +
            ",[ProductSpecification].* " +
            ",[SupplyOrder].* " +
            ",[Organization].* " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = (" +
            "SELECT TOP(1) [ProductSpecification].ID " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "WHERE [ProductSpecification].ProductID = [SupplyInvoiceOrderItem].ProductID " +
            "AND [OrderProductSpecification].SupplyInvoiceID = [SupplyInvoice].ID " +
            "ORDER BY [ProductSpecification].ID DESC" +
            ") " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            (invoice, invoiceOrderItem, orderItem, productSpecification, order, organization) => {
                if (toReturn == null) {
                    order.Organization = organization;

                    invoice.SupplyOrder = order;

                    toReturn = invoice;
                }

                if (invoiceOrderItem == null) return invoice;

                invoiceOrderItem.SupplyOrderItem = orderItem;
                invoiceOrderItem.ProductSpecification = productSpecification;

                toReturn.SupplyInvoiceOrderItems.Add(invoiceOrderItem);

                return invoice;
            },
            new { NetId = netId }
        );

        return toReturn;
    }

    public SupplyInvoice GetByNetIdWithItemsAndSpecificationForExport(Guid netId) {
        SupplyInvoice toReturn = null;

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackageOrderItem),
            typeof(ProductSpecification),
            typeof(ProductSpecification),
            typeof(SupplyOrder),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Currency),
            typeof(Country)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem invoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem orderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[5];
            ProductSpecification productSpecification = (ProductSpecification)objects[6];
            ProductSpecification plProductSpecification = (ProductSpecification)objects[7];
            SupplyOrder supplyOrder = (SupplyOrder)objects[8];
            Organization organization = (Organization)objects[9];
            ClientAgreement clientAgreement = (ClientAgreement)objects[10];
            Client client = (Client)objects[11];
            Agreement agreement = (Agreement)objects[12];
            Currency currency = (Currency)objects[13];
            Country country = (Country)objects[14];

            if (toReturn == null) {
                agreement.Currency = currency;

                client.Country = country;

                clientAgreement.Agreement = agreement;
                clientAgreement.Client = client;

                supplyOrder.ClientAgreement = clientAgreement;
                supplyOrder.Organization = organization;

                invoice.SupplyOrder = supplyOrder;

                toReturn = invoice;
            }

            if (invoiceOrderItem == null) return invoice;

            if (toReturn.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(invoiceOrderItem.Id))) {
                invoiceOrderItem = toReturn.SupplyInvoiceOrderItems.First(i => i.Id.Equals(invoiceOrderItem.Id));
            } else {
                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                invoiceOrderItem.Product = product;

                invoiceOrderItem.SupplyOrderItem = orderItem;
                invoiceOrderItem.ProductSpecification = productSpecification;
                invoiceOrderItem.PlProductSpecification = plProductSpecification;

                toReturn.SupplyInvoiceOrderItems.Add(invoiceOrderItem);
            }

            if (packageOrderItem == null) return invoice;

            invoiceOrderItem.PackingListPackageOrderItems.Add(packageOrderItem);

            return invoice;
        };

        _connection.Query(
            "SELECT [SupplyInvoice].* " +
            ",[SupplyInvoiceOrderItem].* " +
            ",[SupplyOrderItem].* " +
            ",[Product].* " +
            ",[MeasureUnit].* " +
            ",[PackingListPackageOrderItem].* " +
            ",[ProductSpecification].* " +
            ",[PlProductSpecification].* " +
            ",[SupplyOrder].* " +
            ",[Organization].* " +
            ",[ClientAgreement].* " +
            ",[Client].* " +
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[Country].* " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Country] " +
            "ON [Country].ID = [Client].CountryID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = (" +
            "SELECT TOP(1) [ProductSpecification].ID " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "WHERE [ProductSpecification].ProductID = [SupplyInvoiceOrderItem].ProductID " +
            "AND [OrderProductSpecification].SupplyInvoiceID = [SupplyInvoice].ID " +
            "ORDER BY [ProductSpecification].ID DESC" +
            ") " +
            "LEFT JOIN [ProductSpecification] AS [PlProductSpecification] " +
            "ON [PlProductSpecification].ID = (" +
            "SELECT TOP(1) [JoinSpecification].ID " +
            "FROM [ProductSpecification] AS [JoinSpecification] " +
            "WHERE [JoinSpecification].ProductID = [SupplyInvoiceOrderItem].ProductID " +
            "AND [JoinSpecification].IsActive = 1 " +
            "AND [JoinSpecification].Locale = N'pl' " +
            "ORDER BY [JoinSpecification].ID DESC" +
            ") " +
            "WHERE [SupplyInvoice].NetUID = @NetId " +
            "ORDER BY [SupplyInvoiceOrderItem].ID",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public SupplyInvoice GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<SupplyInvoice>(
                "SELECT * " +
                "FROM [SupplyInvoice] " +
                "WHERE [SupplyInvoice].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public SupplyInvoice GetByNetIdForDocumentUpload(Guid netId) {
        SupplyInvoice toReturn =
            _connection.Query<SupplyInvoice>(
                    "SELECT * " +
                    "FROM [SupplyInvoice] " +
                    "WHERE [SupplyInvoice].NetUID = @NetId",
                    new { NetId = netId }
                )
                .Single();

        toReturn.SupplyInvoiceOrderItems =
            _connection.Query<SupplyInvoiceOrderItem, SupplyOrderItem, SupplyInvoiceOrderItem>(
                "SELECT * " +
                "FROM [SupplyInvoiceOrderItem] " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "WHERE [SupplyInvoiceOrderItem].Deleted = 0 " +
                "AND [SupplyInvoiceOrderItem].SupplyInvoiceID = @Id",
                (invoiceItem, orderItem) => {
                    invoiceItem.SupplyOrderItem = orderItem;

                    return invoiceItem;
                },
                new { toReturn.Id }
            ).ToList();

        toReturn.SupplyOrder =
            _connection.Query<SupplyOrder>(
                    "SELECT * " +
                    "FROM [SupplyOrder] " +
                    "WHERE [SupplyOrder].ID = @Id",
                    new { Id = toReturn.SupplyOrderId }
                )
                .Single();

        toReturn.SupplyOrder.SupplyOrderItems =
            _connection.Query<SupplyOrderItem>(
                "SELECT * " +
                "FROM [SupplyOrderItem] " +
                "WHERE [SupplyOrderItem].Deleted = 0 " +
                "AND [SupplyOrderItem].SupplyOrderID = @Id",
                new { Id = toReturn.SupplyOrderId }
            ).ToList();

        _connection.Query<PackingList, PackingListPackageOrderItem, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "WHERE [PackingList].Deleted = 0 " +
            "AND [PackingList].SupplyInvoiceID = @Id",
            (packingList, packingListItem) => {
                if (toReturn.PackingLists.Any(p => p.Id.Equals(toReturn.Id))) {
                    if (packingListItem != null) toReturn.PackingLists.First(p => p.Id.Equals(toReturn.Id)).PackingListPackageOrderItems.Add(packingListItem);
                } else {
                    if (packingListItem != null) packingList.PackingListPackageOrderItems.Add(packingListItem);

                    toReturn.PackingLists.Add(packingList);
                }

                return packingList;
            },
            new { toReturn.Id }
        );

        return toReturn;
    }

    public SupplyInvoice GetByNetIdWithAllIncludes(Guid netId) {
        SupplyInvoice supplyInvoiceToReturn = null;

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyOrder),
            typeof(DeliveryProductProtocol),
            typeof(SupplyInvoiceDeliveryDocument),
            typeof(SupplyDeliveryDocument),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(Organization)
        };

        Func<object[], SupplyInvoice> invoiceMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            InvoiceDocument supplyInvoiceDocument = (InvoiceDocument)objects[1];
            SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey paymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            User supplyOrderPaymentDeliveryProtocolUser = (User)objects[4];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[5];
            User supplyPaymentTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol informationDeliveryProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey informationDeliveryProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            User informationDeliveryProtocolUser = (User)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            DeliveryProductProtocol deliveryProductProtocol = (DeliveryProductProtocol)objects[11];
            SupplyInvoiceDeliveryDocument document = (SupplyInvoiceDeliveryDocument)objects[12];
            SupplyDeliveryDocument supplyDocument = (SupplyDeliveryDocument)objects[13];
            ClientAgreement clientAgreement = (ClientAgreement)objects[14];
            Agreement agreement = (Agreement)objects[15];
            Currency currency = (Currency)objects[16];
            Client client = (Client)objects[17];
            Organization organization = (Organization)objects[18];

            if (supplyInvoiceToReturn == null) {
                agreement.Currency = currency;
                clientAgreement.Agreement = agreement;
                supplyOrder.ClientAgreement = clientAgreement;
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyInvoice.SupplyOrder = supplyOrder;
                supplyInvoice.DeliveryProductProtocol = deliveryProductProtocol;
                supplyInvoiceToReturn = supplyInvoice;
            }

            if (supplyInvoiceDocument != null && !supplyInvoiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(supplyInvoiceDocument.Id)))
                supplyInvoiceToReturn.InvoiceDocuments.Add(supplyInvoiceDocument);

            if (paymentDeliveryProtocol != null && !supplyInvoiceToReturn.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentDeliveryProtocol.Id))) {
                if (supplyPaymentTask != null) {
                    supplyPaymentTask.User = supplyPaymentTaskUser;

                    paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                }

                paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                supplyInvoiceToReturn.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
            }

            if (document != null) {
                document.SupplyDeliveryDocument = supplyDocument;

                if (!supplyInvoiceToReturn.SupplyInvoiceDeliveryDocuments.Any(x => x.Id.Equals(document.Id)))
                    supplyInvoiceToReturn.SupplyInvoiceDeliveryDocuments.Add(document);
            }

            if (informationDeliveryProtocol == null || supplyInvoiceToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationDeliveryProtocol.Id)))
                return supplyInvoice;

            informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
            informationDeliveryProtocol.User = informationDeliveryProtocolUser;

            supplyInvoiceToReturn.InformationDeliveryProtocols.Add(informationDeliveryProtocol);

            return supplyInvoice;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [InvoiceDocument] AS [SupplyInvoiceDocument] " +
            "ON [SupplyInvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] " +
            "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
            "ON [SupplyOrderPaymentDeliveryProtocol].UserID = [SupplyOrderPaymentDeliveryProtocolUser].ID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] " +
            "ON [SupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [SupplyInformationDeliveryProtocolKey] " +
            "ON [SupplyInformationDeliveryProtocolKey].ID = [SupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "AND [SupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [SupplyInformationDeliveryProtocolUser] " +
            "ON [SupplyInformationDeliveryProtocolUser].ID = [SupplyInformationDeliveryProtocol].UserID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].ID = [SupplyInvoice].DeliveryProductProtocolID " +
            "LEFT JOIN [SupplyInvoiceDeliveryDocument] " +
            "ON [SupplyInvoiceDeliveryDocument].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [SupplyInvoiceDeliveryDocument].[Deleted] = 0 " +
            "LEFT JOIN [SupplyDeliveryDocument] " +
            "ON [SupplyDeliveryDocument].[ID] = [SupplyInvoiceDeliveryDocument].[SupplyDeliveryDocumentID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            types,
            invoiceMapper,
            props
        );

        if (supplyInvoiceToReturn == null)
            return null;

        supplyInvoiceToReturn.MergedSupplyInvoices =
            _connection.Query<SupplyInvoice>(
                "SELECT * " +
                "FROM [SupplyInvoice] " +
                "WHERE [SupplyInvoice].RootSupplyInvoiceId = @Id",
                new { supplyInvoiceToReturn.Id }).ToList();

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        bool isGrossCalculated = supplyInvoiceToReturn.SupplyOrder.IsGrossPricesCalculated;

        types = new[] {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        Func<object[], SupplyInvoice> orderItemsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSpecification productSpecification = (ProductSpecification)objects[5];
            User user = (User)objects[6];

            if (supplyInvoiceOrderItem == null) return supplyInvoice;

            if (!supplyInvoiceToReturn.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                if (productSpecification != null) {
                    productSpecification.AddedBy = user;

                    product.ProductSpecifications.Add(productSpecification);
                }

                product.MeasureUnit = measureUnit;

                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                if (supplyOrderItem != null) {
                    supplyOrderItem.Product = product;

                    supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    supplyOrderItem.NetWeight = Math.Round(supplyOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                    supplyOrderItem.GrossWeight = Math.Round(supplyOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);
                }

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                supplyInvoiceToReturn.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
            } else if (productSpecification != null) {
                SupplyInvoiceOrderItem fromList = supplyInvoiceToReturn.SupplyInvoiceOrderItems.First(i => i.Id.Equals(supplyInvoiceOrderItem.Id));

                if (fromList.SupplyOrderItem != null) {
                    if (!fromList.SupplyOrderItem.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id)))
                        fromList.SupplyOrderItem.Product.ProductSpecifications.Add(productSpecification);
                } else {
                    if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id))) fromList.Product.ProductSpecifications.Add(productSpecification);
                }
            }

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            types,
            orderItemsMapper,
            props
        );

        types = new[] {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackage),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductPlacement),
            typeof(ProductPlacement),
            typeof(ProductSpecification),
            typeof(User),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(ProductSpecification)
        };

        List<long> productIds = new();

        Func<object[], PackingList> packingListMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
            SupplyInvoiceOrderItem packageSupplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem packageSupplyOrderItem = (SupplyOrderItem)objects[9];
            Product packageProduct = (Product)objects[10];
            MeasureUnit packageProductMeasureUnit = (MeasureUnit)objects[11];
            ProductPlacement itemProductPlacement = (ProductPlacement)objects[12];
            ProductPlacement packageItemProductPlacement = (ProductPlacement)objects[13];
            ProductSpecification productSpecification = (ProductSpecification)objects[14];
            User user = (User)objects[15];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[16];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[17];
            ProductSpecification productSpecificationConsignmentItem = (ProductSpecification)objects[18];

            if (packingList == null) return null;

            if (!supplyInvoiceToReturn.PackingLists.Any(p => p.Id.Equals(packingList.Id)))
                supplyInvoiceToReturn.PackingLists.Add(packingList);
            else
                packingList = supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(packingList.Id));

            if (packingListPackageOrderItem != null) {
                if (!packingList.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    if (productIncomeItem != null && consignmentItem != null) {
                        consignmentItem.ProductSpecification = productSpecificationConsignmentItem;
                        productIncomeItem.ConsignmentItems.Add(consignmentItem);
                        packingListPackageOrderItem.ProductIncomeItem = productIncomeItem;
                    }

                    if (productSpecification != null) {
                        productSpecification.AddedBy = user;
                        packingList.TotalCustomValue += productSpecification.CustomsValue;
                        product.ProductSpecifications.Add(productSpecification);
                    }

                    product.Name =
                        isPlCulture
                            ? product.NameUA
                            : product.NameUA;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    if (!productIds.Any(p => p.Equals(product.Id))) productIds.Add(product.Id);

                    if (supplyOrderItem != null)
                        supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    supplyInvoiceOrderItem.ProductSpecification = productSpecification;

                    supplyInvoiceOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;

                    supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingListPackageOrderItem.TotalNetWeight =
                        packingListPackageOrderItem.NetWeight * packingListPackageOrderItem.Qty;
                    packingListPackageOrderItem.TotalGrossWeight =
                        packingListPackageOrderItem.GrossWeight * packingListPackageOrderItem.Qty;

                    packingListPackageOrderItem.UnitPrice = decimal.Round(packingListPackageOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    if (isGrossCalculated) {
                        packingListPackageOrderItem.TotalNetPrice =
                            decimal.Round(
                                decimal.Round(packingListPackageOrderItem.UnitPriceEur * packingListPackageOrderItem.ExchangeRateAmount, 2,
                                    MidpointRounding.AwayFromZero)
                                * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalGrossPrice =
                            packingListPackageOrderItem.GrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur
                                                                          * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        packingListPackageOrderItem.AccountingTotalGrossPrice =
                            packingListPackageOrderItem.AccountingGrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur
                                                                                    * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        packingListPackageOrderItem.VatAmount =
                            decimal.Round(
                                packingListPackageOrderItem.TotalNetPrice * packingListPackageOrderItem.VatPercent / 100,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalNetPrice = decimal.Round(
                            packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    } else {
                        packingListPackageOrderItem.TotalNetPrice =
                            packingListPackageOrderItem.AccountingTotalGrossPrice =
                                packingListPackageOrderItem.TotalGrossPrice =
                                    decimal.Round(
                                        packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    );

                        packingListPackageOrderItem.VatAmount =
                            decimal.Round(
                                packingListPackageOrderItem.TotalNetPrice * packingListPackageOrderItem.VatPercent / 100,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalNetPrice = decimal.Round(
                            packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    }

                    packingListPackageOrderItem.TotalNetPriceEur =
                        packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                    packingListPackageOrderItem.TotalGrossPriceEur =
                        packingListPackageOrderItem.GrossUnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                    packingListPackageOrderItem.AccountingTotalGrossPriceEur =
                        packingListPackageOrderItem.AccountingGrossUnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                    packingListPackageOrderItem.VatAmountEur =
                        decimal.Round(
                            packingListPackageOrderItem.TotalNetPriceEur * packingListPackageOrderItem.VatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.TotalNetPriceWithVat =
                        decimal.Round(
                            packingListPackageOrderItem.TotalNetPrice + packingListPackageOrderItem.VatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.TotalNetPriceWithVatEur =
                        decimal.Round(
                            packingListPackageOrderItem.TotalNetPriceEur + packingListPackageOrderItem.VatAmountEur,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingList.TotalNetPriceEur =
                        decimal.Round(packingList.TotalNetPriceEur + packingListPackageOrderItem.TotalNetPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.TotalGrossPriceEur =
                        decimal.Round(packingList.TotalGrossPriceEur + packingListPackageOrderItem.TotalGrossPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.AccountingTotalGrossPriceEur =
                        decimal.Round(packingList.AccountingTotalGrossPriceEur + packingListPackageOrderItem.AccountingTotalGrossPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;
                    packingList.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;

                    packingList.TotalNetPrice += Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.UnitPrice;
                    packingList.TotalGrossPrice += packingListPackageOrderItem.TotalGrossPrice;
                    packingList.AccountingTotalGrossPrice += packingListPackageOrderItem.AccountingTotalGrossPrice;

                    packingList.TotalNetPriceWithVat += packingListPackageOrderItem.TotalNetPriceWithVat;

                    packingList.TotalNetPriceWithVatEur += packingListPackageOrderItem.TotalNetPriceWithVatEur;

                    packingList.TotalVatAmount += packingListPackageOrderItem.VatAmount;

                    supplyInvoiceToReturn.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;

                    supplyInvoiceToReturn.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;

                    supplyInvoiceToReturn.TotalQuantity += packingListPackageOrderItem.Qty;

                    packingList.TotalQuantity += packingListPackageOrderItem.Qty;

                    supplyInvoiceToReturn.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;

                    supplyInvoiceToReturn.TotalGrossPrice += packingListPackageOrderItem.TotalGrossPrice;

                    supplyInvoiceToReturn.AccountingTotalGrossPrice += packingListPackageOrderItem.AccountingTotalGrossPrice;

                    supplyInvoiceToReturn.TotalNetPriceWithVat += packingListPackageOrderItem.TotalNetPriceWithVat;

                    supplyInvoiceToReturn.TotalVatAmount += packingListPackageOrderItem.VatAmount;

                    if (itemProductPlacement != null) packingListPackageOrderItem.ProductPlacements.Add(itemProductPlacement);

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                } else if (itemProductPlacement != null) {
                    packingList
                        .PackingListPackageOrderItems
                        .First(i => i.Id.Equals(packingListPackageOrderItem.Id))
                        .ProductPlacements
                        .Add(itemProductPlacement);

                    PackingListPackageOrderItem fromListItem = packingList.PackingListPackageOrderItems.First(i => i.Id.Equals(packingListPackageOrderItem.Id));

                    if (productIncomeItem != null && consignmentItem != null) {
                        consignmentItem.ProductSpecification = productSpecificationConsignmentItem;
                        productIncomeItem.ConsignmentItems.Add(consignmentItem);
                        fromListItem.ProductIncomeItem = productIncomeItem;
                    }

                    if (fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id)))
                        return packingList;

                    productSpecification.AddedBy = user;

                    fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Add(productSpecification);

                    fromListItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                    if (fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any()) {
                        ProductSpecification productSpecificationFromList = fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Last();

                        decimal dutyAndVatValue;

                        if (!fromListItem.ExchangeRateAmountUahToEur.Equals(0))
                            dutyAndVatValue =
                                fromListItem.ExchangeRateAmountUahToEur > 0
                                    ? (productSpecificationFromList.Duty + productSpecificationFromList.VATValue)
                                      / fromListItem.ExchangeRateAmountUahToEur
                                    : (productSpecificationFromList.Duty + productSpecificationFromList.VATValue) * fromListItem.ExchangeRateAmountUahToEur;
                        else
                            dutyAndVatValue = productSpecificationFromList.Duty + productSpecificationFromList.VATValue;

                        fromListItem.AccountingTotalGrossPrice =
                            Convert.ToDecimal(packingListPackageOrderItem.Qty) *
                            packingListPackageOrderItem.AccountingGrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur +
                            dutyAndVatValue * packingListPackageOrderItem.ExchangeRateAmountUahToEur;

                        fromListItem.TotalGrossPrice = Convert.ToDecimal(packingListPackageOrderItem.Qty) *
                                                       packingListPackageOrderItem.GrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur +
                                                       fromListItem.AccountingTotalGrossPrice;

                        fromListItem.AccountingTotalGrossPriceEur =
                            Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.AccountingGrossUnitPriceEur +
                            dutyAndVatValue;

                        fromListItem.TotalGrossPriceEur = Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.GrossUnitPriceEur +
                                                          fromListItem.AccountingTotalGrossPriceEur;
                    }
                } else {
                    PackingListPackageOrderItem fromListItem = packingList.PackingListPackageOrderItems.First(i => i.Id.Equals(packingListPackageOrderItem.Id));

                    if (fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any()) {
                        ProductSpecification productSpecificationFromList = fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Last();

                        packingList.TotalCustomValue =
                            decimal.Round(packingList.TotalCustomValue - productSpecificationFromList.CustomsValue, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalVatAmount = decimal.Round(packingList.TotalVatAmount - productSpecificationFromList.VATValue, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalDuty = decimal.Round(packingList.TotalDuty - productSpecificationFromList.Duty, 2, MidpointRounding.AwayFromZero);
                    }

                    if (productSpecification != null) {
                        productSpecification.AddedBy = user;

                        if (!fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any(x => x.Id.Equals(productSpecification.Id))) {
                            fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Add(productSpecification);

                            packingList.TotalCustomValue = decimal.Round(packingList.TotalCustomValue + productSpecification.CustomsValue, 2, MidpointRounding.AwayFromZero);
                            packingList.TotalVatAmount = decimal.Round(packingList.TotalVatAmount + productSpecification.VATValue, 2, MidpointRounding.AwayFromZero);
                            packingList.TotalDuty = decimal.Round(packingList.TotalDuty + productSpecification.Duty, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }
            }

            if (package == null) return packingList;

            if (package.Type.Equals(PackingListPackageType.Box)) {
                if (packingList.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                    PackingListPackage packageFromList = packingList.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                    if (packageOrderItem == null) return packingList;

                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else if (packageItemProductPlacement != null) {
                        packageFromList
                            .PackingListPackageOrderItems
                            .First(p => p.Id.Equals(packageOrderItem.Id))
                            .ProductPlacements
                            .Add(packageItemProductPlacement);
                    }
                } else {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    packingList.PackingListBoxes.Add(package);
                }
            } else {
                if (packingList.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                    PackingListPackage packageFromList = packingList.PackingListPallets.First(p => p.Id.Equals(package.Id));

                    if (packageOrderItem == null) return packingList;

                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else if (packageItemProductPlacement != null) {
                        packageFromList
                            .PackingListPackageOrderItems
                            .First(p => p.Id.Equals(packageOrderItem.Id))
                            .ProductPlacements
                            .Add(packageItemProductPlacement);
                    }
                } else {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 3, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);


                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    packingList.PackingListPallets.Add(package);
                }
            }

            return packingList;
        };

        var packingListProps = new { supplyInvoiceToReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            ";WITH [SPECIFICATION_CTE] AS ( " +
            "SELECT " +
            "MAX([ProductSpecification].[ID]) AS [ID] " +
            ", [ProductSpecification].[ProductID] " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].[SupplyInvoiceId] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderProductSpecification].[ProductSpecificationId] " +
            "WHERE [SupplyInvoice].[ID] = @Id " +
            "GROUP BY [ProductSpecification].[ProductID] " +
            ") " +
            "SELECT " +
            "[PackingList].* " +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyInvoiceOrderItem].* " +
            ", [SupplyOrderItem].* " +
            ", [Product].* " +
            ", [MeasureUnit].* " +
            ", [Pallet].* " +
            ", [PalletPackageOrderItem].* " +
            ", [PalletInvoiceOrderItem].* " +
            ", [PalletOrderItem].* " +
            ", [PalletOrderItemProduct].* " +
            ", [PalletOrderItemProductMeasureUnit].* " +
            ", [ItemProductPlacement].* " +
            ", [PackageProductPlacement].* " +
            ", [ProductSpecification].* " +
            ", [User].* " +
            ", [ProductIncomeItem].* " +
            ", [ConsignmentItem].* " +
            ", [ProductSpecificationConsignmentItem].* " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackage] AS [Pallet] " +
            "ON [PackingList].ID = [Pallet].PackingListID " +
            "AND [Pallet].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [PalletPackageOrderItem] " +
            "ON [Pallet].ID = [PalletPackageOrderItem].PackingListPackageID " +
            "AND [PalletPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] AS [PalletInvoiceOrderItem] " +
            "ON [PalletPackageOrderItem].SupplyInvoiceOrderItemID = [PalletInvoiceOrderItem].ID " +
            "LEFT JOIN [SupplyOrderItem] AS [PalletOrderItem] " +
            "ON [PalletInvoiceOrderItem].SupplyOrderItemID = [PalletOrderItem].ID " +
            "LEFT JOIN [Product] AS [PalletOrderItemProduct] " +
            "ON [PalletOrderItemProduct].ID = [PalletOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [PalletOrderItemProductMeasureUnit] " +
            "ON [PalletOrderItemProductMeasureUnit].ID = [PalletOrderItemProduct].MeasureUnitID " +
            "AND [PalletOrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductPlacement] AS [ItemProductPlacement] " +
            "ON [ItemProductPlacement].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductPlacement] AS [PackageProductPlacement] " +
            "ON [PackageProductPlacement].PackingListPackageOrderItemID = [PalletPackageOrderItem].ID " +
            "LEFT JOIN [SPECIFICATION_CTE] " +
            "ON [SPECIFICATION_CTE].[ProductID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [SPECIFICATION_CTE].[ID] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
            "AND [ProductIncomeItem].[Deleted] = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "AND [ConsignmentItem].[Deleted] = 0 " +
            "LEFT JOIN [ProductSpecification] AS [ProductSpecificationConsignmentItem] " +
            "ON [ProductSpecificationConsignmentItem].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [PackingList].SupplyInvoiceID = @Id " +
            "AND [PackingList].Deleted = 0 " +
            "ORDER BY [ProductSpecification].[ID] ",
            types,
            packingListMapper,
            packingListProps,
            commandTimeout: 3600
        );

        _connection.Query<DynamicProductPlacementColumn, DynamicProductPlacementRow, PackingListPackageOrderItem, DynamicProductPlacement, DynamicProductPlacementColumn>(
            "SELECT * " +
            "FROM [DynamicProductPlacementColumn] " +
            "LEFT JOIN [DynamicProductPlacementRow] " +
            "ON [DynamicProductPlacementRow].DynamicProductPlacementColumnID = [DynamicProductPlacementColumn].ID " +
            "AND [DynamicProductPlacementRow].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [DynamicProductPlacementRow].PackingListPackageOrderItemID " +
            "LEFT JOIN [DynamicProductPlacement] " +
            "ON [DynamicProductPlacement].DynamicProductPlacementRowID = [DynamicProductPlacementRow].ID " +
            "AND [DynamicProductPlacement].Deleted = 0 " +
            "WHERE [DynamicProductPlacementColumn].Deleted = 0 " +
            "AND [DynamicProductPlacementColumn].PackingListID IN @Ids",
            (column, row, item, placement) => {
                PackingList packListFromList = supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(column.PackingListId));

                if (!packListFromList.DynamicProductPlacementColumns.Any(c => c.Id.Equals(column.Id))) {
                    if (row != null) {
                        if (placement != null) row.DynamicProductPlacements.Add(placement);

                        row.PackingListPackageOrderItem = item;

                        column.DynamicProductPlacementRows.Add(row);
                    }

                    packListFromList.DynamicProductPlacementColumns.Add(column);
                } else {
                    DynamicProductPlacementColumn columnFromList = packListFromList.DynamicProductPlacementColumns.First(c => c.Id.Equals(column.Id));

                    if (row == null) return column;

                    if (!columnFromList.DynamicProductPlacementRows.Any(r => r.Id.Equals(row.Id))) {
                        if (placement != null) row.DynamicProductPlacements.Add(placement);

                        row.PackingListPackageOrderItem = item;

                        columnFromList.DynamicProductPlacementRows.Add(row);
                    } else {
                        if (placement != null) columnFromList.DynamicProductPlacementRows.First(r => r.Id.Equals(row.Id)).DynamicProductPlacements.Add(placement);
                    }
                }

                return column;
            },
            new {
                Ids = supplyInvoiceToReturn.PackingLists.Select(p => p.Id)
            }
        );

        foreach (PackingList packingList in supplyInvoiceToReturn.PackingLists) {
            packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight, 3);
            packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight, 3);

            packingList.PackingListPackageOrderItems =
                packingList
                    .PackingListPackageOrderItems
                    .OrderBy(x => x.SupplyInvoiceOrderItem.Product.VendorCode)
                    .ToList();
        }

        supplyInvoiceToReturn.TotalNetWeight = Math.Round(supplyInvoiceToReturn.TotalNetWeight, 3);
        supplyInvoiceToReturn.TotalGrossWeight = Math.Round(supplyInvoiceToReturn.TotalGrossWeight, 3);

        if (productIds.Any()) {
            if (productIds.Count > 2000)
                for (int j = 0; j < productIds.Count % 2000; j++)
                    _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                        ";WITH [Search_CTE] " +
                        "AS ( " +
                        "SELECT MAX([ID]) AS [ID] " +
                        "FROM [ProductPlacement] " +
                        "WHERE [ProductPlacement].ProductID IN @Ids " +
                        "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                        "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                        "GROUP BY [ProductPlacement].CellNumber " +
                        ", [ProductPlacement].RowNumber " +
                        ", [ProductPlacement].StorageNumber " +
                        ", [ProductPlacement].StorageID " +
                        ") " +
                        "SELECT * " +
                        "FROM [ProductPlacement] " +
                        "LEFT JOIN [Storage] " +
                        "ON [Storage].ID = [ProductPlacement].StorageID " +
                        "WHERE [ProductPlacement].ID IN ( " +
                        "SELECT [ID] " +
                        "FROM [Search_CTE] " +
                        ")",
                        (placement, storage) => {
                            placement.Storage = storage;

                            foreach (PackingList packingList in supplyInvoiceToReturn
                                         .PackingLists
                                         .Where(p =>
                                             p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))
                                             ||
                                             p.PackingListBoxes.Any(b =>
                                                 b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                             ||
                                             p.PackingListPallets.Any(b =>
                                                 b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                         )) {
                                foreach (PackingListPackageOrderItem item in packingList
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                                foreach (PackingListPackage box in packingList
                                             .PackingListBoxes
                                             .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                                foreach (PackingListPackageOrderItem item in box
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                                foreach (PackingListPackage pallet in packingList
                                             .PackingListPallets
                                             .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                                foreach (PackingListPackageOrderItem item in pallet
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);
                            }

                            return placement;
                        },
                        new { Ids = productIds.Skip(j * 2000).Take(2000) }
                    );
            else
                _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT MAX([ID]) AS [ID] " +
                    "FROM [ProductPlacement] " +
                    "WHERE [ProductPlacement].ProductID IN @Ids " +
                    "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                    "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                    "GROUP BY [ProductPlacement].CellNumber " +
                    ", [ProductPlacement].RowNumber " +
                    ", [ProductPlacement].StorageNumber " +
                    ", [ProductPlacement].StorageID " +
                    ") " +
                    "SELECT * " +
                    "FROM [ProductPlacement] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].ID = [ProductPlacement].StorageID " +
                    "WHERE [ProductPlacement].ID IN ( " +
                    "SELECT [ID] " +
                    "FROM [Search_CTE] " +
                    ")",
                    (placement, storage) => {
                        placement.Storage = storage;

                        foreach (PackingList packingList in supplyInvoiceToReturn
                                     .PackingLists
                                     .Where(p =>
                                         p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))
                                         ||
                                         p.PackingListBoxes.Any(b =>
                                             b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                         ||
                                         p.PackingListPallets.Any(b =>
                                             b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                     )) {
                            foreach (PackingListPackageOrderItem item in packingList
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                            foreach (PackingListPackage box in packingList
                                         .PackingListBoxes
                                         .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                            foreach (PackingListPackageOrderItem item in box
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                            foreach (PackingListPackage pallet in packingList
                                         .PackingListPallets
                                         .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                            foreach (PackingListPackageOrderItem item in pallet
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);
                        }

                        return placement;
                    },
                    new { Ids = productIds }
                );
        }

        _connection.Query<PackingList, InvoiceDocument, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "WHERE [PackingList].SupplyInvoiceID = @Id " +
            "AND [PackingList].Deleted = 0",
            (packingList, document) => {
                if (document != null) supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(packingList.Id)).InvoiceDocuments.Add(document);

                return packingList;
            },
            packingListProps
        );

        return supplyInvoiceToReturn;
    }

    public SupplyInvoice GetByNetIdAndCultureWithAllIncludes(Guid netId, string culture) {
        SupplyInvoice supplyInvoiceToReturn = null;

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyOrder),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], SupplyInvoice> invoiceMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            InvoiceDocument supplyInvoiceDocument = (InvoiceDocument)objects[1];
            SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey paymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            User supplyOrderPaymentDeliveryProtocolUser = (User)objects[4];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[5];
            User supplyPaymentTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol informationDeliveryProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey informationDeliveryProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            User informationDeliveryProtocolUser = (User)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            ClientAgreement clientAgreement = (ClientAgreement)objects[11];
            Agreement agreement = (Agreement)objects[12];
            Currency currency = (Currency)objects[13];

            if (supplyInvoiceToReturn != null) {
                if (supplyInvoiceDocument != null && !supplyInvoiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(supplyInvoiceDocument.Id)))
                    supplyInvoiceToReturn.InvoiceDocuments.Add(supplyInvoiceDocument);

                if (paymentDeliveryProtocol != null && !supplyInvoiceToReturn.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentDeliveryProtocol.Id))) {
                    if (supplyPaymentTask != null) {
                        supplyPaymentTask.User = supplyPaymentTaskUser;

                        paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                    }

                    paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                    paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                    supplyInvoiceToReturn.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                }

                if (informationDeliveryProtocol != null && !supplyInvoiceToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationDeliveryProtocol.Id))) {
                    informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                    informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                    supplyInvoiceToReturn.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                }
            } else {
                if (supplyInvoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(supplyInvoiceDocument);

                if (paymentDeliveryProtocol != null) {
                    if (supplyPaymentTask != null) {
                        supplyPaymentTask.User = supplyPaymentTaskUser;

                        paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                    }

                    paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                    paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                    supplyInvoice.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                }

                if (informationDeliveryProtocol != null) {
                    informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                    informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                    supplyInvoice.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                }

                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                supplyOrder.ClientAgreement = clientAgreement;

                supplyInvoice.SupplyOrder = supplyOrder;

                supplyInvoiceToReturn = supplyInvoice;
            }

            return supplyInvoice;
        };

        var props = new { NetId = netId, Culture = culture };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [InvoiceDocument] AS [SupplyInvoiceDocument] " +
            "ON [SupplyInvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] " +
            "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
            "ON [SupplyOrderPaymentDeliveryProtocol].UserID = [SupplyOrderPaymentDeliveryProtocolUser].ID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] " +
            "ON [SupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [SupplyInformationDeliveryProtocolKey] " +
            "ON [SupplyInformationDeliveryProtocolKey].ID = [SupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "AND [SupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [SupplyInformationDeliveryProtocolUser] " +
            "ON [SupplyInformationDeliveryProtocolUser].ID = [SupplyInformationDeliveryProtocol].UserID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [SupplyOrder].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Agreement].CurrencyID = [Currency].ID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            types,
            invoiceMapper,
            props
        );

        if (supplyInvoiceToReturn == null) return supplyInvoiceToReturn;

        bool isPlCulture = culture.ToLower().Equals("pl");

        types = new[] {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        Func<object[], SupplyInvoice> orderItemsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSpecification productSpecification = (ProductSpecification)objects[5];
            User user = (User)objects[6];

            if (supplyInvoiceOrderItem != null) {
                if (!supplyInvoiceToReturn.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                    if (productSpecification != null) {
                        productSpecification.AddedBy = user;

                        product.ProductSpecifications.Add(productSpecification);
                    }

                    product.MeasureUnit = measureUnit;

                    product.Name =
                        isPlCulture
                            ? product.NameUA
                            : product.NameUA;

                    if (supplyOrderItem != null) {
                        supplyOrderItem.Product = product;

                        supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                        supplyOrderItem.NetWeight = Math.Round(supplyOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        supplyOrderItem.GrossWeight = Math.Round(supplyOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);
                    }

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    supplyInvoiceToReturn.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
                } else if (productSpecification != null) {
                    SupplyInvoiceOrderItem fromList = supplyInvoiceToReturn.SupplyInvoiceOrderItems.First(i => i.Id.Equals(supplyInvoiceOrderItem.Id));

                    if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id))) fromList.Product.ProductSpecifications.Add(productSpecification);
                }
            }

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            types,
            orderItemsMapper,
            props
        );

        types = new[] {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackage),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductPlacement),
            typeof(ProductPlacement)
        };

        List<long> productIds = new();

        Func<object[], PackingList> packingListMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
            SupplyInvoiceOrderItem packageSupplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem packageSupplyOrderItem = (SupplyOrderItem)objects[9];
            Product packageProduct = (Product)objects[10];
            MeasureUnit packageProductMeasureUnit = (MeasureUnit)objects[11];
            ProductPlacement itemProductPlacement = (ProductPlacement)objects[12];
            ProductPlacement packageItemProductPlacement = (ProductPlacement)objects[13];

            if (packingList == null) return null;

            if (!supplyInvoiceToReturn.PackingLists.Any(p => p.Id.Equals(packingList.Id))) {
                if (packingListPackageOrderItem != null) {
                    product.MeasureUnit = measureUnit;

                    product.Name =
                        isPlCulture
                            ? product.NameUA
                            : product.NameUA;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    if (!productIds.Any(p => p.Equals(product.Id))) productIds.Add(product.Id);

                    if (supplyOrderItem != null) {
                        supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                        supplyOrderItem.NetWeight = Math.Round(supplyOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        supplyOrderItem.GrossWeight = Math.Round(supplyOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);
                    }

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingListPackageOrderItem.TotalNetWeight =
                        packingListPackageOrderItem.NetWeight * packingListPackageOrderItem.Qty;
                    packingListPackageOrderItem.TotalGrossWeight =
                        packingListPackageOrderItem.GrossWeight * packingListPackageOrderItem.Qty;

                    packingListPackageOrderItem.UnitPrice = decimal.Round(packingListPackageOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    decimal exchangeRate =
                        packingListPackageOrderItem.ExchangeRateAmount > 0
                            ? packingListPackageOrderItem.ExchangeRateAmount
                            : 1m;

                    packingListPackageOrderItem.TotalNetPrice =
                        packingListPackageOrderItem.TotalGrossPrice =
                            decimal.Round(
                                packingListPackageOrderItem.UnitPrice * exchangeRate * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    packingListPackageOrderItem.VatAmount =
                        decimal.Round(
                            packingListPackageOrderItem.TotalGrossPrice * packingListPackageOrderItem.VatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.TotalNetPriceEur =
                        decimal.Round(
                            packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.TotalGrossPriceEur =
                        decimal.Round(
                            packingListPackageOrderItem.GrossUnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.VatAmountEur =
                        decimal.Round(
                            packingListPackageOrderItem.TotalGrossPriceEur * packingListPackageOrderItem.VatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingList.TotalNetPriceEur =
                        decimal.Round(packingList.TotalNetPriceEur + packingListPackageOrderItem.TotalNetPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.TotalGrossPriceEur =
                        decimal.Round(packingList.TotalGrossPriceEur + packingListPackageOrderItem.TotalGrossPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.TotalNetWeight = packingList.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight;
                    packingList.TotalGrossWeight = packingList.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight;

                    packingList.TotalNetPrice =
                        decimal.Round(packingList.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                    packingList.TotalGrossPrice =
                        decimal.Round(packingList.TotalGrossPrice + packingListPackageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                    supplyInvoiceToReturn.TotalNetWeight =
                        supplyInvoiceToReturn.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight;
                    supplyInvoiceToReturn.TotalGrossWeight =
                        supplyInvoiceToReturn.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight;

                    packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                    packingListPackageOrderItem.GrossWeight = Math.Round(packingListPackageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                    packingListPackageOrderItem.TotalNetWeight =
                        Math.Round(packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                    packingListPackageOrderItem.TotalGrossWeight =
                        Math.Round(packingListPackageOrderItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

                    supplyInvoiceToReturn.TotalQuantity = supplyInvoiceToReturn.TotalQuantity + packingListPackageOrderItem.Qty;

                    packingList.TotalQuantity = packingList.TotalQuantity + packingListPackageOrderItem.Qty;

                    supplyInvoiceToReturn.TotalNetPrice =
                        decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                    supplyInvoiceToReturn.TotalGrossPrice =
                        decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packingListPackageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                    if (itemProductPlacement != null) packingListPackageOrderItem.ProductPlacements.Add(itemProductPlacement);

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                }

                if (package != null) {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.TotalQuantity = packingList.TotalQuantity + packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    if (package.Type.Equals(PackingListPackageType.Box))
                        packingList.PackingListBoxes.Add(package);
                    else
                        packingList.PackingListPallets.Add(package);
                }

                supplyInvoiceToReturn.PackingLists.Add(packingList);
            } else {
                PackingList fromList = supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(packingList.Id));

                if (packingListPackageOrderItem != null) {
                    if (!fromList.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCulture
                                ? product.NameUA
                                : product.NameUA;

                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        if (!productIds.Any(p => p.Equals(product.Id))) productIds.Add(product.Id);

                        if (supplyOrderItem != null) {
                            supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                            supplyOrderItem.NetWeight = Math.Round(supplyOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                            supplyOrderItem.GrossWeight = Math.Round(supplyOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);
                        }

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                        supplyInvoiceOrderItem.Product = product;

                        supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                        packingListPackageOrderItem.TotalNetWeight =
                            packingListPackageOrderItem.NetWeight * packingListPackageOrderItem.Qty;
                        packingListPackageOrderItem.TotalGrossWeight =
                            packingListPackageOrderItem.GrossWeight * packingListPackageOrderItem.Qty;

                        packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.GrossWeight = Math.Round(packingListPackageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.UnitPrice = decimal.Round(packingListPackageOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                        decimal exchangeRate =
                            packingListPackageOrderItem.ExchangeRateAmount > 0
                                ? packingListPackageOrderItem.ExchangeRateAmount
                                : 1m;

                        packingListPackageOrderItem.TotalNetPrice =
                            packingListPackageOrderItem.TotalGrossPrice =
                                decimal.Round(
                                    packingListPackageOrderItem.UnitPrice * exchangeRate * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                        packingListPackageOrderItem.VatAmount =
                            decimal.Round(
                                packingListPackageOrderItem.TotalGrossPrice * packingListPackageOrderItem.VatPercent / 100,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalNetPriceEur =
                            decimal.Round(
                                packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalGrossPriceEur =
                            decimal.Round(
                                packingListPackageOrderItem.GrossUnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.VatAmountEur =
                            decimal.Round(
                                packingListPackageOrderItem.TotalGrossPriceEur * packingListPackageOrderItem.VatPercent / 100,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.TotalNetPriceEur =
                            decimal.Round(fromList.TotalNetPriceEur + packingListPackageOrderItem.TotalNetPriceEur, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalGrossPriceEur =
                            decimal.Round(fromList.TotalGrossPriceEur + packingListPackageOrderItem.TotalGrossPriceEur, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalNetWeight = fromList.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight;
                        fromList.TotalGrossWeight = fromList.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight;

                        fromList.TotalNetPrice =
                            decimal.Round(fromList.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        fromList.TotalGrossPrice =
                            decimal.Round(fromList.TotalGrossPrice + packingListPackageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            supplyInvoiceToReturn.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight;
                        supplyInvoiceToReturn.TotalGrossWeight =
                            supplyInvoiceToReturn.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight;

                        packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.GrossWeight = Math.Round(packingListPackageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.TotalNetWeight =
                            Math.Round(packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.TotalGrossWeight =
                            Math.Round(packingListPackageOrderItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalQuantity = supplyInvoiceToReturn.TotalQuantity + packingListPackageOrderItem.Qty;

                        fromList.TotalQuantity = fromList.TotalQuantity + packingListPackageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packingListPackageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (itemProductPlacement != null) packingListPackageOrderItem.ProductPlacements.Add(itemProductPlacement);

                        fromList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                    } else if (itemProductPlacement != null) {
                        fromList
                            .PackingListPackageOrderItems
                            .First(i => i.Id.Equals(packingListPackageOrderItem.Id))
                            .ProductPlacements
                            .Add(itemProductPlacement);
                    }
                }

                if (package != null) {
                    if (package.Type.Equals(PackingListPackageType.Box)) {
                        if (fromList.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                            PackingListPackage packageFromList = fromList.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                            if (packageOrderItem != null) {
                                if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                    packageProduct.MeasureUnit = packageProductMeasureUnit;

                                    if (packageSupplyOrderItem != null)
                                        packageSupplyOrderItem.Product = packageProduct;

                                    if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                                    packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                    packageSupplyInvoiceOrderItem.Product = product;

                                    packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                    packageOrderItem.TotalNetWeight =
                                        Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.TotalGrossWeight =
                                        Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalNetPrice =
                                        decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalGrossPrice =
                                        decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                            MidpointRounding.AwayFromZero);

                                    fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                    fromList.TotalGrossWeight = Math.Round(fromList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                                    fromList.TotalNetPrice =
                                        decimal.Round(fromList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                                    fromList.TotalGrossPrice =
                                        decimal.Round(fromList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                    supplyInvoiceToReturn.TotalNetWeight =
                                        Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                    supplyInvoiceToReturn.TotalGrossWeight =
                                        Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                                    supplyInvoiceToReturn.TotalQuantity = supplyInvoiceToReturn.TotalQuantity + packageOrderItem.Qty;

                                    fromList.TotalQuantity = fromList.TotalQuantity + packageOrderItem.Qty;

                                    supplyInvoiceToReturn.TotalNetPrice =
                                        decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                    supplyInvoiceToReturn.TotalGrossPrice =
                                        decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                                    if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                    packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                } else if (packageItemProductPlacement != null) {
                                    packageFromList
                                        .PackingListPackageOrderItems
                                        .First(p => p.Id.Equals(packageOrderItem.Id))
                                        .ProductPlacements
                                        .Add(packageItemProductPlacement);
                                }
                            }
                        } else {
                            if (packageOrderItem != null) {
                                packageProduct.MeasureUnit = packageProductMeasureUnit;

                                if (packageSupplyOrderItem != null)
                                    packageSupplyOrderItem.Product = packageProduct;

                                if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                                packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                packageSupplyInvoiceOrderItem.Product = product;

                                packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                packageOrderItem.TotalNetWeight =
                                    Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                packageOrderItem.TotalGrossWeight =
                                    Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                packageOrderItem.TotalNetPrice =
                                    decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                packageOrderItem.TotalGrossPrice =
                                    decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                        MidpointRounding.AwayFromZero);

                                fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                fromList.TotalGrossWeight = Math.Round(fromList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                                fromList.TotalNetPrice =
                                    decimal.Round(fromList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                                fromList.TotalGrossPrice =
                                    decimal.Round(fromList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                supplyInvoiceToReturn.TotalNetWeight =
                                    Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                supplyInvoiceToReturn.TotalGrossWeight =
                                    Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                                supplyInvoiceToReturn.TotalQuantity = supplyInvoiceToReturn.TotalQuantity + packageOrderItem.Qty;

                                fromList.TotalQuantity = fromList.TotalQuantity + packageOrderItem.Qty;

                                supplyInvoiceToReturn.TotalNetPrice =
                                    decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                supplyInvoiceToReturn.TotalGrossPrice =
                                    decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                                if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                package.PackingListPackageOrderItems.Add(packageOrderItem);
                            }

                            fromList.PackingListBoxes.Add(package);
                        }
                    } else {
                        if (fromList.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                            PackingListPackage packageFromList = fromList.PackingListPallets.First(p => p.Id.Equals(package.Id));

                            if (packageOrderItem != null) {
                                if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                    packageProduct.MeasureUnit = packageProductMeasureUnit;

                                    if (packageSupplyOrderItem != null)
                                        packageSupplyOrderItem.Product = packageProduct;

                                    if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                                    packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                    packageSupplyInvoiceOrderItem.Product = product;

                                    packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                    packageOrderItem.TotalNetWeight =
                                        Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.TotalGrossWeight =
                                        Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalNetPrice =
                                        decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalGrossPrice =
                                        decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                            MidpointRounding.AwayFromZero);

                                    fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                    fromList.TotalGrossWeight = Math.Round(fromList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                                    fromList.TotalNetPrice =
                                        decimal.Round(fromList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                                    fromList.TotalGrossPrice =
                                        decimal.Round(fromList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                    supplyInvoiceToReturn.TotalNetWeight =
                                        Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                    supplyInvoiceToReturn.TotalGrossWeight =
                                        Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                                    supplyInvoiceToReturn.TotalQuantity = supplyInvoiceToReturn.TotalQuantity + packageOrderItem.Qty;

                                    fromList.TotalQuantity = fromList.TotalQuantity + packageOrderItem.Qty;

                                    supplyInvoiceToReturn.TotalNetPrice =
                                        decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                    supplyInvoiceToReturn.TotalGrossPrice =
                                        decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                                    if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                    packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                } else if (packageItemProductPlacement != null) {
                                    packageFromList
                                        .PackingListPackageOrderItems
                                        .First(p => p.Id.Equals(packageOrderItem.Id))
                                        .ProductPlacements
                                        .Add(packageItemProductPlacement);
                                }
                            }
                        } else {
                            if (packageOrderItem != null) {
                                packageProduct.MeasureUnit = packageProductMeasureUnit;

                                if (packageSupplyOrderItem != null)
                                    packageSupplyOrderItem.Product = packageProduct;

                                if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                                packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                packageSupplyInvoiceOrderItem.Product = product;

                                packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                packageOrderItem.TotalNetWeight =
                                    Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                packageOrderItem.TotalGrossWeight =
                                    Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                packageOrderItem.TotalNetPrice =
                                    decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                packageOrderItem.TotalGrossPrice =
                                    decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                        MidpointRounding.AwayFromZero);

                                fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                fromList.TotalGrossWeight = Math.Round(fromList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                                fromList.TotalNetPrice =
                                    decimal.Round(fromList.TotalNetPrice + packageOrderItem.TotalNetPrice, 3, MidpointRounding.AwayFromZero);
                                fromList.TotalGrossPrice =
                                    decimal.Round(fromList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                supplyInvoiceToReturn.TotalNetWeight =
                                    Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                                supplyInvoiceToReturn.TotalGrossWeight =
                                    Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                                supplyInvoiceToReturn.TotalQuantity = supplyInvoiceToReturn.TotalQuantity + packageOrderItem.Qty;

                                fromList.TotalQuantity = fromList.TotalQuantity + packageOrderItem.Qty;

                                supplyInvoiceToReturn.TotalNetPrice =
                                    decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 3, MidpointRounding.AwayFromZero);

                                supplyInvoiceToReturn.TotalGrossPrice =
                                    decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                                if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                package.PackingListPackageOrderItems.Add(packageOrderItem);
                            }

                            fromList.PackingListPallets.Add(package);
                        }
                    }
                }
            }

            return packingList;
        };

        var packingListProps = new { supplyInvoiceToReturn.Id, Culture = culture };

        _connection.Query(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackage] AS [Pallet] " +
            "ON [PackingList].ID = [Pallet].PackingListID " +
            "AND [Pallet].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [PalletPackageOrderItem] " +
            "ON [Pallet].ID = [PalletPackageOrderItem].PackingListPackageID " +
            "AND [PalletPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] AS [PalletInvoiceOrderItem] " +
            "ON [PalletPackageOrderItem].SupplyInvoiceOrderItemID = [PalletInvoiceOrderItem].ID " +
            "LEFT JOIN [SupplyOrderItem] AS [PalletOrderItem] " +
            "ON [PalletInvoiceOrderItem].SupplyOrderItemID = [PalletOrderItem].ID " +
            "LEFT JOIN [Product] AS [PalletOrderItemProduct] " +
            "ON [PalletOrderItemProduct].ID = [PalletOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [PalletOrderItemProductMeasureUnit] " +
            "ON [PalletOrderItemProductMeasureUnit].ID = [PalletOrderItemProduct].MeasureUnitID " +
            "AND [PalletOrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductPlacement] AS [ItemProductPlacement] " +
            "ON [ItemProductPlacement].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductPlacement] AS [PackageProductPlacement] " +
            "ON [PackageProductPlacement].PackingListPackageOrderItemID = [PalletPackageOrderItem].ID " +
            "WHERE [PackingList].SupplyInvoiceID = @Id " +
            "AND [PackingList].Deleted = 0 ",
            types,
            packingListMapper,
            packingListProps
        );

        foreach (PackingList packingList in supplyInvoiceToReturn.PackingLists) {
            packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight, 2);
            packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight, 2);
        }

        supplyInvoiceToReturn.TotalNetWeight = Math.Round(supplyInvoiceToReturn.TotalNetWeight, 2);
        supplyInvoiceToReturn.TotalGrossWeight = Math.Round(supplyInvoiceToReturn.TotalGrossWeight, 2);

        if (productIds.Any()) {
            if (productIds.Count > 2000)
                for (int j = 0; j < productIds.Count % 2000; j++)
                    _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                        ";WITH [Search_CTE] " +
                        "AS ( " +
                        "SELECT MAX([ID]) AS [ID] " +
                        "FROM [ProductPlacement] " +
                        "WHERE [ProductPlacement].ProductID IN @Ids " +
                        "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                        "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                        "GROUP BY [ProductPlacement].CellNumber " +
                        ", [ProductPlacement].RowNumber " +
                        ", [ProductPlacement].StorageNumber " +
                        ", [ProductPlacement].StorageID " +
                        ") " +
                        "SELECT * " +
                        "FROM [ProductPlacement] " +
                        "LEFT JOIN [Storage] " +
                        "ON [Storage].ID = [ProductPlacement].StorageID " +
                        "WHERE [ProductPlacement].ID IN ( " +
                        "SELECT [ID] " +
                        "FROM [Search_CTE] " +
                        ")",
                        (placement, storage) => {
                            placement.Storage = storage;

                            foreach (PackingList packingList in supplyInvoiceToReturn
                                         .PackingLists
                                         .Where(p =>
                                             p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))
                                             ||
                                             p.PackingListBoxes.Any(b =>
                                                 b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                             ||
                                             p.PackingListPallets.Any(b =>
                                                 b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                         )) {
                                foreach (PackingListPackageOrderItem item in packingList
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                                foreach (PackingListPackage box in packingList
                                             .PackingListBoxes
                                             .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                                foreach (PackingListPackageOrderItem item in box
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                                foreach (PackingListPackage pallet in packingList
                                             .PackingListPallets
                                             .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                                foreach (PackingListPackageOrderItem item in pallet
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);
                            }

                            return placement;
                        },
                        new { Ids = productIds.Skip(j * 2000).Take(2000) }
                    );
            else
                _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT MAX([ID]) AS [ID] " +
                    "FROM [ProductPlacement] " +
                    "WHERE [ProductPlacement].ProductID IN @Ids " +
                    "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                    "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                    "GROUP BY [ProductPlacement].CellNumber " +
                    ", [ProductPlacement].RowNumber " +
                    ", [ProductPlacement].StorageNumber " +
                    ", [ProductPlacement].StorageID " +
                    ") " +
                    "SELECT * " +
                    "FROM [ProductPlacement] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].ID = [ProductPlacement].StorageID " +
                    "WHERE [ProductPlacement].ID IN ( " +
                    "SELECT [ID] " +
                    "FROM [Search_CTE] " +
                    ")",
                    (placement, storage) => {
                        placement.Storage = storage;

                        foreach (PackingList packingList in supplyInvoiceToReturn
                                     .PackingLists
                                     .Where(p =>
                                         p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))
                                         ||
                                         p.PackingListBoxes.Any(b =>
                                             b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                         ||
                                         p.PackingListPallets.Any(b =>
                                             b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                     )) {
                            foreach (PackingListPackageOrderItem item in packingList
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                            foreach (PackingListPackage box in packingList
                                         .PackingListBoxes
                                         .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                            foreach (PackingListPackageOrderItem item in box
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                            foreach (PackingListPackage pallet in packingList
                                         .PackingListPallets
                                         .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                            foreach (PackingListPackageOrderItem item in pallet
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);
                        }

                        return placement;
                    },
                    new { Ids = productIds }
                );
        }

        _connection.Query<PackingList, InvoiceDocument, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "WHERE [PackingList].SupplyInvoiceID = @Id " +
            "AND [PackingList].Deleted = 0",
            (packingList, document) => {
                if (document != null) supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(packingList.Id)).InvoiceDocuments.Add(document);

                return packingList;
            },
            packingListProps
        );

        return supplyInvoiceToReturn;
    }

    public IEnumerable<SupplyInvoice> GetAllInvoicesFromApprovedSupplyOrder(
        SupplyTransportationType transportationType,
        string culture,
        long protocolId) {
        List<SupplyInvoice> toReturn = new();

        string sqlQuery = ";WITH [TOTAL_VALUE_CTE] AS ( " +
                          "SELECT [SupplyInvoice].[ID] " +
                          ",ROUND(SUM(ROUND([PackingListPackageOrderItem].[UnitPriceEur],2) * [PackingListPackageOrderItem].[Qty]),2) AS [TotalNetPrice] " +
                          "FROM [SupplyInvoice] " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                          "WHERE [SupplyOrder].[TransportationType] = @TransportationType " +
                          "AND [Organization].[Culture] = @Culture " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "AND [SupplyOrder].[IsApproved] = 1 " +
                          "AND [SupplyOrder].[Deleted] = 0 " +
                          "AND [SupplyInvoice].[IsPartiallyPlaced] = 0 " +
                          "AND [SupplyInvoice].[IsFullyPlaced] = 0 " +
                          "AND ([SupplyInvoice].[DeliveryProductProtocolID] IS NULL " +
                          "OR [SupplyInvoice].[DeliveryProductProtocolID] = @ProtocolId) " +
                          "GROUP BY [SupplyInvoice].[ID] " +
                          ") " +
                          "SELECT " +
                          "[SupplyInvoice].* " +
                          ",[TOTAL_VALUE_CTE].[TotalNetPrice] " +
                          ",[SupplyOrder].* " +
                          ",[Organization].* " +
                          ",[Client].* " +
                          ",[ClientAgreement].* " +
                          ",[Agreement].* " +
                          ",[Currency].* " +
                          "FROM [SupplyInvoice] " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "LEFT JOIN [Currency] " +
                          "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                          "LEFT JOIN [TOTAL_VALUE_CTE] " +
                          "ON [TOTAL_VALUE_CTE].[ID] = [SupplyInvoice].[ID] " +
                          "WHERE [TOTAL_VALUE_CTE].[ID] IS NOT NULL ";

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            Organization organization = (Organization)objects[2];
            Client client = (Client)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Currency currency = (Currency)objects[6];

            if (!toReturn.Any(x => x.Id.Equals(invoice.Id)))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id.Equals(invoice.Id));

            invoice.TotalNetPrice = invoice.NetPrice + invoice.DeliveryAmount - invoice.DiscountAmount;

            supplyOrder.Organization = organization;
            supplyOrder.Client = client;
            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.ClientAgreement = clientAgreement;
            invoice.SupplyOrder = supplyOrder;

            return invoice;
        };

        _connection.Query(sqlQuery, types, mapper,
            new { Culture = culture, TransportationType = transportationType, ProtocolId = protocolId });

        return toReturn;
    }

    public void RemoveAllSupplyInvoiceFromDeliveryProductProtocolById(long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [SupplyInvoice].[DeliveryProductProtocolID] = NULL " +
            ", [Updated] = getutcdate() " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id; ",
            new { Id = id });
    }

    public void UnassignAllByDeliveryProductProtocolIdExceptProvided(long protocolId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [DeliveryProductProtocolID] = NULL " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id AND [SupplyInvoice].[ID] NOT IN @Ids",
            new { Id = protocolId, Ids = ids }
        );
    }

    public void AssignProvidedToDeliveryProductProtocol(long protocolId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [DeliveryProductProtocolID] = @Id " +
            "WHERE [SupplyInvoice].[ID] IN @Ids",
            new { Id = protocolId, Ids = ids }
        );
    }

    public List<SupplyInvoice> GetByBillOfLadingServiceId(long id, long protocolId) {
        List<SupplyInvoice> toReturn = new();

        Type[] supplyInvoiceTypes = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(SupplyProForm)
        };

        Func<object[], SupplyInvoice> supplyInvoiceMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[2];
            SupplyOrder supplyOrder = (SupplyOrder)objects[3];
            Client client = (Client)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[8];
            Organization organization = (Organization)objects[9];
            SupplyProForm supplyProForm = (SupplyProForm)objects[10];

            if (!toReturn.Any(x => x.Id.Equals(supplyInvoice.Id)))
                toReturn.Add(supplyInvoice);
            else
                supplyInvoice = toReturn.First(x => x.Id.Equals(supplyInvoice.Id));

            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.Client = client;
            supplyOrder.ClientAgreement = clientAgreement;
            supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            supplyOrder.Organization = organization;
            supplyOrder.SupplyProForm = supplyProForm;

            supplyInvoice.SupplyOrder = supplyOrder;

            if (packingList == null) return supplyInvoice;

            if (!supplyInvoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                supplyInvoice.PackingLists.Add(packingList);
            else
                packingList =
                    supplyInvoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

            packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT [SupplyInvoice].* " +
            ", ROUND (" +
            "[SupplyInvoice].[NetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], 2 " +
            ") AS [TotalNetPrice] " +
            ", [PackingList].* " +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyOrder].* " +
            ", [Client].* " +
            ", [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Currency].* " +
            ", [SupplyOrderNumber].* " +
            ", [Organization].* " +
            ", [SupplyProForm].* " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList]  " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "AND [SupplyOrder].[Deleted] = 0 " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].[ID] = [SupplyOrder].[SupplyProFormID] " +
            "LEFT JOIN [SupplyInvoiceBillOfLadingService] " +
            "ON [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @Id " +
            "AND [SupplyInvoiceBillOfLadingService].[Deleted] = 0 " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            supplyInvoiceTypes, supplyInvoiceMapper, new { Id = id });

        Type[] supplyInvoicesType = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization)
        };

        Func<object[], SupplyInvoice> supplyInvoicesMapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[2];
            SupplyOrder supplyOrder = (SupplyOrder)objects[3];
            Client client = (Client)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Organization organization = (Organization)objects[8];

            if (!toReturn.Any(x => x.Id.Equals(invoice.Id)))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id.Equals(invoice.Id));

            supplyOrder.Organization = organization;
            supplyOrder.Client = client;

            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.ClientAgreement = clientAgreement;

            invoice.SupplyOrder = supplyOrder;

            if (packingList == null) return invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

            if (item == null) return invoice;

            item.TotalNetPrice = Convert.ToDecimal(item.Qty) * item.UnitPrice;

            invoice.TotalQuantity = +item.Qty;

            packingList.PackingListPackageOrderItems.Add(item);

            return invoice;
        };

        _connection.Query(
            "SELECT " +
            "[SupplyInvoice].* " +
            ", ROUND (" +
            "[SupplyInvoice].[NetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], 2 " +
            ") AS [TotalNetPrice] " +
            ", [PackingList].* " +
            ", [PackingListPackageOrderItem].*  " +
            ", [SupplyOrder].*  " +
            ", [Client].*  " +
            ", [ClientAgreement].*  " +
            ", [Agreement].*  " +
            ", [Currency].*  " +
            ", [Organization].*  " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            supplyInvoicesType, supplyInvoicesMapper,
            new { Id = protocolId });

        return toReturn;
    }

    public List<SupplyInvoice> GetByMergedServiceId(long id, long protocolId) {
        List<SupplyInvoice> toReturn = new();

        Type[] supplyInvoiceTypes = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(SupplyProForm)
        };

        Func<object[], SupplyInvoice> supplyInvoiceMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[2];
            SupplyOrder supplyOrder = (SupplyOrder)objects[3];
            Client client = (Client)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[8];
            Organization organization = (Organization)objects[9];
            SupplyProForm supplyProForm = (SupplyProForm)objects[10];

            if (!toReturn.Any(x => x.Id.Equals(supplyInvoice.Id)))
                toReturn.Add(supplyInvoice);
            else
                supplyInvoice = toReturn.First(x => x.Id.Equals(supplyInvoice.Id));

            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.Client = client;
            supplyOrder.ClientAgreement = clientAgreement;
            supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            supplyOrder.Organization = organization;
            supplyOrder.SupplyProForm = supplyProForm;

            supplyInvoice.SupplyOrder = supplyOrder;

            if (packingList == null) return supplyInvoice;

            if (!supplyInvoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                supplyInvoice.PackingLists.Add(packingList);
            else
                packingList =
                    supplyInvoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

            packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT [SupplyInvoice].* " +
            ", ROUND (" +
            "[SupplyInvoice].[NetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], 2 " +
            ") AS [TotalNetPrice] " +
            ", [PackingList].* " +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyOrder].* " +
            ", [Client].* " +
            ", [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Currency].* " +
            ", [SupplyOrderNumber].* " +
            ", [Organization].* " +
            ", [SupplyProForm].* " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList]  " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "AND [SupplyOrder].[Deleted] = 0 " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].[ID] = [SupplyOrder].[SupplyProFormID] " +
            "LEFT JOIN [SupplyInvoiceMergedService] " +
            "ON [SupplyInvoiceMergedService].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @Id " +
            "AND [SupplyInvoiceMergedService].[Deleted] = 0 " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            supplyInvoiceTypes, supplyInvoiceMapper, new { Id = id });

        Type[] supplyInvoicesType = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization)
        };

        Func<object[], SupplyInvoice> supplyInvoicesMapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[2];
            SupplyOrder supplyOrder = (SupplyOrder)objects[3];
            Client client = (Client)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Organization organization = (Organization)objects[8];

            if (!toReturn.Any(x => x.Id.Equals(invoice.Id)))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id.Equals(invoice.Id));

            supplyOrder.Organization = organization;
            supplyOrder.Client = client;

            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.ClientAgreement = clientAgreement;

            invoice.SupplyOrder = supplyOrder;

            if (packingList == null) return invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

            if (item == null) return invoice;

            item.TotalNetPrice = Convert.ToDecimal(item.Qty) * item.UnitPrice;

            invoice.TotalQuantity = +item.Qty;

            packingList.PackingListPackageOrderItems.Add(item);

            return invoice;
        };

        _connection.Query(
            "SELECT " +
            "[SupplyInvoice].* " +
            ", ROUND (" +
            "[SupplyInvoice].[NetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], 2 " +
            ") AS [TotalNetPrice] " +
            ", [PackingList].* " +
            ", [PackingListPackageOrderItem].*  " +
            ", [SupplyOrder].*  " +
            ", [Client].*  " +
            ", [ClientAgreement].*  " +
            ", [Agreement].*  " +
            ", [Currency].*  " +
            ", [Organization].*  " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            supplyInvoicesType, supplyInvoicesMapper,
            new { Id = protocolId });

        return toReturn;
    }

    public List<SupplyInvoice> GetByIds(IEnumerable<long> ids) {
        List<SupplyInvoice> toReturn = new();

        string sqlQuery = "SELECT * " +
                          "FROM [SupplyInvoice] " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "LEFT JOIN [Currency] " +
                          "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "AND [PackingList].[Deleted] = 0 " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                          "LEFT JOIN [DeliveryProductProtocol] " +
                          "ON [DeliveryProductProtocol].[ID] = [SupplyInvoice].[DeliveryProductProtocolID] " +
                          "AND [DeliveryProductProtocol].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyInvoiceOrderItem] " +
                          "ON [SupplyInvoiceOrderItem].[ID] = [PackingListPackageOrderItem].[SupplyInvoiceOrderItemID] " +
                          "LEFT JOIN [SupplyOrderItem] " +
                          "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
                          "LEFT JOIN [Product] " +
                          "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
                          "WHERE [SupplyInvoice].[ID] IN @Ids ";

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(DeliveryProductProtocol),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            Organization organization = (Organization)objects[2];
            Client client = (Client)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Currency currency = (Currency)objects[6];
            PackingList packingList = (PackingList)objects[7];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[8];
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[9];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[10];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[11];
            Product product = (Product)objects[12];

            if (!toReturn.Any(x => x.Id.Equals(invoice.Id)))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id.Equals(invoice.Id));

            supplyOrder.Organization = organization;
            supplyOrder.Client = client;
            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.ClientAgreement = clientAgreement;
            invoice.SupplyOrder = supplyOrder;
            invoice.DeliveryProductProtocol = protocol;

            if (packingList == null) return invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

            if (supplyOrderItem != null)
                supplyOrderItem.Product = product;
            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;
            item.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingList.PackingListPackageOrderItems.Add(item);

            return invoice;
        };

        _connection.Query(sqlQuery, types, mapper,
            new { Ids = ids });

        return toReturn;
    }

    public IEnumerable<long> GetIdBySupplyOrderId(long id) {
        return _connection.Query<long>(
            "SELECT [SupplyInvoice].[ID] " +
            "FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].[SupplyOrderID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0 ",
            new { Id = id }).ToList();
    }

    public IEnumerable<long> GetIdByContainerServiceId(long id) {
        return _connection.Query<long>(
            "SELECT [SupplyInvoice].[ID] " +
            "FROM [ContainerService] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ContainerServiceID] = [ContainerService].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "WHERE [ContainerService].[ID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0 " +
            "GROUP BY [SupplyInvoice].[ID] ",
            new { Id = id }).ToList();
    }

    public IEnumerable<long> GetIdByVehicleServiceId(long id) {
        return _connection.Query<long>(
            "SELECT [SupplyInvoice].[ID] " +
            "FROM [VehicleService] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[VehicleServiceID] = [VehicleService].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "WHERE [VehicleService].[ID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0 " +
            "GROUP BY [SupplyInvoice].[ID] ",
            new { Id = id }).ToList();
    }

    public void UpdateIsShippedByDeliveryProductProtocolId(long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [IsShipped] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id; ",
            new { Id = id });
    }

    public TotalInvoicesItem GetTotalQtyNotArrivedInvoices() {
        return _connection.Query<TotalInvoicesItem>(
            ";WITH [SUPPLY_INVOICE_LIST_CTE] AS ( " +
            "SELECT " +
            "[SupplyInvoice].[ID] " +
            ",[SupplyInvoice].[IsShipped] " +
            "FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].[Deleted] = 0 " +
            "AND [SupplyInvoice].[IsPartiallyPlaced] = 0 " +
            "AND [SupplyInvoice].[IsFullyPlaced] = 0 " +
            ") " +
            "SELECT ( " +
            "SELECT " +
            "COUNT(1) " +
            "FROM [SUPPLY_INVOICE_LIST_CTE] " +
            "WHERE [SUPPLY_INVOICE_LIST_CTE].[IsShipped] = 0 " +
            ") AS [QtyInSupplier] " +
            ",( " +
            "SELECT " +
            "COUNT(1) " +
            "FROM [SUPPLY_INVOICE_LIST_CTE] " +
            "WHERE [SUPPLY_INVOICE_LIST_CTE].[IsShipped] = 1 " +
            ") AS [QtyInRoad] "
        ).FirstOrDefault();
    }

    public List<OrderedInvoiceModel> GetOrderedInvoicesByIsShipped(TypeIsShippedInvoices type) {
        return _connection.Query<OrderedInvoiceModel>(
            "SELECT [SupplyInvoice].[NetUID] AS [NetId] " +
            ",[SupplyInvoice].[Number] " +
            ",[SupplyOrder].[NetUID] AS [OrderNetId] " +
            ",[SupplyOrderNumber].[Number] AS [OrderNumber] " +
            ",[DeliveryProductProtocol].[NetUID] AS [ProtocolNetId] " +
            ",[DeliveryProductProtocolNumber].[Number] AS [ProtocolNumber] " +
            ",[Client].[FullName] AS [SupplierName] " +
            ",[SupplyInvoice].[NetPrice] " +
            ",[SupplyInvoice].[IsShipped] " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [SupplyInvoice].[DeliveryProductProtocolID] " +
            "LEFT JOIN [DeliveryProductProtocolNumber] " +
            "ON [DeliveryProductProtocolNumber].[ID] = [DeliveryProductProtocol].[DeliveryProductProtocolNumberID] " +
            "WHERE [SupplyInvoice].[Deleted] = 0 " +
            "AND [SupplyInvoice].[IsFullyPlaced] = 0 " +
            "AND [SupplyInvoice].[IsPartiallyPlaced] = 0 " +
            (type.Equals(TypeIsShippedInvoices.NotIsShipped)
                ? "AND [SupplyInvoice].[IsShipped] = 0; "
                : (type.Equals(TypeIsShippedInvoices.IsShipped))
                    ? "AND [SupplyInvoice].[IsShipped] = 1; "
                    : "")).ToList();
    }

    public SupplyInvoice GetAllSpendingOnServicesByNetId(Guid netId) {
        SupplyInvoice toReturn =
            _connection.Query<SupplyInvoice>(
                "SELECT * FROM [SupplyInvoice] " +
                "WHERE [SupplyInvoice].[NetUID] = @NetId ",
                new { NetId = netId }).First();

        Type[] billOfLadingServiceTypes = {
            typeof(SupplyInvoiceBillOfLadingService),
            typeof(BillOfLadingService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency)
        };

        Func<object[], SupplyInvoiceBillOfLadingService> billOfLadingServiceMapper = objects => {
            SupplyInvoiceBillOfLadingService junction = (SupplyInvoiceBillOfLadingService)objects[0];
            BillOfLadingService service = (BillOfLadingService)objects[1];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[2];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[3];
            Currency currency = (Currency)objects[4];

            if (!toReturn.SupplyInvoiceBillOfLadingServices.Any(x => x.Id.Equals(junction.Id)))
                toReturn.SupplyInvoiceBillOfLadingServices.Add(junction);

            service.SupplyOrganization = supplyOrganization;
            agreement.Currency = currency;
            service.SupplyOrganizationAgreement = agreement;
            junction.BillOfLadingService = service;
            return junction;
        };

        _connection.Query(
            "SELECT " +
            "* " +
            "FROM [SupplyInvoiceBillOfLadingService] " +
            "LEFT JOIN [BillOfLadingService] " +
            "ON [BillOfLadingService].[ID] = [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].[ID] = [BillOfLadingService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [BillOfLadingService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [SupplyInvoiceBillOfLadingService].[Deleted] = 0 " +
            "AND [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] = @Id " +
            "AND [BillOfLadingService].[DeliveryProductProtocolID] = @ProtocolId ",
            billOfLadingServiceTypes, billOfLadingServiceMapper,
            new {
                toReturn.Id,
                ProtocolId = toReturn.DeliveryProductProtocolId
            });

        Type[] mergedServiceTypes = {
            typeof(SupplyInvoiceMergedService),
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(ConsumableProduct)
        };

        Func<object[], SupplyInvoiceMergedService> mergedServiceMapper = objects => {
            SupplyInvoiceMergedService junction = (SupplyInvoiceMergedService)objects[0];
            MergedService service = (MergedService)objects[1];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[2];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[3];
            Currency currency = (Currency)objects[4];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[5];

            if (!toReturn.SupplyInvoiceMergedServices.Any(x => x.Id.Equals(junction.Id)))
                toReturn.SupplyInvoiceMergedServices.Add(junction);

            service.ConsumableProduct = consumableProduct;
            service.SupplyOrganization = supplyOrganization;
            agreement.Currency = currency;
            service.SupplyOrganizationAgreement = agreement;
            junction.MergedService = service;

            return junction;
        };

        _connection.Query(
            "DECLARE @UAH_ID bigint = ( " +
            "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
            "WHERE [Currency].[Deleted] = 0 " +
            "AND [Currency].[Code] = 'UAH' " +
            "); " +
            "DECLARE @EUR_ID bigint = ( " +
            "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
            "WHERE [Currency].[Deleted] = 0 " +
            "AND [Currency].[Code] = 'EUR' " +
            "); " +
            "SELECT " +
            "[SupplyInvoiceMergedService].* " +
            ", (SELECT " +
            "CASE " +
            "WHEN( " +
            "SELECT TOP 1 ( " +
            "CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NULL " +
            "THEN [GovExchangeRate].[Amount] " +
            "ELSE [GovExchangeRateHistory].Amount " +
            "END " +
            ") AS [Amount] " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
            "WHERE [GovExchangeRate].[Code] = 'EUR' " +
            "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
            "AND CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END > [GovExchangeRateHistory].[Created] " +
            "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
            ") IS NOT NULL " +
            "THEN ( " +
            "SELECT TOP 1 ( " +
            "CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NULL " +
            "THEN [GovExchangeRate].[Amount] " +
            "ELSE [GovExchangeRateHistory].Amount " +
            "END " +
            ") AS [Amount] " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
            "WHERE [GovExchangeRate].[Code] = 'EUR' " +
            "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
            "AND CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END > [GovExchangeRateHistory].[Created] " +
            "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
            ") " +
            "ELSE ( " +
            "SELECT TOP 1 [GovExchangeRate].[Amount] " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].[Code] = 'EUR' " +
            "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
            ") " +
            "END " +
            ") AS [ExchangeRateEurToUah] " +
            ", CASE " +
            "WHEN [Currency].[Code] = 'USD' OR [Currency].[Code] = 'EUR' " +
            "THEN " +
            "CASE " +
            "WHEN [Currency].[Code] = 'USD' " +
            "THEN ( " +
            "SELECT TOP 1 ( " +
            "CASE " +
            "WHEN [GovCrossExchangeRateHistory].Amount IS NULL " +
            "THEN [GovCrossExchangeRate].[Amount] " +
            "ELSE [GovCrossExchangeRateHistory].Amount " +
            "END " +
            ") AS [Amount] " +
            "FROM [GovCrossExchangeRateHistory] " +
            "LEFT JOIN [GovCrossExchangeRate] " +
            "ON [GovCrossExchangeRate].[ID] = [GovCrossExchangeRateHistory].[GovCrossExchangeRateID] " +
            "WHERE [GovCrossExchangeRate].[CurrencyFromID] = @EUR_ID " +
            "AND [GovCrossExchangeRate].[CurrencyToID] = [Currency].[ID] " +
            "AND CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END > [GovCrossExchangeRateHistory].[Created] " +
            "ORDER BY [GovCrossExchangeRateHistory].[Created] DESC " +
            ") " +
            "ELSE 1 " +
            "END " +
            "ELSE " +
            "(SELECT " +
            "CASE " +
            "WHEN( " +
            "SELECT TOP 1 ( " +
            "CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NULL " +
            "THEN [GovExchangeRate].[Amount] " +
            "ELSE [GovExchangeRateHistory].Amount " +
            "END " +
            ") AS [Amount] " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
            "WHERE [GovExchangeRate].[Code] = 'EUR' " +
            "AND [GovExchangeRate].[CurrencyID] = [Currency].[ID] " +
            "AND CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END > [GovExchangeRateHistory].[Created] " +
            "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
            ") IS NOT NULL " +
            "THEN ( " +
            "SELECT TOP 1 ( " +
            "CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NULL " +
            "THEN [GovExchangeRate].[Amount] " +
            "ELSE [GovExchangeRateHistory].Amount " +
            "END " +
            ") AS [Amount] " +
            "FROM [GovExchangeRateHistory] " +
            "LEFT JOIN [GovExchangeRate] " +
            "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
            "WHERE [GovExchangeRate].[Code] = 'EUR' " +
            "AND [GovExchangeRate].[CurrencyID] = [Currency].[ID] " +
            "AND CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END > [GovExchangeRateHistory].[Created] " +
            "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
            ") " +
            "ELSE ( " +
            "SELECT TOP 1 [GovExchangeRate].[Amount] " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].[Code] = 'EUR' " +
            "AND [GovExchangeRate].[CurrencyID] = [Currency].[ID] " +
            ") " +
            "END " +
            ") END AS [ExchangeRateEurToAgreementCurrency] " +
            ", [MergedService].* " +
            ", [SupplyOrganization].* " +
            ", [SupplyOrganizationAgreement].* " +
            ", [Currency].* " +
            ", [ConsumableProduct].* " +
            "FROM [SupplyInvoiceMergedService] " +
            "LEFT JOIN [MergedService] " +
            "ON [MergedService].[ID] = [SupplyInvoiceMergedService].[MergedServiceID] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].[ID] = [MergedService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
            "LEFt JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceMergedService].[SupplyInvoiceID] " +
            "WHERE [SupplyInvoiceMergedService].[Deleted] = 0 " +
            "AND [SupplyInvoiceMergedService].[SupplyInvoiceID] = @Id " +
            "AND [MergedService].[DeliveryProductProtocolID] = @ProtocolId ",
            mergedServiceTypes, mergedServiceMapper,
            new {
                toReturn.Id,
                ProtocolId = toReturn.DeliveryProductProtocolId
            });

        return toReturn;
    }

    public List<SupplyInvoice> GetWithConsignmentsByIds(IEnumerable<long> ids) {
        List<SupplyInvoice> toReturn = new();

        string sqlQuery = "SELECT " +
                          "* " +
                          "FROM [SupplyInvoice] " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "LEFT JOIN [Currency] " +
                          "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "AND [PackingList].[Deleted] = 0 " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                          "LEFT JOIN [ProductIncomeItem] " +
                          "ON [ProductIncomeItem].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
                          "AND [ProductIncomeItem].[Deleted] = 0 " +
                          "LEFT JOIN [ConsignmentItem] " +
                          "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
                          "AND [ConsignmentItem].[Deleted] = 0 " +
                          "WHERE [SupplyInvoice].[ID] IN @Ids ";
        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Currency currency = (Currency)objects[4];
            PackingList packingList = (PackingList)objects[5];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[6];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[7];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[8];

            if (!toReturn.Any(x => x.Id.Equals(invoice.Id)))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id.Equals(invoice.Id));

            agreement.Currency = currency;
            clientAgreement.Agreement = agreement;
            supplyOrder.ClientAgreement = clientAgreement;
            invoice.SupplyOrder = supplyOrder;

            if (packingList == null) return invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

            if (packingListPackageOrderItem == null) return invoice;

            if (!packingList.PackingListPackageOrderItems.Any(x => x.Id.Equals(packingListPackageOrderItem.Id)))
                packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
            else
                packingListPackageOrderItem = packingList.PackingListPackageOrderItems.First(x => x.Id.Equals(packingListPackageOrderItem.Id));

            if (productIncomeItem == null) return invoice;

            if (!packingListPackageOrderItem.ProductIncomeItems.Any(x => x.Id.Equals(productIncomeItem.Id)))
                packingListPackageOrderItem.ProductIncomeItems.Add(productIncomeItem);
            else
                productIncomeItem = packingListPackageOrderItem.ProductIncomeItems.First(x => x.Id.Equals(productIncomeItem.Id));

            if (consignmentItem != null)
                productIncomeItem.ConsignmentItems.Add(consignmentItem);

            return invoice;
        };

        _connection.Query(
            sqlQuery, types, mapper, new { Ids = ids });

        return toReturn;
    }

    public void UpdateCustomDeclarationData(SupplyInvoice invoice) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [Updated] = getutcdate() " +
            ",[NumberCustomDeclaration] = @NumberCustomDeclaration " +
            ",[DateCustomDeclaration] = @DateCustomDeclaration " +
            "WHERE [ID] = @Id; ",
            invoice);
    }

    public List<SupplyInvoice> GetBySupplyOrderId(long id) {
        return _connection.Query<SupplyInvoice>(
            "SELECT * FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].[SupplyOrderID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            new { Id = id }).ToList();
    }

    public void RestoreSupplyInvoice(long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 0 " +
            "WHERE [SupplyInvoice].[ID] = @Id; ",
            new { Id = id });
    }

    public SupplyInvoice GetSupplyInvoiceByPackingListNetId(Guid netId) {
        return _connection.Query<SupplyInvoice>(
                "SELECT [SupplyInvoice].* FROM [PackingList] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                "WHERE [PackingList].[NetUID] = @NetId ",
                new { NetId = netId })
            .FirstOrDefault();
    }

    public IEnumerable<SupplyInvoice> GetAllByDeliveryProductProtocolId(long deliveryProductProtocolId, IEnumerable<Guid> invoiceNetIds) {
        List<SupplyInvoice> invoices = new();

        _connection.Query<SupplyInvoice, SupplyInvoiceOrderItem, PackingList, SupplyInvoice>(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id " +
            "AND [SupplyInvoice].NetUID IN @InvoiceNetIds " +
            "AND [SupplyInvoice].Deleted = 0",
            (invoice, invoiceOrderItem, packingList) => {
                if (invoices.Any(x => x.Id == invoice.Id))
                    invoice = invoices.First(x => x.Id == invoice.Id);
                else
                    invoices.Add(invoice);

                if (invoiceOrderItem is not null && invoice.SupplyInvoiceOrderItems.All(x => x.Id != invoiceOrderItem.Id)) invoice.SupplyInvoiceOrderItems.Add(invoiceOrderItem);

                if (packingList is not null && invoice.PackingLists.All(x => x.Id != packingList.Id)) invoice.PackingLists.Add(packingList);

                return invoice;
            },
            new { Id = deliveryProductProtocolId, InvoiceNetIds = invoiceNetIds });

        return invoices;
    }

    public SupplyInvoice GetByNetIdWithAllIncludesForExport(Guid netId) { // TODO Do something with it
        SupplyInvoice supplyInvoiceToReturn = null;

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyOrder),
            typeof(DeliveryProductProtocol),
            typeof(SupplyInvoiceDeliveryDocument),
            typeof(SupplyDeliveryDocument),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(Organization)
        };

        Func<object[], SupplyInvoice> invoiceMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            InvoiceDocument supplyInvoiceDocument = (InvoiceDocument)objects[1];
            SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey paymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            User supplyOrderPaymentDeliveryProtocolUser = (User)objects[4];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[5];
            User supplyPaymentTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol informationDeliveryProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey informationDeliveryProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            User informationDeliveryProtocolUser = (User)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            DeliveryProductProtocol deliveryProductProtocol = (DeliveryProductProtocol)objects[11];
            SupplyInvoiceDeliveryDocument document = (SupplyInvoiceDeliveryDocument)objects[12];
            SupplyDeliveryDocument supplyDocument = (SupplyDeliveryDocument)objects[13];
            ClientAgreement clientAgreement = (ClientAgreement)objects[14];
            Agreement agreement = (Agreement)objects[15];
            Currency currency = (Currency)objects[16];
            Client client = (Client)objects[17];
            Organization organization = (Organization)objects[18];

            if (supplyInvoiceToReturn == null) {
                agreement.Currency = currency;
                clientAgreement.Agreement = agreement;
                supplyOrder.ClientAgreement = clientAgreement;
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyInvoice.SupplyOrder = supplyOrder;
                supplyInvoice.DeliveryProductProtocol = deliveryProductProtocol;
                supplyInvoiceToReturn = supplyInvoice;
            }

            if (supplyInvoiceDocument != null && !supplyInvoiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(supplyInvoiceDocument.Id)))
                supplyInvoiceToReturn.InvoiceDocuments.Add(supplyInvoiceDocument);

            if (paymentDeliveryProtocol != null && !supplyInvoiceToReturn.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentDeliveryProtocol.Id))) {
                if (supplyPaymentTask != null) {
                    supplyPaymentTask.User = supplyPaymentTaskUser;

                    paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                }

                paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                supplyInvoiceToReturn.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
            }

            if (document != null) {
                document.SupplyDeliveryDocument = supplyDocument;

                if (!supplyInvoiceToReturn.SupplyInvoiceDeliveryDocuments.Any(x => x.Id.Equals(document.Id)))
                    supplyInvoiceToReturn.SupplyInvoiceDeliveryDocuments.Add(document);
            }

            if (informationDeliveryProtocol == null || supplyInvoiceToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationDeliveryProtocol.Id)))
                return supplyInvoice;

            informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
            informationDeliveryProtocol.User = informationDeliveryProtocolUser;

            supplyInvoiceToReturn.InformationDeliveryProtocols.Add(informationDeliveryProtocol);

            return supplyInvoice;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [InvoiceDocument] AS [SupplyInvoiceDocument] " +
            "ON [SupplyInvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] " +
            "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
            "ON [SupplyOrderPaymentDeliveryProtocol].UserID = [SupplyOrderPaymentDeliveryProtocolUser].ID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] " +
            "ON [SupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [SupplyInformationDeliveryProtocolKey] " +
            "ON [SupplyInformationDeliveryProtocolKey].ID = [SupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "AND [SupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [SupplyInformationDeliveryProtocolUser] " +
            "ON [SupplyInformationDeliveryProtocolUser].ID = [SupplyInformationDeliveryProtocol].UserID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].ID = [SupplyInvoice].DeliveryProductProtocolID " +
            "LEFT JOIN [SupplyInvoiceDeliveryDocument] " +
            "ON [SupplyInvoiceDeliveryDocument].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [SupplyInvoiceDeliveryDocument].[Deleted] = 0 " +
            "LEFT JOIN [SupplyDeliveryDocument] " +
            "ON [SupplyDeliveryDocument].[ID] = [SupplyInvoiceDeliveryDocument].[SupplyDeliveryDocumentID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            types,
            invoiceMapper,
            props
        );

        if (supplyInvoiceToReturn == null)
            return null;

        supplyInvoiceToReturn.MergedSupplyInvoices =
            _connection.Query<SupplyInvoice>(
                "SELECT * " +
                "FROM [SupplyInvoice] " +
                "WHERE [SupplyInvoice].RootSupplyInvoiceId = @Id",
                new { supplyInvoiceToReturn.Id }).ToList();

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        bool isGrossCalculated = supplyInvoiceToReturn.SupplyOrder.IsGrossPricesCalculated;

        types = new[] {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        Func<object[], SupplyInvoice> orderItemsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSpecification productSpecification = (ProductSpecification)objects[5];
            User user = (User)objects[6];

            if (supplyInvoiceOrderItem == null) return supplyInvoice;

            if (!supplyInvoiceToReturn.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                if (productSpecification != null) {
                    productSpecification.AddedBy = user;

                    product.ProductSpecifications.Add(productSpecification);
                }

                product.MeasureUnit = measureUnit;

                product.Name =
                    isPlCulture
                        ? product.NameUA
                        : product.NameUA;

                if (supplyOrderItem != null) {
                    supplyOrderItem.Product = product;

                    supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    supplyOrderItem.NetWeight = Math.Round(supplyOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                    supplyOrderItem.GrossWeight = Math.Round(supplyOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);
                }

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                supplyInvoiceToReturn.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
            } else if (productSpecification != null) {
                SupplyInvoiceOrderItem fromList = supplyInvoiceToReturn.SupplyInvoiceOrderItems.First(i => i.Id.Equals(supplyInvoiceOrderItem.Id));

                if (fromList.SupplyOrderItem != null) {
                    if (!fromList.SupplyOrderItem.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id)))
                        fromList.SupplyOrderItem.Product.ProductSpecifications.Add(productSpecification);
                } else {
                    if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id))) fromList.Product.ProductSpecifications.Add(productSpecification);
                }
            }

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "WHERE [SupplyInvoice].NetUID = @NetId",
            types,
            orderItemsMapper,
            props
        );

        types = new[] {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackage),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductPlacement),
            typeof(ProductPlacement),
            typeof(ProductSpecification),
            typeof(User),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(ProductSpecification)
        };

        List<long> productIds = new();

        Func<object[], PackingList> packingListMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
            SupplyInvoiceOrderItem packageSupplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem packageSupplyOrderItem = (SupplyOrderItem)objects[9];
            Product packageProduct = (Product)objects[10];
            MeasureUnit packageProductMeasureUnit = (MeasureUnit)objects[11];
            ProductPlacement itemProductPlacement = (ProductPlacement)objects[12];
            ProductPlacement packageItemProductPlacement = (ProductPlacement)objects[13];
            ProductSpecification productSpecification = (ProductSpecification)objects[14];
            User user = (User)objects[15];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[16];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[17];
            ProductSpecification productSpecificationConsignmentItem = (ProductSpecification)objects[18];

            if (packingList == null) return null;

            if (!supplyInvoiceToReturn.PackingLists.Any(p => p.Id.Equals(packingList.Id)))
                supplyInvoiceToReturn.PackingLists.Add(packingList);
            else
                packingList = supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(packingList.Id));

            if (packingListPackageOrderItem != null) {
                if (!packingList.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    if (productIncomeItem != null && consignmentItem != null) {
                        consignmentItem.ProductSpecification = productSpecificationConsignmentItem;
                        productIncomeItem.ConsignmentItems.Add(consignmentItem);
                        packingListPackageOrderItem.ProductIncomeItem = productIncomeItem;
                    }

                    if (productSpecification != null) {
                        productSpecification.AddedBy = user;
                        packingList.TotalCustomValue += productSpecification.CustomsValue;
                        product.ProductSpecifications.Add(productSpecification);
                    }

                    product.Name =
                        isPlCulture
                            ? product.NameUA
                            : product.NameUA;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    if (!productIds.Any(p => p.Equals(product.Id))) productIds.Add(product.Id);

                    if (supplyOrderItem != null)
                        supplyOrderItem.UnitPrice = decimal.Round(supplyOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    supplyInvoiceOrderItem.ProductSpecification = productSpecification;

                    supplyInvoiceOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;

                    supplyInvoiceOrderItem.UnitPrice = decimal.Round(supplyInvoiceOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingListPackageOrderItem.TotalNetWeight =
                        packingListPackageOrderItem.NetWeight * packingListPackageOrderItem.Qty;
                    packingListPackageOrderItem.TotalGrossWeight =
                        packingListPackageOrderItem.GrossWeight * packingListPackageOrderItem.Qty;

                    packingListPackageOrderItem.UnitPrice = decimal.Round(packingListPackageOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    if (isGrossCalculated) {
                        packingListPackageOrderItem.TotalNetPrice =
                            decimal.Round(
                                decimal.Round(packingListPackageOrderItem.UnitPriceEur * packingListPackageOrderItem.ExchangeRateAmount, 2,
                                    MidpointRounding.AwayFromZero)
                                * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalGrossPrice =
                            packingListPackageOrderItem.GrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur
                                                                          * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        packingListPackageOrderItem.AccountingTotalGrossPrice =
                            packingListPackageOrderItem.AccountingGrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur
                                                                                    * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        packingListPackageOrderItem.VatAmount =
                            decimal.Round(
                                packingListPackageOrderItem.TotalNetPrice * packingListPackageOrderItem.VatPercent / 100,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalNetPrice = decimal.Round(
                            packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    } else {
                        packingListPackageOrderItem.TotalNetPrice =
                            packingListPackageOrderItem.AccountingTotalGrossPrice =
                                packingListPackageOrderItem.TotalGrossPrice =
                                    decimal.Round(
                                        packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    );

                        packingListPackageOrderItem.VatAmount =
                            decimal.Round(
                                packingListPackageOrderItem.TotalNetPrice * packingListPackageOrderItem.VatPercent / 100,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        packingListPackageOrderItem.TotalNetPrice = decimal.Round(
                            packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    }

                    packingListPackageOrderItem.TotalNetPriceEur =
                        packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                    packingListPackageOrderItem.TotalGrossPriceEur =
                        packingListPackageOrderItem.GrossUnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                    packingListPackageOrderItem.AccountingTotalGrossPriceEur =
                        packingListPackageOrderItem.AccountingGrossUnitPriceEur * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                    packingListPackageOrderItem.VatAmountEur =
                        decimal.Round(
                            packingListPackageOrderItem.TotalNetPriceEur * packingListPackageOrderItem.VatPercent / 100,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.TotalNetPriceWithVat =
                        decimal.Round(
                            packingListPackageOrderItem.TotalNetPrice + packingListPackageOrderItem.VatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingListPackageOrderItem.TotalNetPriceWithVatEur =
                        decimal.Round(
                            packingListPackageOrderItem.TotalNetPriceEur + packingListPackageOrderItem.VatAmountEur,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    packingList.TotalNetPriceEur =
                        decimal.Round(packingList.TotalNetPriceEur + packingListPackageOrderItem.TotalNetPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.TotalGrossPriceEur =
                        decimal.Round(packingList.TotalGrossPriceEur + packingListPackageOrderItem.TotalGrossPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.AccountingTotalGrossPriceEur =
                        decimal.Round(packingList.AccountingTotalGrossPriceEur + packingListPackageOrderItem.AccountingTotalGrossPriceEur, 2, MidpointRounding.AwayFromZero);

                    packingList.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;
                    packingList.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;

                    packingList.TotalNetPrice += Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.UnitPrice;
                    packingList.TotalGrossPrice += packingListPackageOrderItem.TotalGrossPrice;
                    packingList.AccountingTotalGrossPrice += packingListPackageOrderItem.AccountingTotalGrossPrice;

                    packingList.TotalNetPriceWithVat += packingListPackageOrderItem.TotalNetPriceWithVat;

                    packingList.TotalNetPriceWithVatEur += packingListPackageOrderItem.TotalNetPriceWithVatEur;

                    packingList.TotalVatAmount += packingListPackageOrderItem.VatAmount;

                    supplyInvoiceToReturn.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;

                    supplyInvoiceToReturn.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;

                    supplyInvoiceToReturn.TotalQuantity += packingListPackageOrderItem.Qty;

                    packingList.TotalQuantity += packingListPackageOrderItem.Qty;

                    supplyInvoiceToReturn.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;

                    supplyInvoiceToReturn.TotalGrossPrice += packingListPackageOrderItem.TotalGrossPrice;

                    supplyInvoiceToReturn.AccountingTotalGrossPrice += packingListPackageOrderItem.AccountingTotalGrossPrice;

                    supplyInvoiceToReturn.TotalNetPriceWithVat += packingListPackageOrderItem.TotalNetPriceWithVat;

                    supplyInvoiceToReturn.TotalVatAmount += packingListPackageOrderItem.VatAmount;

                    if (itemProductPlacement != null) packingListPackageOrderItem.ProductPlacements.Add(itemProductPlacement);

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                } else if (itemProductPlacement != null) {
                    packingList
                        .PackingListPackageOrderItems
                        .First(i => i.Id.Equals(packingListPackageOrderItem.Id))
                        .ProductPlacements
                        .Add(itemProductPlacement);

                    PackingListPackageOrderItem fromListItem = packingList.PackingListPackageOrderItems.First(i => i.Id.Equals(packingListPackageOrderItem.Id));

                    if (productIncomeItem != null && consignmentItem != null) {
                        consignmentItem.ProductSpecification = productSpecificationConsignmentItem;
                        productIncomeItem.ConsignmentItems.Add(consignmentItem);
                        fromListItem.ProductIncomeItem = productIncomeItem;
                    }

                    if (fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id)))
                        return packingList;

                    productSpecification.AddedBy = user;

                    fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Add(productSpecification);

                    fromListItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                    if (fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any()) {
                        ProductSpecification productSpecificationFromList = fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Last();

                        decimal dutyAndVatValue;

                        if (!fromListItem.ExchangeRateAmountUahToEur.Equals(0))
                            dutyAndVatValue =
                                fromListItem.ExchangeRateAmountUahToEur > 0
                                    ? (productSpecificationFromList.Duty + productSpecificationFromList.VATValue)
                                      / fromListItem.ExchangeRateAmountUahToEur
                                    : (productSpecificationFromList.Duty + productSpecificationFromList.VATValue) * fromListItem.ExchangeRateAmountUahToEur;
                        else
                            dutyAndVatValue = productSpecificationFromList.Duty + productSpecificationFromList.VATValue;

                        fromListItem.AccountingTotalGrossPrice =
                            Convert.ToDecimal(packingListPackageOrderItem.Qty) *
                            packingListPackageOrderItem.AccountingGrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur +
                            dutyAndVatValue * packingListPackageOrderItem.ExchangeRateAmountUahToEur;

                        fromListItem.TotalGrossPrice = Convert.ToDecimal(packingListPackageOrderItem.Qty) *
                                                       packingListPackageOrderItem.GrossUnitPriceEur * packingListPackageOrderItem.ExchangeRateAmountUahToEur +
                                                       fromListItem.AccountingTotalGrossPrice;

                        fromListItem.AccountingTotalGrossPriceEur =
                            Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.AccountingGrossUnitPriceEur +
                            dutyAndVatValue;

                        fromListItem.TotalGrossPriceEur = Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.GrossUnitPriceEur +
                                                          fromListItem.AccountingTotalGrossPriceEur;
                    }
                } else {
                    PackingListPackageOrderItem fromListItem = packingList.PackingListPackageOrderItems.First(i => i.Id.Equals(packingListPackageOrderItem.Id));

                    if (fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any()) {
                        ProductSpecification productSpecificationFromList = fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Last();

                        packingList.TotalCustomValue =
                            decimal.Round(packingList.TotalCustomValue - productSpecificationFromList.CustomsValue, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalVatAmount = decimal.Round(packingList.TotalVatAmount - productSpecificationFromList.VATValue, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalDuty = decimal.Round(packingList.TotalDuty - productSpecificationFromList.Duty, 2, MidpointRounding.AwayFromZero);
                    }

                    if (productSpecification != null) {
                        productSpecification.AddedBy = user;

                        if (!fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Any(x => x.Id.Equals(productSpecification.Id))) {
                            fromListItem.SupplyInvoiceOrderItem.Product.ProductSpecifications.Add(productSpecification);

                            packingList.TotalCustomValue = decimal.Round(packingList.TotalCustomValue + productSpecification.CustomsValue, 2, MidpointRounding.AwayFromZero);
                            packingList.TotalVatAmount = decimal.Round(packingList.TotalVatAmount + productSpecification.VATValue, 2, MidpointRounding.AwayFromZero);
                            packingList.TotalDuty = decimal.Round(packingList.TotalDuty + productSpecification.Duty, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }
            }

            if (package == null) return packingList;

            if (package.Type.Equals(PackingListPackageType.Box)) {
                if (packingList.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                    PackingListPackage packageFromList = packingList.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                    if (packageOrderItem == null) return packingList;

                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else if (packageItemProductPlacement != null) {
                        packageFromList
                            .PackingListPackageOrderItems
                            .First(p => p.Id.Equals(packageOrderItem.Id))
                            .ProductPlacements
                            .Add(packageItemProductPlacement);
                    }
                } else {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    packingList.PackingListBoxes.Add(package);
                }
            } else {
                if (packingList.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                    PackingListPackage packageFromList = packingList.PackingListPallets.First(p => p.Id.Equals(package.Id));

                    if (packageOrderItem == null) return packingList;

                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else if (packageItemProductPlacement != null) {
                        packageFromList
                            .PackingListPackageOrderItems
                            .First(p => p.Id.Equals(packageOrderItem.Id))
                            .ProductPlacements
                            .Add(packageItemProductPlacement);
                    }
                } else {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        if (!productIds.Any(p => p.Equals(packageProduct.Id))) productIds.Add(packageProduct.Id);

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageOrderItem.SupplyInvoiceOrderItem.ProductSpecification = productSpecification;

                        packageOrderItem.TotalNetWeight =
                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.TotalGrossWeight =
                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalNetPrice =
                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packageOrderItem.TotalGrossPrice =
                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packageOrderItem.AccountingTotalGrossPrice =
                            decimal.Round(packageOrderItem.AccountingTotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packageOrderItem.TotalNetWeight, 2);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 3, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);


                        supplyInvoiceToReturn.TotalNetWeight =
                            Math.Round(supplyInvoiceToReturn.TotalNetWeight + packageOrderItem.TotalNetWeight, 2);
                        supplyInvoiceToReturn.TotalGrossWeight =
                            Math.Round(supplyInvoiceToReturn.TotalGrossWeight + packageOrderItem.TotalGrossWeight, 2);

                        supplyInvoiceToReturn.TotalQuantity += packageOrderItem.Qty;

                        packingList.TotalQuantity += packageOrderItem.Qty;

                        supplyInvoiceToReturn.TotalNetPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.TotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        supplyInvoiceToReturn.AccountingTotalGrossPrice =
                            decimal.Round(supplyInvoiceToReturn.AccountingTotalGrossPrice + packageOrderItem.AccountingTotalGrossPrice, 3, MidpointRounding.AwayFromZero);

                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    packingList.PackingListPallets.Add(package);
                }
            }

            return packingList;
        };

        var packingListProps = new { supplyInvoiceToReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            ";WITH [SPECIFICATION_CTE] AS ( " +
            "SELECT " +
            "MAX([ProductSpecification].[ID]) AS [ID] " +
            ", [ProductSpecification].[ProductID] " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].[SupplyInvoiceId] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderProductSpecification].[ProductSpecificationId] " +
            "WHERE [SupplyInvoice].[ID] = @Id " +
            "GROUP BY [ProductSpecification].[ProductID] " +
            ") " +
            "SELECT " +
            "[PackingList].* " +
            ", [PackingListPackageOrderItem].* " +
            ", [SupplyInvoiceOrderItem].* " +
            ", [SupplyOrderItem].* " +
            ", [Product].* " +
            ", [MeasureUnit].* " +
            ", [Pallet].* " +
            ", [PalletPackageOrderItem].* " +
            ", [PalletInvoiceOrderItem].* " +
            ", [PalletOrderItem].* " +
            ", [PalletOrderItemProduct].* " +
            ", [PalletOrderItemProductMeasureUnit].* " +
            ", [ItemProductPlacement].* " +
            ", [PackageProductPlacement].* " +
            ", [ProductSpecification].* " +
            ", [User].* " +
            ", [ProductIncomeItem].* " +
            ", [ConsignmentItem].* " +
            ", [ProductSpecificationConsignmentItem].* " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackage] AS [Pallet] " +
            "ON [PackingList].ID = [Pallet].PackingListID " +
            "AND [Pallet].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [PalletPackageOrderItem] " +
            "ON [Pallet].ID = [PalletPackageOrderItem].PackingListPackageID " +
            "AND [PalletPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] AS [PalletInvoiceOrderItem] " +
            "ON [PalletPackageOrderItem].SupplyInvoiceOrderItemID = [PalletInvoiceOrderItem].ID " +
            "LEFT JOIN [SupplyOrderItem] AS [PalletOrderItem] " +
            "ON [PalletInvoiceOrderItem].SupplyOrderItemID = [PalletOrderItem].ID " +
            "LEFT JOIN [Product] AS [PalletOrderItemProduct] " +
            "ON [PalletOrderItemProduct].ID = [PalletOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [PalletOrderItemProductMeasureUnit] " +
            "ON [PalletOrderItemProductMeasureUnit].ID = [PalletOrderItemProduct].MeasureUnitID " +
            "AND [PalletOrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductPlacement] AS [ItemProductPlacement] " +
            "ON [ItemProductPlacement].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductPlacement] AS [PackageProductPlacement] " +
            "ON [PackageProductPlacement].PackingListPackageOrderItemID = [PalletPackageOrderItem].ID " +
            "LEFT JOIN [SPECIFICATION_CTE] " +
            "ON [SPECIFICATION_CTE].[ProductID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [SPECIFICATION_CTE].[ID] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
            "AND [ProductIncomeItem].[Deleted] = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "AND [ConsignmentItem].[Deleted] = 0 " +
            "LEFT JOIN [ProductSpecification] AS [ProductSpecificationConsignmentItem] " +
            "ON [ProductSpecificationConsignmentItem].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [PackingList].SupplyInvoiceID = @Id " +
            "AND [PackingList].Deleted = 0 " +
            "ORDER BY [ProductSpecification].[ID] ",
            types,
            packingListMapper,
            packingListProps,
            commandTimeout: 3600
        );

        _connection.Query<DynamicProductPlacementColumn, DynamicProductPlacementRow, PackingListPackageOrderItem, DynamicProductPlacement, DynamicProductPlacementColumn>(
            "SELECT * " +
            "FROM [DynamicProductPlacementColumn] " +
            "LEFT JOIN [DynamicProductPlacementRow] " +
            "ON [DynamicProductPlacementRow].DynamicProductPlacementColumnID = [DynamicProductPlacementColumn].ID " +
            "AND [DynamicProductPlacementRow].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [DynamicProductPlacementRow].PackingListPackageOrderItemID " +
            "LEFT JOIN [DynamicProductPlacement] " +
            "ON [DynamicProductPlacement].DynamicProductPlacementRowID = [DynamicProductPlacementRow].ID " +
            "AND [DynamicProductPlacement].Deleted = 0 " +
            "WHERE [DynamicProductPlacementColumn].Deleted = 0 " +
            "AND [DynamicProductPlacementColumn].PackingListID IN @Ids",
            (column, row, item, placement) => {
                PackingList packListFromList = supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(column.PackingListId));

                if (!packListFromList.DynamicProductPlacementColumns.Any(c => c.Id.Equals(column.Id))) {
                    if (row != null) {
                        if (placement != null) row.DynamicProductPlacements.Add(placement);

                        row.PackingListPackageOrderItem = item;

                        column.DynamicProductPlacementRows.Add(row);
                    }

                    packListFromList.DynamicProductPlacementColumns.Add(column);
                } else {
                    DynamicProductPlacementColumn columnFromList = packListFromList.DynamicProductPlacementColumns.First(c => c.Id.Equals(column.Id));

                    if (row == null) return column;

                    if (!columnFromList.DynamicProductPlacementRows.Any(r => r.Id.Equals(row.Id))) {
                        if (placement != null) row.DynamicProductPlacements.Add(placement);

                        row.PackingListPackageOrderItem = item;

                        columnFromList.DynamicProductPlacementRows.Add(row);
                    } else {
                        if (placement != null) columnFromList.DynamicProductPlacementRows.First(r => r.Id.Equals(row.Id)).DynamicProductPlacements.Add(placement);
                    }
                }

                return column;
            },
            new {
                Ids = supplyInvoiceToReturn.PackingLists.Select(p => p.Id)
            }
        );

        foreach (PackingList packingList in supplyInvoiceToReturn.PackingLists) {
            packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight, 3);
            packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight, 3);

            packingList.PackingListPackageOrderItems =
                packingList
                    .PackingListPackageOrderItems
                    .OrderBy(x => x.SupplyInvoiceOrderItem.Product.VendorCode)
                    .ToList();
        }

        supplyInvoiceToReturn.TotalNetWeight = Math.Round(supplyInvoiceToReturn.TotalNetWeight, 3);
        supplyInvoiceToReturn.TotalGrossWeight = Math.Round(supplyInvoiceToReturn.TotalGrossWeight, 3);

        if (productIds.Any()) {
            if (productIds.Count > 2000)
                for (int j = 0; j < productIds.Count % 2000; j++)
                    _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                        ";WITH [Search_CTE] " +
                        "AS ( " +
                        "SELECT MAX([ID]) AS [ID] " +
                        "FROM [ProductPlacement] " +
                        "WHERE [ProductPlacement].ProductID IN @Ids " +
                        "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                        "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                        "GROUP BY [ProductPlacement].CellNumber " +
                        ", [ProductPlacement].RowNumber " +
                        ", [ProductPlacement].StorageNumber " +
                        ", [ProductPlacement].StorageID " +
                        ") " +
                        "SELECT * " +
                        "FROM [ProductPlacement] " +
                        "LEFT JOIN [Storage] " +
                        "ON [Storage].ID = [ProductPlacement].StorageID " +
                        "WHERE [ProductPlacement].ID IN ( " +
                        "SELECT [ID] " +
                        "FROM [Search_CTE] " +
                        ")",
                        (placement, storage) => {
                            placement.Storage = storage;

                            foreach (PackingList packingList in supplyInvoiceToReturn
                                         .PackingLists
                                         .Where(p =>
                                             p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))
                                             ||
                                             p.PackingListBoxes.Any(b =>
                                                 b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                             ||
                                             p.PackingListPallets.Any(b =>
                                                 b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                         )) {
                                foreach (PackingListPackageOrderItem item in packingList
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                                foreach (PackingListPackage box in packingList
                                             .PackingListBoxes
                                             .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                                foreach (PackingListPackageOrderItem item in box
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                                foreach (PackingListPackage pallet in packingList
                                             .PackingListPallets
                                             .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                                foreach (PackingListPackageOrderItem item in pallet
                                             .PackingListPackageOrderItems
                                             .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                    item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);
                            }

                            return placement;
                        },
                        new { Ids = productIds.Skip(j * 2000).Take(2000) }
                    );
            else
                _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT MAX([ID]) AS [ID] " +
                    "FROM [ProductPlacement] " +
                    "WHERE [ProductPlacement].ProductID IN @Ids " +
                    "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                    "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                    "GROUP BY [ProductPlacement].CellNumber " +
                    ", [ProductPlacement].RowNumber " +
                    ", [ProductPlacement].StorageNumber " +
                    ", [ProductPlacement].StorageID " +
                    ") " +
                    "SELECT * " +
                    "FROM [ProductPlacement] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].ID = [ProductPlacement].StorageID " +
                    "WHERE [ProductPlacement].ID IN ( " +
                    "SELECT [ID] " +
                    "FROM [Search_CTE] " +
                    ")",
                    (placement, storage) => {
                        placement.Storage = storage;

                        foreach (PackingList packingList in supplyInvoiceToReturn
                                     .PackingLists
                                     .Where(p =>
                                         p.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))
                                         ||
                                         p.PackingListBoxes.Any(b =>
                                             b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                         ||
                                         p.PackingListPallets.Any(b =>
                                             b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                     )) {
                            foreach (PackingListPackageOrderItem item in packingList
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                            foreach (PackingListPackage box in packingList
                                         .PackingListBoxes
                                         .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                            foreach (PackingListPackageOrderItem item in box
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                            foreach (PackingListPackage pallet in packingList
                                         .PackingListPallets
                                         .Where(b => b.PackingListPackageOrderItems.Any(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId))))
                            foreach (PackingListPackageOrderItem item in pallet
                                         .PackingListPackageOrderItems
                                         .Where(i => i.SupplyInvoiceOrderItem.ProductId.Equals(placement.ProductId)))
                                item.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);
                        }

                        return placement;
                    },
                    new { Ids = productIds }
                );
        }

        _connection.Query<PackingList, InvoiceDocument, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "WHERE [PackingList].SupplyInvoiceID = @Id " +
            "AND [PackingList].Deleted = 0",
            (packingList, document) => {
                if (document != null) supplyInvoiceToReturn.PackingLists.First(p => p.Id.Equals(packingList.Id)).InvoiceDocuments.Add(document);

                return packingList;
            },
            packingListProps
        );

        return supplyInvoiceToReturn;
    }
}