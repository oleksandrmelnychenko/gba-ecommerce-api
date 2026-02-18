using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyOrderItemRepository : ISupplyOrderItemRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<SupplyOrderItem> GetAllBySupplyOrderNetId(Guid netId) {
        List<SupplyOrderItem> toReturn = new();

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        _connection.Query<SupplyOrderItem, Product, MeasureUnit, ProductSpecification, User, SupplyOrderItem>(
            "SELECT * FROM SupplyOrderItem " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = SupplyOrderItem.ProductID " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "WHERE SupplyOrderItem.SupplyOrderID = (" +
            "SELECT ID FROM SupplyOrder " +
            "WHERE NetUID = @NetId " +
            ") " +
            "ORDER BY ProductSpecification.Created",
            (orderItem, product, measureUnit, specification, user) => {
                if (product != null) {
                    if (toReturn.Any(o => o.Id.Equals(orderItem.Id))) {
                        SupplyOrderItem fromList = toReturn.First(o => o.Id.Equals(orderItem.Id));

                        if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                            specification.AddedBy = user;

                            fromList.Product.ProductSpecifications.Add(specification);
                        }
                    } else {
                        if (measureUnit != null) product.MeasureUnit = measureUnit;

                        if (specification != null) {
                            specification.AddedBy = user;

                            product.ProductSpecifications.Add(specification);
                        }

                        product.Name =
                            isPlCulture
                                ? product.NameUA
                                : product.NameUA;

                        orderItem.Product = product;

                        toReturn.Add(orderItem);
                    }
                }

                return orderItem;
            },
            new { NetId = netId }
        );

        return toReturn;
    }

    public List<SupplyOrderItem> GetAll() {
        List<SupplyOrderItem> toReturn = new();

        _connection.Query<SupplyOrderItem, Product, MeasureUnit, ProductSpecification, User, SupplyOrderItem>(
            "SELECT * FROM SupplyOrderItem " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = SupplyOrderItem.ProductID " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "WHERE SupplyOrderItem.Deleted = 0 " +
            "ORDER BY ProductSpecification.Created",
            (orderItem, product, measureUnit, specification, user) => {
                if (product != null) {
                    if (toReturn.Any(o => o.Id.Equals(orderItem.Id))) {
                        SupplyOrderItem fromList = toReturn.First(o => o.Id.Equals(orderItem.Id));

                        if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                            specification.AddedBy = user;

                            fromList.Product.ProductSpecifications.Add(specification);
                        }
                    } else {
                        if (measureUnit != null) product.MeasureUnit = measureUnit;

                        if (specification != null) {
                            specification.AddedBy = user;

                            product.ProductSpecifications.Add(specification);
                        }

                        orderItem.Product = product;

                        toReturn.Add(orderItem);
                    }
                }

                return orderItem;
            });

        return toReturn;
    }

    public SupplyOrderItem GetByNetId(Guid netId) {
        return _connection.Query<SupplyOrderItem, Product, MeasureUnit, SupplyOrderItem>(
                "SELECT * " +
                "FROM [SupplyOrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyOrderItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [SupplyOrderItem].NetUID = @NetId",
                (item, product, measureUnit) => {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    return item;
                },
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }


    public SupplyOrderItem GetByOrderAndProductIdWithInvoiceItemsIfExistsAndQty(long orderId, long productId, double qty) {
        SupplyOrderItem toReturn = null;

        _connection.Query<SupplyOrderItem, SupplyInvoiceOrderItem, SupplyInvoice, SupplyOrderItem>(
            "SELECT [SupplyOrderItem].*, [SupplyInvoiceOrderItem].*, [SupplyInvoice].* " +
            "FROM [SupplyOrderItem] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "WHERE [SupplyOrderItem].SupplyOrderID = @OrderId " +
            "AND [SupplyOrderItem].ProductID = @ProductId " +
            "AND [SupplyOrderItem].Qty = @Qty " +
            "AND [SupplyOrderItem].Deleted = 0",
            (orderItem, invoiceOrderItem, invoice) => {
                if (toReturn == null)
                    toReturn = orderItem;

                if (invoiceOrderItem == null || invoice.Deleted) return orderItem;

                toReturn.SupplyInvoiceOrderItems.Add(invoiceOrderItem);

                return orderItem;
            },
            new { OrderId = orderId, ProductId = productId, Qty = qty }
        );

        return toReturn;
    }

    public SupplyOrderItem GetByOrderAndProductIdWithInvoiceItemsIfExists(long orderId, long productId) {
        SupplyOrderItem toReturn = null;

        _connection.Query<SupplyOrderItem, SupplyInvoiceOrderItem, SupplyInvoice, SupplyOrderItem>(
            "SELECT [SupplyOrderItem].*, [SupplyInvoiceOrderItem].*, [SupplyInvoice].* " +
            "FROM [SupplyOrderItem] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "WHERE [SupplyOrderItem].SupplyOrderID = @OrderId " +
            "AND [SupplyOrderItem].ProductID = @ProductId " +
            "AND [SupplyOrderItem].Deleted = 0",
            (orderItem, invoiceOrderItem, invoice) => {
                if (toReturn == null)
                    toReturn = orderItem;

                if (invoiceOrderItem == null || invoice.Deleted) return orderItem;

                toReturn.SupplyInvoiceOrderItems.Add(invoiceOrderItem);

                return orderItem;
            },
            new { OrderId = orderId, ProductId = productId }
        );

        return toReturn;
    }

    public SupplyOrderItem GetByOrderAndProductIdAndQtyWithInvoiceItemsIfExists(long orderId, long productId, double qty, decimal unitPrice) {
        SupplyOrderItem toReturn = null;

        _connection.Query<SupplyOrderItem, SupplyInvoiceOrderItem, SupplyInvoice, SupplyOrderItem>(
            "SELECT [SupplyOrderItem].*, [SupplyInvoiceOrderItem].*, [SupplyInvoice].* " +
            "FROM [SupplyOrderItem] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "WHERE [SupplyOrderItem].SupplyOrderID = @OrderId " +
            "AND [SupplyOrderItem].ProductID = @ProductId " +
            "AND [SupplyOrderItem].Qty = @Qty " +
            "AND [SupplyOrderItem].UnitPrice = @UnitPrice " +
            "AND [SupplyOrderItem].Deleted = 0",
            (orderItem, invoiceOrderItem, invoice) => {
                if (toReturn == null)
                    toReturn = orderItem;

                if (invoiceOrderItem == null || invoice.Deleted) return orderItem;

                toReturn.SupplyInvoiceOrderItems.Add(invoiceOrderItem);

                return orderItem;
            },
            new { OrderId = orderId, ProductId = productId, Qty = qty, UnitPrice = unitPrice }
        );

        return toReturn;
    }

    public void UpdateQty(SupplyOrderItem supplyOrderItem) {
        _connection.Execute(
            "UPDATE [SupplyOrderItem] " +
            "SET Qty = @Qty, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            supplyOrderItem
        );
    }

    public void Update(SupplyOrderItem supplyOrderItem) {
        _connection.Execute(
            "UPDATE [SupplyOrderItem] " +
            "SET StockNo = @StockNo, ItemNo = @ItemNo, Qty = @Qty, UnitPrice = @UnitPrice, TotalAmount = @TotalAmount, GrossWeight = @GrossWeight, NetWeight = @NetWeight, " +
            "Description = @Description, SupplyOrderId = @SupplyOrderId, ProductId = @ProductId, IsPacked = @IsPacked, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            supplyOrderItem
        );
    }

    public void Update(IEnumerable<SupplyOrderItem> supplyOrderItems) {
        _connection.Execute(
            "UPDATE [SupplyOrderItem] " +
            "SET StockNo = @StockNo, ItemNo = @ItemNo, Qty = @Qty, UnitPrice = @UnitPrice, TotalAmount = @TotalAmount, GrossWeight = @GrossWeight, NetWeight = @NetWeight, " +
            "Description = @Description, SupplyOrderId = @SupplyOrderId, ProductId = @ProductId, IsPacked = @IsPacked, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            supplyOrderItems
        );
    }

    public void Add(IEnumerable<SupplyOrderItem> supplyOrderItems) {
        _connection.Execute(
            "INSERT INTO SupplyOrderItem ([Description], ItemNo, ProductID, Qty, StockNo, SupplyOrderID, TotalAmount, UnitPrice, GrossWeight, NetWeight, " +
            "IsPacked, Updated) " +
            "VALUES(@Description, @ItemNo, @ProductID, @Qty, @StockNo, @SupplyOrderID, @TotalAmount, @UnitPrice, @GrossWeight, @NetWeight, @IsPacked, getutcdate())",
            supplyOrderItems
        );
    }

    public long Add(SupplyOrderItem supplyOrderItem) {
        return _connection.Query<long>(
            "INSERT INTO SupplyOrderItem ([Description], ItemNo, ProductID, Qty, StockNo, SupplyOrderID, TotalAmount, UnitPrice, GrossWeight, NetWeight, " +
            "IsPacked, Updated) " +
            "VALUES(@Description, @ItemNo, @ProductID, @Qty, @StockNo, @SupplyOrderID, @TotalAmount, @UnitPrice, @GrossWeight, @NetWeight, @IsPacked, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrderItem
        ).FirstOrDefault();
    }
}