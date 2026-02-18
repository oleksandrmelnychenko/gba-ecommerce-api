using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Carriers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class TaxFreeRepository : ITaxFreeRepository {
    private readonly IDbConnection _connection;

    private readonly IDbConnection _exchangeRateConnection;

    public TaxFreeRepository(IDbConnection connection, IDbConnection exchangeRateConnection) {
        _connection = connection;

        _exchangeRateConnection = exchangeRateConnection;
    }

    public long Add(TaxFree taxFree) {
        return _connection.Query<long>(
                "INSERT INTO [TaxFree] " +
                "(Number, CustomCode, Comment, AmountPayedStatham, AmountInPLN, VatAmountInPLN, AmountInEur, Weight, TaxFreeStatus, DateOfPrint, DateOfIssue, " +
                "DateOfStathamPayment, DateOfTabulation, StathamId, StathamCarId, TaxFreePackListId, ResponsibleId, MarginAmount, VatPercent, FormedDate, SelectedDate, " +
                "ReturnedDate, ClosedDate, CanceledDate, StathamPassportId, Updated) " +
                "VALUES " +
                "(@Number, @CustomCode, @Comment, @AmountPayedStatham, @AmountInPLN, @VatAmountInPLN, @AmountInEur, @Weight, @TaxFreeStatus, @DateOfPrint, @DateOfIssue, " +
                "@DateOfStathamPayment, @DateOfTabulation, @StathamId, @StathamCarId, @TaxFreePackListId, @ResponsibleId, @MarginAmount, @VatPercent, @FormedDate, @SelectedDate, " +
                "@ReturnedDate, @ClosedDate, @CanceledDate, @StathamPassportId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                taxFree
            )
            .Single();
    }

    public void Update(TaxFree taxFree) {
        _connection.Execute(
            "UPDATE [TaxFree] " +
            "SET CustomCode = @CustomCode, Comment = @Comment, AmountPayedStatham = @AmountPayedStatham, AmountInPLN = @AmountInPLN, VatAmountInPLN = @VatAmountInPLN, " +
            "AmountInEur = @AmountInEur, Weight = @Weight, TaxFreeStatus = @TaxFreeStatus, DateOfPrint = @DateOfPrint, DateOfIssue = @DateOfIssue, " +
            "DateOfStathamPayment = @DateOfStathamPayment, DateOfTabulation = @DateOfTabulation, StathamId = @StathamId, StathamCarId = @StathamCarId, " +
            "ResponsibleId = @ResponsibleId, MarginAmount = @MarginAmount, VatPercent = @VatPercent,  FormedDate = @FormedDate, SelectedDate = @SelectedDate, " +
            "ReturnedDate = @ReturnedDate, ClosedDate = @ClosedDate, CanceledDate = @CanceledDate, StathamPassportId = @StathamPassportId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            taxFree
        );
    }

    public void Remove(TaxFree taxFree) {
        _connection.Execute(
            "UPDATE [TaxFree] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            taxFree
        );
    }

    public TaxFree GetLastRecord() {
        return _connection.Query<TaxFree>(
                "SELECT TOP(1) * " +
                "FROM [TaxFree] " +
                "WHERE [TaxFree].Deleted = 0 " +
                "ORDER BY [TaxFree].ID DESC"
            )
            .SingleOrDefault();
    }

    public TaxFree GetById(long id) {
        TaxFree toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [TaxFree].StathamCarID " +
            "LEFT JOIN [User] AS [TaxFreeResponsible] " +
            "ON [TaxFreeResponsible].ID = [TaxFree].ResponsibleID " +
            "LEFT JOIN [TaxFreeDocument] " +
            "ON [TaxFreeDocument].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeDocument].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [User] AS [TaxFreePackListResponsible] " +
            "ON [TaxFreePackListResponsible].ID = [TaxFreePackList].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] AS [OrderItemProduct] " +
            "ON [OrderItem].ProductID = [OrderItemProduct].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [OrderItemProductMeasureUnit] " +
            "ON [OrderItemProductMeasureUnit].ID = [OrderItemProduct].MeasureUnitID " +
            "AND [OrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].ID = @Id";

        Type[] types = {
            typeof(TaxFree),
            typeof(Statham),
            typeof(StathamCar),
            typeof(User),
            typeof(TaxFreeDocument),
            typeof(TaxFreeItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(TaxFreePackList),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraine),
            typeof(TaxFreePackListOrderItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(StathamPassport)
        };

        Func<object[], TaxFree> mapper = objects => {
            TaxFree taxFree = (TaxFree)objects[0];
            Statham statham = (Statham)objects[1];
            StathamCar stathamCar = (StathamCar)objects[2];
            User taxFreeResponsible = (User)objects[3];
            TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            User createdBy = (User)objects[9];
            User updatedBy = (User)objects[10];
            User responsible = (User)objects[11];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[12];
            User packListUser = (User)objects[13];
            Organization organization = (Organization)objects[14];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[15];
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            Product orderItemProduct = (Product)objects[18];
            MeasureUnit orderItemProductMeasureUnit = (MeasureUnit)objects[19];
            StathamPassport stathamPassport = (StathamPassport)objects[20];

            if (toReturn == null) {
                if (taxFreeItem != null) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFreePackList.Responsible = packListUser;
                taxFreePackList.Organization = organization;
                taxFreePackList.SupplyOrderUkraine = supplyOrderUkraine;

                taxFree.Statham = statham;
                taxFree.StathamCar = stathamCar;
                taxFree.StathamPassport = stathamPassport;
                taxFree.Responsible = taxFreeResponsible;
                taxFree.TaxFreePackList = taxFreePackList;

                toReturn = taxFree;
            } else {
                if (taxFreeItem != null && !toReturn.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(toReturn.DateOfPrint?.AddDays(-1) ?? toReturn.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.UnitPriceWithVat =
                            decimal.Round(
                                toReturn.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.TotalWithVat =
                            decimal.Round(
                                toReturn.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        toReturn.VatAmountPl =
                            decimal.Round(toReturn.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        toReturn.TotalWithVatPl =
                            decimal.Round(toReturn.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.UnitPriceWithVat =
                            decimal.Round(
                                toReturn.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.TotalWithVat =
                            decimal.Round(
                                toReturn.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        toReturn.VatAmountPl =
                            decimal.Round(toReturn.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        toReturn.TotalWithVatPl =
                            decimal.Round(toReturn.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    toReturn.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null && !toReturn.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) toReturn.TaxFreeDocuments.Add(taxFreeDocument);
            }

            return taxFree;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Id = id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return toReturn;
    }

    public TaxFree GetByNetId(Guid netId) {
        TaxFree toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [TaxFree].StathamCarID " +
            "LEFT JOIN [User] AS [TaxFreeResponsible] " +
            "ON [TaxFreeResponsible].ID = [TaxFree].ResponsibleID " +
            "LEFT JOIN [TaxFreeDocument] " +
            "ON [TaxFreeDocument].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeDocument].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [User] AS [TaxFreePackListResponsible] " +
            "ON [TaxFreePackListResponsible].ID = [TaxFreePackList].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] AS [OrderItemProduct] " +
            "ON [OrderItem].ProductID = [OrderItemProduct].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [OrderItemProductMeasureUnit] " +
            "ON [OrderItemProductMeasureUnit].ID = [OrderItemProduct].MeasureUnitID " +
            "AND [OrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].NetUID = @NetId";

        Type[] types = {
            typeof(TaxFree),
            typeof(Statham),
            typeof(StathamCar),
            typeof(User),
            typeof(TaxFreeDocument),
            typeof(TaxFreeItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(TaxFreePackList),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraine),
            typeof(TaxFreePackListOrderItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(StathamPassport)
        };

        Func<object[], TaxFree> mapper = objects => {
            TaxFree taxFree = (TaxFree)objects[0];
            Statham statham = (Statham)objects[1];
            StathamCar stathamCar = (StathamCar)objects[2];
            User taxFreeResponsible = (User)objects[3];
            TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            User createdBy = (User)objects[9];
            User updatedBy = (User)objects[10];
            User responsible = (User)objects[11];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[12];
            User packListUser = (User)objects[13];
            Organization organization = (Organization)objects[14];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[15];
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            Product orderItemProduct = (Product)objects[18];
            MeasureUnit orderItemProductMeasureUnit = (MeasureUnit)objects[19];
            StathamPassport stathamPassport = (StathamPassport)objects[20];

            if (toReturn == null) {
                if (taxFreeItem != null) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFreePackList.Responsible = packListUser;
                taxFreePackList.Organization = organization;
                taxFreePackList.SupplyOrderUkraine = supplyOrderUkraine;

                taxFree.Statham = statham;
                taxFree.StathamCar = stathamCar;
                taxFree.StathamPassport = stathamPassport;
                taxFree.Responsible = taxFreeResponsible;
                taxFree.TaxFreePackList = taxFreePackList;

                toReturn = taxFree;
            } else {
                if (taxFreeItem != null && !toReturn.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(toReturn.DateOfPrint?.AddDays(-1) ?? toReturn.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.UnitPriceWithVat =
                            decimal.Round(
                                toReturn.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.TotalWithVat =
                            decimal.Round(
                                toReturn.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        toReturn.VatAmountPl =
                            decimal.Round(toReturn.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        toReturn.TotalWithVatPl =
                            decimal.Round(toReturn.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.UnitPriceWithVat =
                            decimal.Round(
                                toReturn.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.TotalWithVat =
                            decimal.Round(
                                toReturn.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        toReturn.VatAmountPl =
                            decimal.Round(toReturn.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        toReturn.TotalWithVatPl =
                            decimal.Round(toReturn.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    toReturn.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null && !toReturn.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) toReturn.TaxFreeDocuments.Add(taxFreeDocument);
            }

            return taxFree;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                NetId = netId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return toReturn;
    }

    public List<TaxFree> GetByNetIds(IEnumerable<Guid> netIds) {
        List<TaxFree> taxFrees = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [TaxFree].StathamCarID " +
            "LEFT JOIN [User] AS [TaxFreeResponsible] " +
            "ON [TaxFreeResponsible].ID = [TaxFree].ResponsibleID " +
            "LEFT JOIN [TaxFreeDocument] " +
            "ON [TaxFreeDocument].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeDocument].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [User] AS [TaxFreePackListResponsible] " +
            "ON [TaxFreePackListResponsible].ID = [TaxFreePackList].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] AS [OrderItemProduct] " +
            "ON [OrderItem].ProductID = [OrderItemProduct].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [OrderItemProductMeasureUnit] " +
            "ON [OrderItemProductMeasureUnit].ID = [OrderItemProduct].MeasureUnitID " +
            "AND [OrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].NetUID IN @NetIds";

        Type[] types = {
            typeof(TaxFree),
            typeof(Statham),
            typeof(StathamCar),
            typeof(User),
            typeof(TaxFreeDocument),
            typeof(TaxFreeItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(TaxFreePackList),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraine),
            typeof(TaxFreePackListOrderItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(StathamPassport)
        };

        Func<object[], TaxFree> mapper = objects => {
            TaxFree taxFree = (TaxFree)objects[0];
            Statham statham = (Statham)objects[1];
            StathamCar stathamCar = (StathamCar)objects[2];
            User taxFreeResponsible = (User)objects[3];
            TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            User createdBy = (User)objects[9];
            User updatedBy = (User)objects[10];
            User responsible = (User)objects[11];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[12];
            User packListUser = (User)objects[13];
            Organization organization = (Organization)objects[14];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[15];
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            Product orderItemProduct = (Product)objects[18];
            MeasureUnit orderItemProductMeasureUnit = (MeasureUnit)objects[19];
            StathamPassport stathamPassport = (StathamPassport)objects[20];

            if (!taxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                if (taxFreeItem != null) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFreePackList.Responsible = packListUser;
                taxFreePackList.Organization = organization;
                taxFreePackList.SupplyOrderUkraine = supplyOrderUkraine;

                taxFree.Statham = statham;
                taxFree.StathamCar = stathamCar;
                taxFree.StathamPassport = stathamPassport;
                taxFree.Responsible = taxFreeResponsible;
                taxFree.TaxFreePackList = taxFreePackList;

                taxFrees.Add(taxFree);
            } else {
                TaxFree fromList = taxFrees.First(t => t.Id.Equals(taxFree.Id));

                if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.UnitPriceWithVat =
                            decimal.Round(
                                fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.TotalWithVat =
                            decimal.Round(
                                fromList.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl =
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.UnitPriceWithVat =
                            decimal.Round(
                                fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.TotalWithVat =
                            decimal.Round(
                                fromList.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl =
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    fromList.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null && !fromList.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) fromList.TaxFreeDocuments.Add(taxFreeDocument);
            }

            return taxFree;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                NetIds = netIds,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return taxFrees;
    }

    public TaxFree GetByNetIdWithPackList(Guid netId) {
        return _connection.Query<TaxFree, TaxFreePackList, TaxFree>(
                "SELECT * " +
                "FROM [TaxFree] " +
                "LEFT JOIN [TaxFreePackList] " +
                "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
                "WHERE [TaxFree].NetUID = @NetId",
                (taxFree, packList) => {
                    taxFree.TaxFreePackList = packList;

                    return taxFree;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public List<TaxFree> GetByNetIdsWithPackList(IEnumerable<Guid> netIds) {
        return _connection.Query<TaxFree, TaxFreePackList, TaxFree>(
                "SELECT * " +
                "FROM [TaxFree] " +
                "LEFT JOIN [TaxFreePackList] " +
                "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
                "WHERE [TaxFree].NetUID IN @NetIds",
                (taxFree, packList) => {
                    taxFree.TaxFreePackList = packList;

                    return taxFree;
                },
                new { NetIds = netIds }
            )
            .ToList();
    }

    public TaxFree GetByNetIdFromSaleForPrinting(Guid netId) {
        TaxFree toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [TaxFree].StathamCarID " +
            "LEFT JOIN [User] AS [TaxFreeResponsible] " +
            "ON [TaxFreeResponsible].ID = [TaxFree].ResponsibleID " +
            "LEFT JOIN [TaxFreeDocument] " +
            "ON [TaxFreeDocument].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeDocument].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [User] AS [TaxFreePackListResponsible] " +
            "ON [TaxFreePackListResponsible].ID = [TaxFreePackList].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "LEFT JOIN [Order] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "WHERE [TaxFree].NetUID = @NetId";

        Type[] types = {
            typeof(TaxFree),
            typeof(Statham),
            typeof(StathamCar),
            typeof(User),
            typeof(TaxFreeDocument),
            typeof(TaxFreeItem),
            typeof(TaxFreePackListOrderItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(TaxFreePackList),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraine),
            typeof(StathamPassport),
            typeof(Order),
            typeof(Sale),
            typeof(SaleInvoiceDocument)
        };

        Func<object[], TaxFree> mapper = objects => {
            TaxFree taxFree = (TaxFree)objects[0];
            Statham statham = (Statham)objects[1];
            StathamCar stathamCar = (StathamCar)objects[2];
            User taxFreeResponsible = (User)objects[3];
            TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[10];
            User packListUser = (User)objects[11];
            Organization organization = (Organization)objects[12];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[13];
            StathamPassport stathamPassport = (StathamPassport)objects[14];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[17];

            if (toReturn == null) {
                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFreePackList.Responsible = packListUser;
                taxFreePackList.Organization = organization;
                taxFreePackList.SupplyOrderUkraine = supplyOrderUkraine;

                taxFree.Statham = statham;
                taxFree.StathamCar = stathamCar;
                taxFree.StathamPassport = stathamPassport;
                taxFree.Responsible = taxFreeResponsible;
                taxFree.TaxFreePackList = taxFreePackList;

                toReturn = taxFree;
            }

            if (taxFreeItem == null || toReturn.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id)))
                return taxFree;

            taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

            toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

            decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                ? 1m
                : orderItem.ExchangeRateAmount;

            orderItem.TotalAmountLocal =
                decimal.Round(
                    orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                    4,
                    MidpointRounding.AwayFromZero
                );

            orderItem.TotalAmount =
                decimal.Round(orderItem.PricePerItemWithoutVat * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

            taxFreeItem.UnitPriceWithVat = orderItem.PricePerItem;

            toReturn.UnitPriceWithVat += taxFreeItem.UnitPriceWithVat;

            taxFreeItem.TotalWithVat =
                decimal.Round(
                    orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty),
                    2,
                    MidpointRounding.AwayFromZero
                );

            toReturn.TotalWithVat =
                decimal.Round(
                    toReturn.TotalWithVat + taxFreeItem.TotalWithVat,
                    2,
                    MidpointRounding.AwayFromZero
                );

            taxFreeItem.VatAmountPl =
                decimal.Round(orderItem.TotalAmountLocal * (saleInvoiceDocument?.Vat ?? 23) / 100, 4, MidpointRounding.AwayFromZero);

            toReturn.VatAmountPl =
                decimal.Round(toReturn.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

            taxFreeItem.TotalWithoutVatPl =
                decimal.Round(orderItem.TotalAmountLocal - taxFreeItem.VatAmountPl, 4, MidpointRounding.AwayFromZero);

            taxFreeItem.UnitPricePL =
                decimal.Round(taxFreeItem.TotalWithoutVatPl / Convert.ToDecimal(taxFreeItem.Qty), 4, MidpointRounding.AwayFromZero);

            taxFreeItem.TotalWithVatPl += orderItem.TotalAmountLocal;

            toReturn.TotalWithVatPl =
                decimal.Round(toReturn.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

            product.MeasureUnit = measureUnit;

            orderItem.Product = product;

            taxFreePackListOrderItem.OrderItem = orderItem;

            taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

            toReturn.TaxFreeItems.Add(taxFreeItem);

            return taxFree;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                NetId = netId,
                Culture = "pl"
            }
        );

        return toReturn;
    }

    public TaxFree GetByNetIdForPrinting(Guid netId) {
        TaxFree toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [TaxFree].StathamCarID " +
            "LEFT JOIN [User] AS [TaxFreeResponsible] " +
            "ON [TaxFreeResponsible].ID = [TaxFree].ResponsibleID " +
            "LEFT JOIN [TaxFreeDocument] " +
            "ON [TaxFreeDocument].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeDocument].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [User] AS [TaxFreePackListResponsible] " +
            "ON [TaxFreePackListResponsible].ID = [TaxFreePackList].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].NetUID = @NetId";

        Type[] types = {
            typeof(TaxFree),
            typeof(Statham),
            typeof(StathamCar),
            typeof(User),
            typeof(TaxFreeDocument),
            typeof(TaxFreeItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(TaxFreePackList),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraine),
            typeof(StathamPassport)
        };

        Func<object[], TaxFree> mapper = objects => {
            TaxFree taxFree = (TaxFree)objects[0];
            Statham statham = (Statham)objects[1];
            StathamCar stathamCar = (StathamCar)objects[2];
            User taxFreeResponsible = (User)objects[3];
            TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            User createdBy = (User)objects[9];
            User updatedBy = (User)objects[10];
            User responsible = (User)objects[11];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[12];
            User packListUser = (User)objects[13];
            Organization organization = (Organization)objects[14];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[15];
            StathamPassport stathamPassport = (StathamPassport)objects[16];

            if (toReturn == null) {
                if (taxFreeItem != null) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

                    taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 3);

                    taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 3);

                    item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 3);

                    item.NetWeight = Math.Round(item.NetWeight, 3);

                    decimal exchangeRate = GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1));

                    item.UnitPrice =
                        decimal.Round(
                            item.UnitPrice + item.UnitPrice * taxFreePackList.MarginAmount / 100m,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    item.TotalAmount =
                        decimal.Round(
                            item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    item.TotalAmountLocal =
                        decimal.Round(
                            decimal.Round(item.UnitPrice * exchangeRate, 2, MidpointRounding.AwayFromZero) * Convert.ToDecimal(taxFreeItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFreeItem.UnitPricePL = decimal.Round(item.UnitPrice * exchangeRate, 2, MidpointRounding.AwayFromZero);
                    taxFreeItem.TotalWithoutVatPl = decimal.Round(taxFreeItem.UnitPricePL * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                    taxFreeItem.UnitPriceWithVat =
                        decimal.Round(
                            item.UnitPrice + item.UnitPrice * 0.23m,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFree.UnitPriceWithVat =
                        decimal.Round(
                            taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFreeItem.TotalWithVat =
                        decimal.Round(
                            item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFree.TotalWithVat =
                        decimal.Round(
                            taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFreeItem.VatAmountPl =
                        decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                    taxFree.VatAmountPl =
                        decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                    taxFreeItem.TotalWithVatPl =
                        decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                    taxFree.TotalWithVatPl =
                        decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFreePackList.Responsible = packListUser;
                taxFreePackList.Organization = organization;
                taxFreePackList.SupplyOrderUkraine = supplyOrderUkraine;

                taxFree.Statham = statham;
                taxFree.StathamCar = stathamCar;
                taxFree.StathamPassport = stathamPassport;
                taxFree.Responsible = taxFreeResponsible;
                taxFree.TaxFreePackList = taxFreePackList;

                toReturn = taxFree;
            } else {
                if (taxFreeItem != null && !toReturn.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

                    taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 3);

                    toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + taxFreeItem.TotalNetWeight, 3);

                    item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 3);

                    item.NetWeight = Math.Round(item.NetWeight, 3);

                    item.UnitPrice =
                        decimal.Round(
                            item.UnitPrice + item.UnitPrice * taxFreePackList.MarginAmount / 100m,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    decimal exchangeRate = GetPlnExchangeRate(toReturn.DateOfPrint?.AddDays(-1) ?? toReturn.Created.AddDays(-1));

                    item.TotalAmount =
                        decimal.Round(
                            item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    item.TotalAmountLocal =
                        decimal.Round(
                            decimal.Round(item.UnitPrice * exchangeRate, 2, MidpointRounding.AwayFromZero) * Convert.ToDecimal(taxFreeItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFreeItem.UnitPricePL = decimal.Round(item.UnitPrice * exchangeRate, 2, MidpointRounding.AwayFromZero);
                    taxFreeItem.TotalWithoutVatPl = decimal.Round(taxFreeItem.UnitPricePL * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                    taxFreeItem.UnitPriceWithVat =
                        decimal.Round(
                            item.UnitPrice + item.UnitPrice * 0.23m,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    toReturn.UnitPriceWithVat =
                        decimal.Round(
                            toReturn.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFreeItem.TotalWithVat =
                        decimal.Round(
                            item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    toReturn.TotalWithVat =
                        decimal.Round(
                            toReturn.TotalWithVat + taxFreeItem.TotalWithVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    taxFreeItem.VatAmountPl =
                        decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                    toReturn.VatAmountPl =
                        decimal.Round(toReturn.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                    taxFreeItem.TotalWithVatPl =
                        decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                    toReturn.TotalWithVatPl =
                        decimal.Round(toReturn.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    toReturn.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null && !toReturn.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) toReturn.TaxFreeDocuments.Add(taxFreeDocument);
            }

            return taxFree;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                NetId = netId,
                Culture = "pl"
            }
        );

        return toReturn;
    }

    public List<TaxFree> GetAllByPackListIdExceptProvided(long packListId, IEnumerable<long> ids) {
        List<TaxFree> taxFrees = new();

        _connection.Query<TaxFree, TaxFreeItem, SupplyOrderUkraineCartItem, TaxFree>(
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "WHERE [TaxFree].TaxFreePackListID = @PackListId " +
            "AND [TaxFree].ID NOT IN @Ids " +
            "AND [TaxFree].Deleted = 0",
            (taxFree, item, cartItem) => {
                if (!taxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                    if (item != null) {
                        item.SupplyOrderUkraineCartItem = cartItem;

                        taxFree.TaxFreeItems.Add(item);
                    }

                    taxFrees.Add(taxFree);
                } else {
                    TaxFree fromList = taxFrees.First(t => t.Id.Equals(taxFree.Id));

                    if (item == null || fromList.TaxFreeItems.Any(i => i.Id.Equals(item.Id))) return taxFree;

                    item.SupplyOrderUkraineCartItem = cartItem;

                    fromList.TaxFreeItems.Add(item);
                }

                return taxFree;
            },
            new { PackListId = packListId, Ids = ids }
        );

        return taxFrees;
    }

    public IEnumerable<TaxFree> GetAllFiltered(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        string value,
        TaxFreeStatus? status,
        Guid? stathamNetId
    ) {
        List<TaxFree> taxFrees = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS (" +
            "SELECT [TaxFree].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [TaxFreePackList].[Number] DESC, [TaxFree].[Number] DESC) AS [RowNumber] " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "WHERE [TaxFree].Deleted = 0 " +
            "AND [TaxFree].Created >= @From " +
            "AND [TaxFree].Created <= @To " +
            "AND (" +
            "[TaxFree].Number like N'%' + @Value + N'%' " +
            "OR" +
            "[Statham].LastName + [Statham].FirstName + [Statham].MiddleName like N'%' + @Value + N'%'" +
            ") ";

        if (status.HasValue) sqlExpression += "AND [TaxFree].TaxFreeStatus = @Status ";

        if (stathamNetId.HasValue) sqlExpression += "AND [Statham].NetUID = @StathamNetId ";

        sqlExpression +=
            ") " +
            "SELECT * " +
            "FROM [TaxFree] " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [TaxFree].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [TaxFree].StathamCarID " +
            "LEFT JOIN [User] AS [TaxFreeResponsible] " +
            "ON [TaxFreeResponsible].ID = [TaxFree].ResponsibleID " +
            "LEFT JOIN [TaxFreeDocument] " +
            "ON [TaxFreeDocument].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeDocument].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [User] AS [TaxFreePackListResponsible] " +
            "ON [TaxFreePackListResponsible].ID = [TaxFreePackList].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] AS [OrderItemProduct] " +
            "ON [OrderItem].ProductID = [OrderItemProduct].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [OrderItemProductMeasureUnit] " +
            "ON [OrderItemProductMeasureUnit].ID = [OrderItemProduct].MeasureUnitID " +
            "AND [OrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [TaxFreePackList].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [TaxFree].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ") " +
            "ORDER BY [TaxFreePackList].[Number] DESC, [TaxFree].[Number] DESC";

        Type[] types = {
            typeof(TaxFree),
            typeof(Statham),
            typeof(StathamCar),
            typeof(User),
            typeof(TaxFreeDocument),
            typeof(TaxFreeItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(TaxFreePackList),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraine),
            typeof(TaxFreePackListOrderItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(StathamPassport),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], TaxFree> mapper = objects => {
            TaxFree taxFree = (TaxFree)objects[0];
            Statham statham = (Statham)objects[1];
            StathamCar stathamCar = (StathamCar)objects[2];
            User taxFreeResponsible = (User)objects[3];
            TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            User createdBy = (User)objects[9];
            User updatedBy = (User)objects[10];
            User responsible = (User)objects[11];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[12];
            User packListUser = (User)objects[13];
            Organization organization = (Organization)objects[14];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[15];
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            Product orderItemProduct = (Product)objects[18];
            MeasureUnit orderItemProductMeasureUnit = (MeasureUnit)objects[19];
            StathamPassport stathamPassport = (StathamPassport)objects[20];
            Client client = (Client)objects[21];
            ClientAgreement clientAgreement = (ClientAgreement)objects[22];
            Agreement agreement = (Agreement)objects[23];
            Currency currency = (Currency)objects[24];

            if (!taxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                if (taxFreeItem != null) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.UnitPriceWithVat =
                            decimal.Round(
                                taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.TotalWithVat =
                            decimal.Round(
                                taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.SupplyOrderUkraineCartItem = item;
                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                if (clientAgreement != null) {
                    agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;
                }

                taxFreePackList.Client = client;
                taxFreePackList.ClientAgreement = clientAgreement;
                taxFreePackList.Responsible = packListUser;
                taxFreePackList.Organization = organization;
                taxFreePackList.SupplyOrderUkraine = supplyOrderUkraine;

                taxFree.Statham = statham;
                taxFree.StathamCar = stathamCar;
                taxFree.StathamPassport = stathamPassport;
                taxFree.Responsible = taxFreeResponsible;
                taxFree.TaxFreePackList = taxFreePackList;

                taxFrees.Add(taxFree);
            } else {
                TaxFree fromList = taxFrees.First(t => t.Id.Equals(taxFree.Id));

                if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1));

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.UnitPriceWithVat =
                            decimal.Round(
                                fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.TotalWithVat =
                            decimal.Round(
                                fromList.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl =
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                            ? GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1))
                            : orderItem.ExchangeRateAmount;

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                orderItem.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat =
                            decimal.Round(
                                orderItem.PricePerItem + orderItem.PricePerItem * 0.23m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.UnitPriceWithVat =
                            decimal.Round(
                                fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                orderItem.TotalAmount + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.TotalWithVat =
                            decimal.Round(
                                fromList.TotalWithVat + taxFreeItem.TotalWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(orderItem.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl =
                            decimal.Round(orderItem.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl =
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;
                    }

                    taxFreeItem.SupplyOrderUkraineCartItem = item;
                    taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

                    fromList.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null && !fromList.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) fromList.TaxFreeDocuments.Add(taxFreeDocument);
            }

            return taxFree;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Value = value,
                Status = status,
                StathamNetId = stathamNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            },
            commandTimeout: 3600
        );

        return taxFrees;
    }

    private decimal GetPlnExchangeRate(DateTime fromDate) {
        Currency pln =
            _exchangeRateConnection.Query<Currency>(
                    "SELECT TOP(1) * " +
                    "FROM [Currency] " +
                    "WHERE [Currency].Deleted = 0 " +
                    "AND [Currency].Code = 'pln'"
                )
                .SingleOrDefault();

        if (pln != null) {
            ExchangeRate exchangeRate =
                _exchangeRateConnection.Query<ExchangeRate>(
                        "SELECT TOP(1) " +
                        "[ExchangeRate].ID, " +
                        "(CASE " +
                        "WHEN [ExchangeRateHistory].Amount IS NOT NULL " +
                        "THEN [ExchangeRateHistory].Amount " +
                        "ELSE [ExchangeRate].Amount " +
                        "END) AS [Amount] " +
                        "FROM [ExchangeRate] " +
                        "LEFT JOIN [ExchangeRateHistory] " +
                        "ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID " +
                        "AND [ExchangeRateHistory].Created <= @FromDate " +
                        "WHERE [ExchangeRate].CurrencyID = @Id " +
                        "AND [ExchangeRate].Code = @Code " +
                        "ORDER BY [ExchangeRateHistory].Created DESC",
                        new { pln.Id, Code = "EUR", FromDate = fromDate }
                    )
                    .FirstOrDefault();

            return exchangeRate?.Amount ?? 1m;
        }

        return 1m;
    }
}