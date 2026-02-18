using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Extensions;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductIncomeRepository : IProductIncomeRepository {
    private readonly IDbConnection _connection;

    public ProductIncomeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductIncome productIncome) {
        return _connection.Query<long>(
            "INSERT INTO [ProductIncome] (FromDate, Number, UserId, StorageId, ProductIncomeType, Comment, Updated) " +
            "VALUES (@FromDate, @Number, @UserId, @StorageId, @ProductIncomeType, @Comment, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productIncome
        ).Single();
    }

    public void RemoveAllBySaleReturnItemIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductIncome] " +
            "SET Deleted = 0 " +
            "FROM [ProductIncome] " +
            "WHERE ID IN (" +
            "SELECT [ProductIncome].ID " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "WHERE [ProductIncomeItem].SaleReturnItemID IN @Ids" +
            ")",
            new { Ids = ids }
        );
    }

    public ProductIncome GetById(long id) {
        ProductIncome toReturn =
            _connection.Query<ProductIncome>(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "WHERE [ProductIncome].ID = @Id",
                new { Id = id }
            ).SingleOrDefault();

        if (toReturn == null) return null;

        string returnsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [SaleReturnItem].ID IS NOT NULL";

        Type[] returnsTypes = {
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> returnsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
            SaleReturn saleReturn = (SaleReturn)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            User returnCreatedBy = (User)objects[5];
            User itemCreatedBy = (User)objects[6];
            User moneyReturnedBy = (User)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            Product product = (Product)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            Order order = (Order)objects[11];
            Sale sale = (Sale)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Agreement agreement = (Agreement)objects[14];
            Organization organization = (Organization)objects[15];
            Currency currency = (Currency)objects[16];
            Pricing pricing = (Pricing)objects[17];
            SaleNumber saleNumber = (SaleNumber)objects[18];
            Storage storage = (Storage)objects[19];

            if (!toReturn.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) {
                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.SaleNumber = saleNumber;

                order.Sale = sale;

                product.MeasureUnit = measureUnit;
                product.CurrentPrice = orderItem.PricePerItem;

                if (culture.Equals("pl")) {
                    product.Name = product.NameUA;
                    product.Description = product.DescriptionUA;
                } else {
                    product.Name = product.NameUA;
                    product.Description = product.DescriptionUA;
                }

                orderItem.Product = product;
                orderItem.Order = order;

                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (saleReturnItem != null) {
                    if (saleReturn != null) {
                        client.RegionCode = regionCode;

                        saleReturn.Client = client;
                        saleReturn.CreatedBy = returnCreatedBy;
                    }

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;
                    saleReturnItem.SaleReturn = saleReturn;
                }

                productIncomeItem.SaleReturnItem = saleReturnItem;

                toReturn.ProductIncomeItems.Add(productIncomeItem);
            }

            toReturn.Storage = storage;
            toReturn.User = itemCreatedBy;

            return productIncomeItem;
        };

        _connection.Query(
            returnsSqlExpression,
            returnsTypes,
            returnsMapper,
            new { Culture = culture, Id = id }
        );

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];

            if (toReturn.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null)
                supplyOrderItem.Product = product;

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            toReturn.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, Id = id }
        );

        string capitalizationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL";

        Type[] capitalizationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductCapitalization),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], ProductIncomeItem> capitalizationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[4];
            Organization organization = (Organization)objects[5];
            User responsible = (User)objects[6];
            Storage storage = (Storage)objects[7];

            if (toReturn.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            productCapitalization.Organization = organization;
            productCapitalization.Responsible = responsible;
            productCapitalization.Storage = storage;

            productCapitalizationItem.Product = product;
            productCapitalizationItem.ProductCapitalization = productCapitalization;

            productCapitalizationItem.TotalAmount =
                decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);

            productCapitalizationItem.TotalNetWeight = Math.Round(productCapitalizationItem.Qty * productCapitalizationItem.Weight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;

            toReturn.TotalQty += productIncomeItem.Qty;

            toReturn.TotalNetPrice =
                decimal.Round(toReturn.TotalNetPrice + productCapitalizationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            toReturn.TotalNetWeight =
                Math.Round(
                    toReturn.TotalNetWeight + productCapitalizationItem.TotalNetWeight,
                    3,
                    MidpointRounding.AwayFromZero
                );

            toReturn.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            capitalizationsSqlExpression,
            capitalizationsTypes,
            capitalizationsMapper,
            new { Culture = culture, Id = id }
        );

        return toReturn;
    }

    public ProductIncome GetByIdForConsignmentCreate(long id) {
        ProductIncome toReturn = null;

        Type[] types = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(SaleReturnItem),
            typeof(OrderItem),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(ActReconciliationItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(ProductCapitalizationItem),
            typeof(ProductCapitalization),
            typeof(Organization),
            typeof(OrderProductSpecification),
            typeof(ProductSpecification),
            typeof(ConsignmentItemMovement),
            typeof(ConsignmentItem),
            typeof(ProductSpecification),
            typeof(ProductSpecification)
        };

        Func<object[], ProductIncome> mapper = objects => {
            ProductIncome income = (ProductIncome)objects[0];
            ProductIncomeItem incomeItem = (ProductIncomeItem)objects[1];

            SupplyOrderUkraineItem ukraineItem = (SupplyOrderUkraineItem)objects[2];
            SupplyOrderUkraine orderUkraine = (SupplyOrderUkraine)objects[3];

            PackingListPackageOrderItem packingListOrderItem = (PackingListPackageOrderItem)objects[4];
            SupplyInvoiceOrderItem invoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[7];
            SupplyOrder supplyOrder = (SupplyOrder)objects[8];

            SaleReturnItem returnItem = (SaleReturnItem)objects[9];
            OrderItem orderItem = (OrderItem)objects[10];
            Order order = (Order)objects[11];
            Sale sale = (Sale)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Agreement agreement = (Agreement)objects[14];

            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[15];
            SupplyOrderUkraineItem actUkraineItem = (SupplyOrderUkraineItem)objects[16];
            SupplyOrderUkraine actOrderUkraine = (SupplyOrderUkraine)objects[17];
            SupplyInvoiceOrderItem actInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[18];
            SupplyOrderItem actSupplyOrderItem = (SupplyOrderItem)objects[19];
            SupplyOrder actSupplyOrder = (SupplyOrder)objects[20];

            ProductCapitalizationItem capitalizationItem = (ProductCapitalizationItem)objects[21];
            ProductCapitalization capitalization = (ProductCapitalization)objects[22];

            Organization organization = (Organization)objects[23];

            OrderProductSpecification orderProductSpecification = (OrderProductSpecification)objects[24];
            ProductSpecification productSpecification = (ProductSpecification)objects[25];

            ConsignmentItemMovement saleMovement = (ConsignmentItemMovement)objects[26];
            ConsignmentItem saleMovementConsignmentItem = (ConsignmentItem)objects[27];

            ProductSpecification productSpecificationSupplyUkraineItem = (ProductSpecification)objects[28];
            ProductSpecification productSpecificationSaleReturn = (ProductSpecification)objects[29];

            if (toReturn == null) {
                income.Organization = organization;

                toReturn = income;
            }

            if (incomeItem == null)
                return income;

            if (toReturn.ProductIncomeItems.Any(i => i.Id.Equals(incomeItem.Id)))
                incomeItem = toReturn.ProductIncomeItems.First(i => i.Id.Equals(incomeItem.Id));
            else
                toReturn.ProductIncomeItems.Add(incomeItem);

            if (ukraineItem != null) {
                ukraineItem.SupplyOrderUkraine = orderUkraine;

                ukraineItem.ProductSpecification = productSpecificationSupplyUkraineItem;
            }

            if (packingListOrderItem != null) {
                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                invoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                packingListOrderItem.SupplyInvoiceOrderItem = invoiceOrderItem;
            }

            if (actReconciliationItem != null) {
                if (actUkraineItem != null)
                    actUkraineItem.SupplyOrderUkraine = actOrderUkraine;

                if (actInvoiceOrderItem != null) {
                    if (actSupplyOrderItem != null)
                        actSupplyOrderItem.SupplyOrder = actSupplyOrder;

                    actInvoiceOrderItem.SupplyOrderItem = actSupplyOrderItem;
                }

                actReconciliationItem.SupplyOrderUkraineItem = actUkraineItem;
                actReconciliationItem.SupplyInvoiceOrderItem = actInvoiceOrderItem;
            }

            if (capitalizationItem != null)
                capitalizationItem.ProductCapitalization = capitalization;

            if (orderProductSpecification != null)
                orderProductSpecification.ProductSpecification = productSpecification;

            incomeItem.SupplyOrderUkraineItem = ukraineItem;
            incomeItem.PackingListPackageOrderItem = packingListOrderItem;

            if (returnItem != null) {
                if (incomeItem.SaleReturnItem == null)
                    incomeItem.SaleReturnItem = returnItem;
                else
                    returnItem = incomeItem.SaleReturnItem;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;

                order.Sale = sale;

                orderItem.Order = order;

                if (returnItem.OrderItem == null)
                    returnItem.OrderItem = orderItem;
                else
                    orderItem = returnItem.OrderItem;
            }

            incomeItem.ActReconciliationItem = actReconciliationItem;
            incomeItem.ProductCapitalizationItem = capitalizationItem;
            incomeItem.OrderProductSpecification = orderProductSpecification;

            if (saleMovement == null || orderItem == null) return income;

            saleMovementConsignmentItem.ProductSpecification = productSpecificationSaleReturn;

            saleMovement.ConsignmentItem = saleMovementConsignmentItem;

            orderItem.ConsignmentItemMovements.Add(saleMovement);

            return income;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ActReconciliationItem].ID = [ProductIncomeItem].ActReconciliationItemID " +
            "LEFT JOIN [SupplyOrderUkraineItem] AS [ActSupplyOrderUkraineItem] " +
            "ON [ActSupplyOrderUkraineItem].ID = [ActReconciliationItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] AS [ActSupplyOrderUkraine] " +
            "ON [ActSupplyOrderUkraine].ID = [ActSupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN ( " +
            "SELECT [JoinItem].ID " +
            ", [JoinItem].Qty " +
            ", [JoinItem].SupplyInvoiceID " +
            ", [JoinItem].SupplyOrderItemID " +
            ", ( " +
            "SELECT ROUND(SUM([PackListItem].UnitPriceEur * [PackListItem].Qty) / SUM([PackListItem].Qty), 2) " +
            "FROM [PackingListPackageOrderItem] AS [PackListItem] " +
            "WHERE [PackListItem].SupplyInvoiceOrderItemID = [JoinItem].ID " +
            ") AS [UnitPrice] " +
            ", ( " +
            "SELECT ROUND(SUM([PackListItem].GrossUnitPriceEur * [PackListItem].Qty) / SUM([PackListItem].Qty), 2) " +
            "FROM [PackingListPackageOrderItem] AS [PackListItem] " +
            "WHERE [PackListItem].SupplyInvoiceOrderItemID = [JoinItem].ID " +
            ") AS [GrossUnitPrice] " +
            ", ( " +
            "SELECT ROUND(SUM([PackListItem].GrossWeight * [PackListItem].Qty) / SUM([PackListItem].Qty), 3) " +
            "FROM [PackingListPackageOrderItem] AS [PackListItem] " +
            "WHERE [PackListItem].SupplyInvoiceOrderItemID = [JoinItem].ID " +
            ") AS [Weight] " +
            "FROM [SupplyInvoiceOrderItem] AS [JoinItem] " +
            ") AS [ActSupplyInvoiceOrderItem] " +
            "ON [ActSupplyInvoiceOrderItem].ID = [ActReconciliationItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] AS [ActSupplyOrderItem] " +
            "ON [ActSupplyOrderItem].ID = [ActSupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyOrder] AS [ActSupplyOrder] " +
            "ON [ActSupplyOrder].ID = [ActSupplyOrderItem].SupplyOrderID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrderUkraine].OrganizationID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].OrganizationID " +
            "WHEN [SupplyOrder].OrganizationID IS NOT NULL " +
            "THEN [SupplyOrder].OrganizationID " +
            "WHEN [Agreement].OrganizationID IS NOT NULL " +
            "THEN [Agreement].OrganizationID " +
            "WHEN [ActSupplyOrderUkraine].OrganizationID IS NOT NULL " +
            "THEN [ActSupplyOrderUkraine].OrganizationID " +
            "WHEN [ActSupplyOrder].OrganizationID IS NOT NULL " +
            "THEN [ActSupplyOrder].OrganizationID " +
            "ELSE [ProductCapitalization].OrganizationID " +
            "END " +
            ") " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ID = ( " +
            "SELECT TOP(1) [JoinSpecification].ID " +
            "FROM [OrderProductSpecification] AS [JoinSpecification] " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [JoinSpecification].SadID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = [JoinSpecification].ProductSpecificationID " +
            "WHERE [ProductSpecification].Locale = [Organization].Culture " +
            "AND ( " +
            "( " +
            "[Sad].SupplyOrderUkraineID IS NOT NULL " +
            "AND " +
            "( " +
            "[Sad].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "OR " +
            "[Sad].SupplyOrderUkraineID = [ActSupplyOrderUkraine].ID " +
            ") " +
            ") " +
            "OR " +
            "([JoinSpecification].SupplyInvoiceID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "AND [SupplyInvoiceOrderItem].[ProductID] = [ProductSpecification].[ProductID]) " +
            "OR " +
            "[JoinSpecification].SupplyInvoiceID = [ActSupplyInvoiceOrderItem].SupplyInvoiceID " +
            ") " +
            "ORDER BY [JoinSpecification].ID DESC " +
            ") " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = [OrderProductSpecification].ProductSpecificationID " +
            "LEFT JOIN [ConsignmentItemMovement] " +
            "ON [ConsignmentItemMovement].OrderItemID = [OrderItem].ID " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].[MovementType] = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [ProductSpecification] AS [ProductSpecificationSupplyOrderUkraineItem] " +
            "ON [ProductSpecificationSupplyOrderUkraineItem].[ID] = [SupplyOrderUkraineItem].[ProductSpecificationID] " +
            "LEFT JOIN [ProductSpecification] AS [SaleReturnSpecification] " +
            "ON [SaleReturnSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [ProductIncome].ID = @Id",
            types,
            mapper,
            new { Id = id },
            commandTimeout: 3600
        );

        return toReturn;
    }

    public ProductIncome GetBySupplyOrderNetId(Guid netId) {
        ProductIncome income =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                "SELECT TOP(1) [ProductIncome].* " +
                ", [User].* " +
                ", [Storage].* " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [PackingListPackage] " +
                "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
                "LEFT JOIN [PackingList] AS [PackagePackingList] " +
                "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [ProductIncome].UserID = [User].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "WHERE [SupplyOrder].NetUID = @NetId",
                (productIncome, user, storage) => {
                    productIncome.User = user;
                    productIncome.Storage = storage;

                    return productIncome;
                },
                new { NetId = netId }
            ).SingleOrDefault();

        if (income == null) return null;

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "AND [ConsignmentItem].[Deleted] = 0 " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(ConsignmentItem),
            typeof(ProductSpecification)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[14];
            ProductSpecification productSpecification = (ProductSpecification)objects[15];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (consignmentItem != null) {
                consignmentItem.ProductSpecification = productSpecification;
                productIncomeItem.ConsignmentItems.Add(consignmentItem);
            }

            if (supplyOrder != null) {
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, income.Id }
        );

        return income;
    }

    public ProductIncome GetByDeliveryProductProtocolNetId(Guid netId) {
        ProductIncome income =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                "SELECT TOP(1) [ProductIncome].* " +
                ", [User].* " +
                ", [Storage].* " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [PackingListPackage] " +
                "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
                "LEFT JOIN [PackingList] AS [PackagePackingList] " +
                "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [DeliveryProductProtocol] " +
                "ON [DeliveryProductProtocol].ID = [SupplyInvoice].DeliveryProductProtocolID " +
                "LEFT JOIN [User] " +
                "ON [ProductIncome].UserID = [User].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "WHERE [DeliveryProductProtocol].NetUID = @NetId",
                (productIncome, user, storage) => {
                    productIncome.User = user;
                    productIncome.Storage = storage;

                    return productIncome;
                },
                new { NetId = netId }
            ).SingleOrDefault();

        if (income == null) return null;

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "AND [ConsignmentItem].[Deleted] = 0 " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(ConsignmentItem),
            typeof(ProductSpecification)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[14];
            ProductSpecification productSpecification = (ProductSpecification)objects[15];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;


            if (consignmentItem != null) {
                consignmentItem.ProductSpecification = productSpecification;
                productIncomeItem.ConsignmentItems.Add(consignmentItem);
            }

            if (supplyOrder != null) {
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            ;

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, income.Id }
        );

        return income;
    }

    public ProductIncome GetByNetId(Guid netId) {
        ProductIncome income =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [ProductIncome].UserID = [User].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "WHERE [ProductIncome].NetUID = @NetId",
                (productIncome, user, storage) => {
                    productIncome.User = user;
                    productIncome.Storage = storage;

                    return productIncome;
                },
                new { NetId = netId }
            ).SingleOrDefault();

        if (income == null) return null;

        string returnsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [SaleReturnItem].ID IS NOT NULL";

        Type[] returnsTypes = {
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> returnsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
            SaleReturn saleReturn = (SaleReturn)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            User returnCreatedBy = (User)objects[5];
            User itemCreatedBy = (User)objects[6];
            User moneyReturnedBy = (User)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            Product product = (Product)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            Order order = (Order)objects[11];
            Sale sale = (Sale)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Agreement agreement = (Agreement)objects[14];
            Organization organization = (Organization)objects[15];
            Currency currency = (Currency)objects[16];
            Pricing pricing = (Pricing)objects[17];
            SaleNumber saleNumber = (SaleNumber)objects[18];
            Storage storage = (Storage)objects[19];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            agreement.Pricing = pricing;
            agreement.Organization = organization;
            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            order.Sale = sale;

            product.MeasureUnit = measureUnit;
            product.CurrentPrice = orderItem.PricePerItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            orderItem.Product = product;
            orderItem.Order = order;

            orderItem.Product.CurrentLocalPrice =
                decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount =
                decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

            orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += orderItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            client.RegionCode = regionCode;

            saleReturn.CreatedBy = returnCreatedBy;
            saleReturn.Client = client;
            saleReturn.Sale = sale;

            saleReturnItem.OrderItem = orderItem;
            saleReturnItem.CreatedBy = itemCreatedBy;
            saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
            saleReturnItem.Storage = storage;
            saleReturnItem.SaleReturn = saleReturn;

            productIncomeItem.SaleReturnItem = saleReturnItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            returnsSqlExpression,
            returnsTypes,
            returnsMapper,
            new { Culture = culture, income.Id }
        );

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization)
        };

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (supplyOrder != null) {
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, income.Id }
        );

        string ukraineOrdersSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [SupplyOrderUkraineItem].ID IS NOT NULL";

        Type[] ukraineOrdersTypes = {
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(Organization)
        };

        Func<object[], ProductIncomeItem> ukraineOrdersMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            User responsible = (User)objects[5];
            Organization organization = (Organization)objects[6];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Responsible = responsible;

            supplyOrderUkraineItem.Product = product;
            supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

            supplyOrderUkraineItem.TotalNetWeight = Math.Round(supplyOrderUkraineItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetWeight = Math.Round(supplyOrderUkraineItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetPrice =
                decimal.Round(supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + supplyOrderUkraineItem.NetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + supplyOrderUkraineItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            ukraineOrdersSqlExpression,
            ukraineOrdersTypes,
            ukraineOrdersMapper,
            new { Culture = culture, income.Id }
        );

        string reconciliationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ActReconciliationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [User] AS [ActUser] " +
            "ON [ActReconciliation].ResponsibleID = [ActUser].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [ActReconciliation].SupplyOrderUkraineID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [ActReconciliation].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
            "AND [SupplyOrderOrganization].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [ActReconciliationItem].ID IS NOT NULL";

        Type[] reconciliationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ActReconciliationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ActReconciliation),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Organization)
        };

        Func<object[], ProductIncomeItem> reconciliationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ActReconciliation actReconciliation = (ActReconciliation)objects[4];
            User user = (User)objects[5];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
            User ukResponsible = (User)objects[7];
            Organization ukOrganization = (Organization)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Organization plOrganization = (Organization)objects[11];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (supplyOrderUkraine != null) {
                supplyOrderUkraine.Organization = ukOrganization;
                supplyOrderUkraine.Responsible = ukResponsible;
            }

            if (supplyInvoice != null) {
                supplyInvoice.SupplyOrder = supplyOrder;
                supplyOrder.Organization = plOrganization;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            actReconciliation.Responsible = user;
            actReconciliation.SupplyInvoice = supplyInvoice;
            actReconciliation.SupplyOrderUkraine = supplyOrderUkraine;

            actReconciliationItem.Product = product;
            actReconciliationItem.ActReconciliation = actReconciliation;

            productIncomeItem.ActReconciliationItem = actReconciliationItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            reconciliationsSqlExpression,
            reconciliationsTypes,
            reconciliationsMapper,
            new { Culture = culture, income.Id }
        );

        string capitalizationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL";

        Type[] capitalizationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductCapitalization),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], ProductIncomeItem> capitalizationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[4];
            Organization organization = (Organization)objects[5];
            User responsible = (User)objects[6];
            Storage storage = (Storage)objects[7];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            productCapitalization.Organization = organization;
            productCapitalization.Responsible = responsible;
            productCapitalization.Storage = storage;

            productCapitalizationItem.Product = product;
            productCapitalizationItem.ProductCapitalization = productCapitalization;

            productCapitalizationItem.TotalAmount =
                decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);

            productCapitalizationItem.TotalNetWeight = Math.Round(productCapitalizationItem.Qty * productCapitalizationItem.Weight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + productCapitalizationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            income.TotalNetWeight =
                Math.Round(
                    income.TotalNetWeight + productCapitalizationItem.TotalNetWeight,
                    3,
                    MidpointRounding.AwayFromZero
                );

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            capitalizationsSqlExpression,
            capitalizationsTypes,
            capitalizationsMapper,
            new { Culture = culture, income.Id }
        );

        return income;
    }

    public ProductIncome GetByNetIdForPrintingDocument(Guid netId) {
        ProductIncome income =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [ProductIncome].UserID = [User].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "WHERE [ProductIncome].NetUID = @NetId",
                (productIncome, user, storage) => {
                    productIncome.User = user;
                    productIncome.Storage = storage;

                    return productIncome;
                },
                new { NetId = netId }
            ).SingleOrDefault();

        if (income == null) return null;

        string returnsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [SaleReturnItem].ID IS NOT NULL";

        Type[] returnsTypes = {
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> returnsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
            SaleReturn saleReturn = (SaleReturn)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            User returnCreatedBy = (User)objects[5];
            User itemCreatedBy = (User)objects[6];
            User moneyReturnedBy = (User)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            Product product = (Product)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            Order order = (Order)objects[11];
            Sale sale = (Sale)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Agreement agreement = (Agreement)objects[14];
            Organization organization = (Organization)objects[15];
            Currency currency = (Currency)objects[16];
            Pricing pricing = (Pricing)objects[17];
            SaleNumber saleNumber = (SaleNumber)objects[18];
            Storage storage = (Storage)objects[19];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            agreement.Pricing = pricing;
            agreement.Organization = organization;
            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            order.Sale = sale;

            product.MeasureUnit = measureUnit;
            product.CurrentPrice = orderItem.PricePerItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            orderItem.Product = product;
            orderItem.Order = order;

            orderItem.Product.CurrentLocalPrice =
                decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount =
                decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

            orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += orderItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            client.RegionCode = regionCode;

            saleReturn.CreatedBy = returnCreatedBy;
            saleReturn.Client = client;
            saleReturn.Sale = sale;

            saleReturnItem.OrderItem = orderItem;
            saleReturnItem.CreatedBy = itemCreatedBy;
            saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
            saleReturnItem.Storage = storage;
            saleReturnItem.SaleReturn = saleReturn;

            productIncomeItem.SaleReturnItem = saleReturnItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            returnsSqlExpression,
            returnsTypes,
            returnsMapper,
            new { Culture = culture, income.Id }
        );

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];
            ClientAgreement clientAgreement = (ClientAgreement)objects[14];
            Agreement agreement = (Agreement)objects[15];
            Currency currency = (Currency)objects[16];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (supplyOrder != null) {
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                if (clientAgreement != null && agreement != null && currency != null) {
                    agreement.Currency = currency;
                    clientAgreement.Agreement = agreement;
                    supplyOrder.ClientAgreement = clientAgreement;
                }
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, income.Id }
        );

        string ukraineOrdersSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[Id] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [SupplyOrderUkraineItem].ID IS NOT NULL";

        Type[] ukraineOrdersTypes = {
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> ukraineOrdersMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            User responsible = (User)objects[5];
            Organization organization = (Organization)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Client client = (Client)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Currency currency = (Currency)objects[10];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (clientAgreement != null && client != null && agreement != null && currency != null) {
                agreement.Currency = currency;
                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
                supplyOrderUkraine.ClientAgreement = clientAgreement;
            }

            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Responsible = responsible;

            supplyOrderUkraineItem.Product = product;
            supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

            supplyOrderUkraineItem.TotalNetWeight = Math.Round(supplyOrderUkraineItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetWeight = Math.Round(supplyOrderUkraineItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetPrice =
                decimal.Round(supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + supplyOrderUkraineItem.NetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + supplyOrderUkraineItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            ukraineOrdersSqlExpression,
            ukraineOrdersTypes,
            ukraineOrdersMapper,
            new { Culture = culture, income.Id }
        );

        string reconciliationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ActReconciliationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [User] AS [ActUser] " +
            "ON [ActReconciliation].ResponsibleID = [ActUser].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [ActReconciliation].SupplyOrderUkraineID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [ActReconciliation].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
            "AND [SupplyOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "OR [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [ActReconciliationItem].ID IS NOT NULL";

        Type[] reconciliationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ActReconciliationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ActReconciliation),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> reconciliationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ActReconciliation actReconciliation = (ActReconciliation)objects[4];
            User user = (User)objects[5];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
            User ukResponsible = (User)objects[7];
            Organization ukOrganization = (Organization)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Organization plOrganization = (Organization)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Client client = (Client)objects[13];
            Agreement agreement = (Agreement)objects[14];
            Currency currency = (Currency)objects[15];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (agreement != null && currency != null && client != null && clientAgreement != null) {
                agreement.Currency = currency;
                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
            }

            if (supplyOrderUkraine != null) {
                supplyOrderUkraine.Organization = ukOrganization;
                supplyOrderUkraine.Responsible = ukResponsible;

                if (clientAgreement != null)
                    supplyOrderUkraine.ClientAgreement = clientAgreement;
            }

            if (supplyInvoice != null) {
                supplyInvoice.SupplyOrder = supplyOrder;
                supplyOrder.Organization = plOrganization;

                if (clientAgreement != null)
                    supplyOrder.ClientAgreement = clientAgreement;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            actReconciliation.Responsible = user;
            actReconciliation.SupplyInvoice = supplyInvoice;
            actReconciliation.SupplyOrderUkraine = supplyOrderUkraine;

            actReconciliationItem.Product = product;
            actReconciliationItem.ActReconciliation = actReconciliation;

            productIncomeItem.ActReconciliationItem = actReconciliationItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            reconciliationsSqlExpression,
            reconciliationsTypes,
            reconciliationsMapper,
            new { Culture = culture, income.Id }
        );

        string capitalizationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID = @Id " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL";

        Type[] capitalizationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductCapitalization),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], ProductIncomeItem> capitalizationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[4];
            Organization organization = (Organization)objects[5];
            User responsible = (User)objects[6];
            Storage storage = (Storage)objects[7];

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            productCapitalization.Organization = organization;
            productCapitalization.Responsible = responsible;
            productCapitalization.Storage = storage;

            productCapitalizationItem.Product = product;
            productCapitalizationItem.ProductCapitalization = productCapitalization;

            productCapitalizationItem.TotalAmount =
                decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);

            productCapitalizationItem.TotalNetWeight = Math.Round(productCapitalizationItem.Qty * productCapitalizationItem.Weight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + productCapitalizationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            income.TotalNetWeight =
                Math.Round(
                    income.TotalNetWeight + productCapitalizationItem.TotalNetWeight,
                    3,
                    MidpointRounding.AwayFromZero
                );

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            capitalizationsSqlExpression,
            capitalizationsTypes,
            capitalizationsMapper,
            new { Culture = culture, income.Id }
        );

        return income;
    }

    public List<ProductIncome> GetAllBySupplyOrderUkraineNetId(Guid netId) {
        IEnumerable<long> ids =
            _connection.Query<long>(
                "SELECT [ProductIncome].ID " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "WHERE [SupplyOrderUkraine].NetUID = @NetId " +
                "AND [ProductIncome].Deleted = 0 " +
                "GROUP BY [ProductIncome].ID",
                new { NetId = netId }
            );

        if (ids.Any()) {
            List<ProductIncome> incomes =
                _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                    "SELECT * " +
                    "FROM [ProductIncome] " +
                    "LEFT JOIN [User] " +
                    "ON [ProductIncome].UserID = [User].ID " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].ID = [ProductIncome].StorageID " +
                    "WHERE [ProductIncome].ID IN @Ids " +
                    "ORDER BY [ProductIncome].ID DESC",
                    (productIncome, user, storage) => {
                        productIncome.User = user;
                        productIncome.Storage = storage;

                        return productIncome;
                    },
                    new { Ids = ids }
                ).ToList();

            string ukraineOrdersSqlExpression =
                "SELECT * " +
                "FROM [ProductIncomeItem] " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
                "AND [SupplyOrderUkraineItem].ID IS NOT NULL";

            Type[] ukraineOrdersTypes = {
                typeof(ProductIncomeItem),
                typeof(SupplyOrderUkraineItem),
                typeof(SupplyOrderUkraine),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(Organization)
            };

            string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            Func<object[], ProductIncomeItem> ukraineOrdersMapper = objects => {
                ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
                SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[2];
                Product product = (Product)objects[3];
                MeasureUnit measureUnit = (MeasureUnit)objects[4];
                User responsible = (User)objects[5];
                Organization organization = (Organization)objects[6];

                ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

                if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

                if (culture.Equals("pl")) {
                    product.Name = product.NameUA;
                    product.Description = product.DescriptionUA;
                } else {
                    product.Name = product.NameUA;
                    product.Description = product.DescriptionUA;
                }

                product.MeasureUnit = measureUnit;

                supplyOrderUkraine.Organization = organization;
                supplyOrderUkraine.Responsible = responsible;

                supplyOrderUkraineItem.Product = product;
                supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

                supplyOrderUkraineItem.TotalNetWeight = Math.Round(supplyOrderUkraineItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

                supplyOrderUkraineItem.NetWeight = Math.Round(supplyOrderUkraineItem.NetWeight, 3, MidpointRounding.AwayFromZero);

                supplyOrderUkraineItem.NetPrice =
                    decimal.Round(supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

                income.TotalNetPrice =
                    decimal.Round(income.TotalNetPrice + supplyOrderUkraineItem.NetPrice, 2, MidpointRounding.AwayFromZero);

                income.TotalQty += productIncomeItem.Qty;

                income.TotalNetWeight =
                    Math.Round(income.TotalNetWeight + supplyOrderUkraineItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

                productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

                income.ProductIncomeItems.Add(productIncomeItem);

                return productIncomeItem;
            };

            _connection.Query(
                ukraineOrdersSqlExpression,
                ukraineOrdersTypes,
                ukraineOrdersMapper,
                new { Culture = culture, Ids = ids }
            );

            return incomes;
        }

        return new List<ProductIncome>();
    }

    public List<ProductIncome> GetAll() {
        List<ProductIncome> toReturn = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID";

        Type[] types = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncome> mapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[1];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[2];
            SaleReturn saleReturn = (SaleReturn)objects[3];
            Client client = (Client)objects[4];
            RegionCode regionCode = (RegionCode)objects[5];
            User returnCreatedBy = (User)objects[6];
            User itemCreatedBy = (User)objects[7];
            User moneyReturnedBy = (User)objects[8];
            OrderItem orderItem = (OrderItem)objects[9];
            Product product = (Product)objects[10];
            MeasureUnit measureUnit = (MeasureUnit)objects[11];
            Order order = (Order)objects[12];
            Sale sale = (Sale)objects[13];
            ClientAgreement clientAgreement = (ClientAgreement)objects[14];
            Agreement agreement = (Agreement)objects[15];
            Organization organization = (Organization)objects[16];
            Currency currency = (Currency)objects[17];
            Pricing pricing = (Pricing)objects[18];
            SaleNumber saleNumber = (SaleNumber)objects[19];
            Storage storage = (Storage)objects[20];

            if (toReturn.Any(r => r.Id.Equals(productIncome.Id))) {
                ProductIncome fromList = toReturn.First(r => r.Id.Equals(productIncome.Id));

                if (productIncomeItem == null || fromList.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncome;

                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.SaleNumber = saleNumber;

                order.Sale = sale;

                product.MeasureUnit = measureUnit;
                product.CurrentPrice = orderItem.PricePerItem;

                if (culture.Equals("pl")) {
                    product.Name = product.NameUA;
                    product.Description = product.DescriptionUA;
                } else {
                    product.Name = product.NameUA;
                    product.Description = product.DescriptionUA;
                }

                orderItem.Product = product;
                orderItem.Order = order;

                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (saleReturnItem != null) {
                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;
                    saleReturnItem.SaleReturn = saleReturn;
                }

                productIncomeItem.SaleReturnItem = saleReturnItem;

                fromList.ProductIncomeItems.Add(productIncomeItem);
            } else {
                if (productIncomeItem != null) {
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;
                    agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;

                    sale.ClientAgreement = clientAgreement;
                    sale.SaleNumber = saleNumber;

                    order.Sale = sale;

                    product.MeasureUnit = measureUnit;
                    product.CurrentPrice = orderItem.PricePerItem;

                    if (culture.Equals("pl")) {
                        product.Name = product.NameUA;
                        product.Description = product.DescriptionUA;
                    } else {
                        product.Name = product.NameUA;
                        product.Description = product.DescriptionUA;
                    }

                    orderItem.Product = product;
                    orderItem.Order = order;

                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    client.RegionCode = regionCode;

                    saleReturn.Client = client;
                    saleReturn.CreatedBy = returnCreatedBy;

                    if (saleReturnItem != null) {
                        saleReturnItem.OrderItem = orderItem;
                        saleReturnItem.CreatedBy = itemCreatedBy;
                        saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                        saleReturnItem.Storage = storage;
                        saleReturnItem.SaleReturn = saleReturn;
                    }

                    productIncomeItem.SaleReturnItem = saleReturnItem;

                    productIncome.Storage = storage;
                    productIncome.User = itemCreatedBy;

                    productIncome.ProductIncomeItems.Add(productIncomeItem);
                }

                toReturn.Add(productIncome);
            }

            return productIncome;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Culture = culture }
        );

        return toReturn;
    }

    public List<ProductIncome> GetAllFiltered(DateTime from, DateTime to, long limit, long offset, string value) {
        value = value.Trim();

        string[] concreteValues = value.Split(' ');

        dynamic props = new ExpandoObject();

        props.Limit = limit;
        props.Offset = offset;
        props.From = TimeZoneInfo.ConvertTimeToUtc(from);
        props.To = to;
        props.Value = value;
        props.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        for (int i = 0; i < concreteValues.Length; i++) (props as ExpandoObject).AddProperty($"Var{i}", concreteValues[i]);

        #region ProductIncomeFilteredQuery

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductIncome].ID, [ProductIncome].FromDate, COUNT(*) OVER() [TotalRowsQty] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [PackingListPackageOrderItem] ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [SupplyOrderUkraine] ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "LEFT JOIN [ClientAgreement] ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [ActReconciliationItem] ON [ActReconciliationItem].[ID] = [ProductIncomeItem].[ActReconciliationItemID] " +
            "LEFT JOIN [ActReconciliation] ON [ActReconciliation].[ID] = [ActReconciliationItem].[ActReconciliationID] " +
            "LEFT JOIN [SupplyOrderUkraine] [ActReconciliationSupplyOrderUkraine] ON [ActReconciliationSupplyOrderUkraine].[ID] = [ActReconciliation].[SupplyOrderUkraineID] " +
            "LEFT JOIN [SupplyInvoice] [ActReconciliationSupplyInvoice] ON [ActReconciliationSupplyInvoice].[ID] = [ActReconciliation].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] [ActReconciliationSupplyOrder] ON [ActReconciliationSupplyOrder].[ID] = [ActReconciliationSupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ProductCapitalizationItem] ON [ProductCapitalizationItem].[ID] = [ProductIncomeItem].[ProductCapitalizationItemID] " +
            "LEFT JOIN [ProductCapitalization] ON [ProductCapitalization].[ID] = [ProductCapitalizationItem].[ProductCapitalizationID] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ClientAgreement] [SaleReturnClientAgreement] " +
            "ON [SaleReturnClientAgreement].[ID] = [SaleReturn].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [SaleReturnClientAgreement].[AgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "OR [Client].[ID] = [SupplyOrder].[ClientID] " +
            "OR [Client].[ID] = [ClientAgreement].[ClientID] " +
            "OR [Client].[ID] = [ActReconciliationSupplyOrder].[ClientID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "OR [Organization].[ID] = [SupplyOrderUkraine].[OrganizationID] " +
            "OR [Organization].[ID] = [ActReconciliationSupplyOrderUkraine].[OrganizationID] " +
            "OR [Organization].[ID] = [ProductCapitalization].[OrganizationID] " +
            "OR [Organization].[ID] = [Agreement].[OrganizationID] " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "AND (([ProductIncome].FromDate <= @To " +
            "AND [ProductIncome].FromDate >= @From ) " +
            "OR ([SupplyInvoice].DateCustomDeclaration <= @To " +
            "AND [SupplyInvoice].DateCustomDeclaration >= @From )) ";

        for (int i = 0; i < concreteValues.Length; i++)
            sqlExpression +=
                "AND ( " +
                $"[Responsible].LastName like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[Storage].[Name] like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[ProductIncome].Number like '%' + @Var{i} + '%' " +
                "OR " +
                $"[Client].Name like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[Client].FullName like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[SupplyInvoice].[NumberCustomDeclaration] like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[Organization].[Name] like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[Organization].[FullName] like N'%' + @Var{i} + '%' " +
                "OR " +
                $"[SupplyInvoice].[Number] like N'%' + @Var{i} + '%' " +
                ") ";

        sqlExpression +=
            "GROUP BY [ProductIncome].ID, [ProductIncome].FromDate " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID, " +
            "ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber], " +
            "[Search_CTE].TotalRowsQty AS [TotalRowsQty] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT " +
            "[ProductIncome].* " +
            ", (SELECT TOP 1 TotalRowsQty FROM [Rowed_CTE]) AS [TotalRowsQty] " +
            ", [User].* " +
            ", [Storage].* " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [User] " +
            "ON [ProductIncome].UserID = [User].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "WHERE [ProductIncome].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [ProductIncome].FromDate DESC, [ProductIncome].[Number] DESC";

        List<ProductIncome> incomes =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                sqlExpression,
                (income, user, storage) => {
                    income.User = user;
                    income.Storage = storage;

                    return income;
                },
                (object)props
            ).ToList();

        if (!incomes.Any()) return incomes;

        #endregion

        #region SaleReturnItems

        string returnsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [SaleReturnItem].ID IS NOT NULL";

        Type[] returnsTypes = {
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage),
            typeof(VatRate)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> returnsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
            SaleReturn saleReturn = (SaleReturn)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            User returnCreatedBy = (User)objects[5];
            User itemCreatedBy = (User)objects[6];
            User moneyReturnedBy = (User)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            Product product = (Product)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            Order order = (Order)objects[11];
            Sale sale = (Sale)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Agreement agreement = (Agreement)objects[14];
            Organization organization = (Organization)objects[15];
            Currency currency = (Currency)objects[16];
            Pricing pricing = (Pricing)objects[17];
            SaleNumber saleNumber = (SaleNumber)objects[18];
            Storage storage = (Storage)objects[19];
            VatRate vatRate = (VatRate)objects[20];

            decimal vatRatePercent = Convert.ToDecimal(vatRate?.Value ?? 0) / 100;

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            income.Currency = currency;

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            agreement.Pricing = pricing;
            agreement.Organization = organization;
            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            order.Sale = sale;

            product.MeasureUnit = measureUnit;
            product.CurrentPrice = orderItem.PricePerItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            orderItem.Product = product;
            orderItem.Order = order;

            orderItem.Product.CurrentLocalPrice =
                decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount =
                decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

            orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += saleReturnItem.Qty;

            client.RegionCode = regionCode;

            saleReturn.CreatedBy = returnCreatedBy;
            saleReturn.Client = client;

            saleReturnItem.OrderItem = orderItem;
            saleReturnItem.CreatedBy = itemCreatedBy;
            saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
            saleReturnItem.Storage = storage;
            saleReturnItem.SaleReturn = saleReturn;

            saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

            if (sale.IsVatSale)
                saleReturnItem.VatAmount =
                    decimal.Round(
                        saleReturnItem.AmountLocal * (vatRatePercent / (vatRatePercent + 1)),
                        14,
                        MidpointRounding.AwayFromZero);

            income.TotalNetPrice += saleReturnItem.AmountLocal;

            income.TotalVatAmount += saleReturnItem.VatAmount;

            productIncomeItem.SaleReturnItem = saleReturnItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            returnsSqlExpression,
            returnsTypes,
            returnsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        #endregion

        #region PackingListPackageOrderItemsQuery

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyInvoice),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[6];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];
            Currency currency = (Currency)objects[16];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            income.Currency = currency;

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (supplyOrder != null) {
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            supplyInvoice.SupplyOrder = supplyOrder;
            packingList.SupplyInvoice = supplyInvoice;
            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty);

            income.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;

            income.TotalNetWithVat += packingListPackageOrderItem.TotalNetPrice + packingListPackageOrderItem.VatAmount;

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        #endregion

        #region SupplyOrderUkraineItemsQuery

        string ukraineOrdersSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [SupplyOrderUkraineItem].ID IS NOT NULL";

        Type[] ukraineOrdersTypes = {
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> ukraineOrdersMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            User responsible = (User)objects[5];
            Organization organization = (Organization)objects[6];
            Client supplier = (Client)objects[7];
            Currency currency = (Currency)objects[10];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            income.Currency = currency;

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Supplier = supplier;
            supplyOrderUkraine.Responsible = responsible;

            supplyOrderUkraineItem.Product = product;
            supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

            supplyOrderUkraineItem.TotalNetWeight = Math.Round(supplyOrderUkraineItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetWeight = Math.Round(supplyOrderUkraineItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetPrice =
                decimal.Round(supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetPriceLocal =
                decimal.Round(supplyOrderUkraineItem.UnitPriceLocal * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.GrossPrice =
                decimal.Round(supplyOrderUkraineItem.GrossUnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + supplyOrderUkraineItem.NetPriceLocal + supplyOrderUkraineItem.VatAmountLocal, 2, MidpointRounding.AwayFromZero);

            income.AccountingTotalNetPrice =
                decimal.Round(income.AccountingTotalNetPrice + supplyOrderUkraineItem.GrossPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + supplyOrderUkraineItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            ukraineOrdersSqlExpression,
            ukraineOrdersTypes,
            ukraineOrdersMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        #endregion

        #region ActReconciliationItemsQuery

        string reconciliationsSqlExpression =
            "SELECT " +
            "[ProductIncomeItem].* " +
            ", [ActReconciliationItem].* " +
            ", [Product].* " +
            ", [MeasureUnit].* " +
            ", [ActReconciliation].* " +
            ", [ActUser].* " +
            ", [SupplyInvoice].* " +
            ", [SupplyOrder].* " +
            ", [SupplyOrderOrganization].* " +
            ", [Client].* " +
            ", [Currency].* " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ActReconciliationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [User] AS [ActUser] " +
            "ON [ActReconciliation].ResponsibleID = [ActUser].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [ActReconciliation].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
            "AND [SupplyOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [ActReconciliationItem].ID IS NOT NULL ";

        Type[] reconciliationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ActReconciliationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ActReconciliation),
            typeof(User),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Organization),
            typeof(Client),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> reconciliationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ActReconciliation actReconciliation = (ActReconciliation)objects[4];
            User user = (User)objects[5];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[6];
            SupplyOrder supplyOrder = (SupplyOrder)objects[7];
            Organization supplyOrderOrganization = (Organization)objects[8];
            Client supplyOrderSupplier = (Client)objects[9];
            Currency currency = (Currency)objects[10];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            income.Currency = currency;

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyInvoice != null) {
                supplyOrder.Organization = supplyOrderOrganization;
                supplyOrder.Client = supplyOrderSupplier;

                supplyInvoice.SupplyOrder = supplyOrder;
            }

            actReconciliation.Responsible = user;
            actReconciliation.SupplyInvoice = supplyInvoice;

            actReconciliationItem.Product = product;
            actReconciliationItem.ActReconciliation = actReconciliation;

            actReconciliationItem.TotalNetWeight = Math.Round(actReconciliationItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);
            actReconciliationItem.NetWeight = Math.Round(actReconciliationItem.NetWeight, 3, MidpointRounding.AwayFromZero);
            actReconciliationItem.TotalAmount =
                decimal.Round(actReconciliationItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            productIncomeItem.ActReconciliationItem = actReconciliationItem;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + actReconciliationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + actReconciliationItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            reconciliationsSqlExpression,
            reconciliationsTypes,
            reconciliationsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        string reconciliationsOrderSqlQuery =
            "SELECT " +
            "[ProductIncome].* " +
            ", [SupplyOrderUkraine].* " +
            ", [User].* " +
            ", [Organization].* " +
            ", [Supplier].* " +
            ", [CurrencySupplyOrderUkraine].* " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] AS [ClientAgreementSupplyOrderUkraine] " +
            "ON [ClientAgreementSupplyOrderUkraine].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] AS [AgreementSupplyOrderUkraine] " +
            "ON [AgreementSupplyOrderUkraine].[ID] = [ClientAgreementSupplyOrderUkraine].[AgreementID] " +
            "LEFT JOIN [Currency] AS [CurrencySupplyOrderUkraine] " +
            "ON [CurrencySupplyOrderUkraine].[ID] = [AgreementSupplyOrderUkraine].[CurrencyID] " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliation].[SupplyOrderUkraineID] = [SupplyOrderUkraine].[ID] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ActReconciliationItem].[ActReconciliationID] = [ActReconciliation].[ID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON  [ProductIncomeItem].[ActReconciliationItemID] = [ActReconciliationItem].[ID] " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
            "WHERE [ProductIncome].[ID] IN @Ids ";

        Type[] reconciliationsOrderOrders = {
            typeof(ProductIncome),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(Client),
            typeof(Currency)
        };

        Func<object[], ProductIncome> reconciliationsOrderMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[1];
            User responsible = (User)objects[2];
            Organization organization = (Organization)objects[3];
            Client supplier = (Client)objects[4];
            Currency currencySupplyOrderUkraine = (Currency)objects[5];

            ProductIncome existProductIncome = incomes.First(x => x.Id.Equals(productIncome.Id));

            if (existProductIncome.Currency == null)
                existProductIncome.Currency = currencySupplyOrderUkraine;

            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Responsible = responsible;
            supplyOrderUkraine.Supplier = supplier;

            if (existProductIncome.ProductIncomeItems.Any(x =>
                    x.SupplyOrderUkraineItem.SupplyOrderUkraineId.Equals(supplyOrderUkraine.Id)))
                existProductIncome.ProductIncomeItems.First(x =>
                        x.SupplyOrderUkraineItem.SupplyOrderUkraineId.Equals(supplyOrderUkraine.Id))
                    .SupplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

            return productIncome;
        };

        _connection.Query(
            reconciliationsOrderSqlQuery,
            reconciliationsOrderOrders,
            reconciliationsOrderMapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = incomes.Select(x => x.Id)
            });

        #endregion

        Currency eur = _connection.Query<Currency>(
            "SELECT TOP 1 * FROM [Currency] " +
            "WHERE [Currency].[Deleted] = 0 " +
            "AND [Currency].[Code] = 'EUR' ").FirstOrDefault();

        #region CapitalizationItemsQuery

        string capitalizationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL";

        Type[] capitalizationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductCapitalization),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], ProductIncomeItem> capitalizationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[4];
            Organization organization = (Organization)objects[5];
            User responsible = (User)objects[6];
            Storage storage = (Storage)objects[7];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            income.Currency = eur;

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            productCapitalization.Organization = organization;
            productCapitalization.Responsible = responsible;
            productCapitalization.Storage = storage;

            productCapitalizationItem.Product = product;
            productCapitalizationItem.ProductCapitalization = productCapitalization;

            productCapitalizationItem.TotalAmount =
                decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);

            productCapitalizationItem.TotalNetWeight = Math.Round(productCapitalizationItem.Qty * productCapitalizationItem.Weight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + productCapitalizationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            income.TotalNetWeight =
                Math.Round(
                    income.TotalNetWeight + productCapitalizationItem.TotalNetWeight,
                    3,
                    MidpointRounding.AwayFromZero
                );

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            capitalizationsSqlExpression,
            capitalizationsTypes,
            capitalizationsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        #endregion

        return incomes;
    }

    public List<ProductIncome> GetAllFiltered(DateTime from, DateTime to) {
        dynamic props = new ExpandoObject();

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        props.From = TimeZoneInfo.ConvertTimeToUtc(from);
        props.To = to;
        props.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        #region ProductIncomeFilteredQuery

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductIncome].ID, [ProductIncome].FromDate, COUNT(*) OVER() [TotalRowsQty] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [PackingListPackageOrderItem] ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "AND (([ProductIncome].FromDate <= @To " +
            "AND [ProductIncome].FromDate >= @From ) " +
            "OR ([SupplyInvoice].DateCustomDeclaration <= @To " +
            "AND [SupplyInvoice].DateCustomDeclaration >= @From )) " +
            "AND [ProductIncome].ProductIncomeType = 1 " +
            "AND [ProductIncomeItem].PackingListPackageOrderItemID IS NOT NULL " +
            "GROUP BY [ProductIncome].ID, [ProductIncome].FromDate " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID, " +
            "ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber], " +
            "[Search_CTE].TotalRowsQty AS [TotalRowsQty] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT " +
            "[ProductIncome].* " +
            ", (SELECT TOP 1 TotalRowsQty FROM [Rowed_CTE]) AS [TotalRowsQty] " +
            ", [User].* " +
            ", [Storage].* " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [User] " +
            "ON [ProductIncome].UserID = [User].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "WHERE [ProductIncome].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            ") " +
            "ORDER BY [ProductIncome].FromDate DESC, [ProductIncome].[Number] DESC ";

        List<ProductIncome> incomes =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                sqlExpression,
                (income, user, storage) => {
                    income.User = user;
                    income.Storage = storage;

                    return income;
                },
                (object)props
            ).ToList();

        if (!incomes.Any()) return incomes;

        #endregion

        // #region SaleReturnItems
        // string returnsSqlExpression =
        //     "SELECT * " +
        //     "FROM [ProductIncomeItem] " +
        //     "LEFT JOIN [SaleReturnItem] " +
        //     "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
        //     "LEFT JOIN [SaleReturn] " +
        //     "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
        //     "LEFT JOIN [Client] " +
        //     "ON [Client].ID = [SaleReturn].ClientID " +
        //     "LEFT JOIN [RegionCode] " +
        //     "ON [RegionCode].ID = [Client].RegionCodeID " +
        //     "LEFT JOIN [User] AS [ReturnCreatedBy] " +
        //     "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
        //     "LEFT JOIN [User] AS [ItemCreatedBy] " +
        //     "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
        //     "LEFT JOIN [User] AS [MoneyReturnedBy] " +
        //     "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
        //     "LEFT JOIN [OrderItem] " +
        //     "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
        //     "LEFT JOIN [Product] " +
        //     "ON [Product].ID = [OrderItem].ProductID " +
        //     "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
        //     "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
        //     "AND [MeasureUnit].CultureCode = @Culture " +
        //     "LEFT JOIN [Order] " +
        //     "ON [Order].ID = [OrderItem].OrderID " +
        //     "LEFT JOIN [Sale] " +
        //     "ON [Sale].OrderID = [Order].ID " +
        //     "LEFT JOIN [ClientAgreement] " +
        //     "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
        //     "LEFT JOIN [Agreement] " +
        //     "ON [Agreement].ID = [ClientAgreement].AgreementID " +
        //     "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
        //     "ON [Organization].ID = [Agreement].OrganizationID " +
        //     "AND [Organization].CultureCode = @Culture " +
        //     "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
        //     "ON [Currency].ID = [Agreement].CurrencyID " +
        //     "AND [Currency].CultureCode = @Culture " +
        //     "LEFT JOIN [views].[PricingView] AS [Pricing] " +
        //     "ON [Pricing].ID = [Agreement].PricingID " +
        //     "AND [Pricing].CultureCode = @Culture " +
        //     "LEFT JOIN [SaleNumber] " +
        //     "ON [SaleNumber].ID = [Sale].SaleNumberID " +
        //     "LEFT JOIN [Storage] " +
        //     "ON [Storage].ID = [SaleReturnItem].StorageID " +
        //     "LEFT JOIN [VatRate] " +
        //     "ON [VatRate].[ID] = [Organization].[VatRateID] " +
        //     "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
        //     "AND [SaleReturnItem].ID IS NOT NULL";
        //
        // Type[] returnsTypes = {
        //     typeof(ProductIncomeItem),
        //     typeof(SaleReturnItem),
        //     typeof(SaleReturn),
        //     typeof(Client),
        //     typeof(RegionCode),
        //     typeof(User),
        //     typeof(User),
        //     typeof(User),
        //     typeof(OrderItem),
        //     typeof(Product),
        //     typeof(MeasureUnit),
        //     typeof(Order),
        //     typeof(Sale),
        //     typeof(ClientAgreement),
        //     typeof(Agreement),
        //     typeof(Organization),
        //     typeof(Currency),
        //     typeof(Pricing),
        //     typeof(SaleNumber),
        //     typeof(Storage),
        //     typeof(VatRate),
        // };
        //
        // string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        //
        // Func<object[], ProductIncomeItem> returnsMapper = objects => {
        //     ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
        //     SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
        //     SaleReturn saleReturn = (SaleReturn)objects[2];
        //     Client client = (Client)objects[3];
        //     RegionCode regionCode = (RegionCode)objects[4];
        //     User returnCreatedBy = (User)objects[5];
        //     User itemCreatedBy = (User)objects[6];
        //     User moneyReturnedBy = (User)objects[7];
        //     OrderItem orderItem = (OrderItem)objects[8];
        //     Product product = (Product)objects[9];
        //     MeasureUnit measureUnit = (MeasureUnit)objects[10];
        //     Order order = (Order)objects[11];
        //     Sale sale = (Sale)objects[12];
        //     ClientAgreement clientAgreement = (ClientAgreement)objects[13];
        //     Agreement agreement = (Agreement)objects[14];
        //     Organization organization = (Organization)objects[15];
        //     Currency currency = (Currency)objects[16];
        //     Pricing pricing = (Pricing)objects[17];
        //     SaleNumber saleNumber = (SaleNumber)objects[18];
        //     Storage storage = (Storage)objects[19];
        //     VatRate vatRate = (VatRate)objects[20];
        //
        //     decimal vatRatePercent = Convert.ToDecimal(vatRate?.Value ?? 0) / 100;
        //
        //     ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));
        //
        //     income.Currency = currency;
        //
        //     if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;
        //
        //     agreement.Pricing = pricing;
        //     agreement.Organization = organization;
        //     agreement.Currency = currency;
        //
        //     clientAgreement.Agreement = agreement;
        //
        //     sale.ClientAgreement = clientAgreement;
        //     sale.SaleNumber = saleNumber;
        //
        //     order.Sale = sale;
        //
        //     product.MeasureUnit = measureUnit;
        //     product.CurrentPrice = orderItem.PricePerItem;
        //
        //     if (culture.Equals("pl")) {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     } else {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     }
        //
        //     orderItem.Product = product;
        //     orderItem.Order = order;
        //
        //     orderItem.Product.CurrentLocalPrice =
        //         decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);
        //
        //     orderItem.TotalAmount =
        //         decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        //     orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);
        //
        //     orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
        //     orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
        //
        //     income.TotalQty += saleReturnItem.Qty;
        //
        //     client.RegionCode = regionCode;
        //
        //     saleReturn.CreatedBy = returnCreatedBy;
        //     saleReturn.Client = client;
        //
        //     saleReturnItem.OrderItem = orderItem;
        //     saleReturnItem.CreatedBy = itemCreatedBy;
        //     saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
        //     saleReturnItem.Storage = storage;
        //     saleReturnItem.SaleReturn = saleReturn;
        //
        //     saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);
        //
        //     if (sale.IsVatSale)
        //         saleReturnItem.VatAmount =
        //             decimal.Round(
        //                 saleReturnItem.AmountLocal * (vatRatePercent / (vatRatePercent + 1)),
        //                 14,
        //                 MidpointRounding.AwayFromZero);
        //
        //     income.TotalNetPrice += saleReturnItem.AmountLocal;
        //
        //     income.TotalVatAmount += saleReturnItem.VatAmount;
        //
        //     productIncomeItem.SaleReturnItem = saleReturnItem;
        //
        //     income.ProductIncomeItems.Add(productIncomeItem);
        //
        //     return productIncomeItem;
        // };
        //
        // _connection.Query(
        //     returnsSqlExpression,
        //     returnsTypes,
        //     returnsMapper,
        //     new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        // );
        // #endregion

        #region PackingListPackageOrderItemsQuery

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [Pricing] [BasePricing] " +
            "ON [BasePricing].ID = [ProviderPricing].BasePricingID " +
            "LEFT JOIN [VatRate] " +
            "ON VatRate.ID = Organization.VatRateID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyInvoice),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(VatRate),
            typeof(ProductProductGroup),
            typeof(ProductGroup)
        };

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[6];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];
            ClientAgreement clientAgreement = (ClientAgreement)objects[14];
            Agreement agreement = (Agreement)objects[15];
            Currency currency = (Currency)objects[16];
            ProviderPricing pricing = (ProviderPricing)objects[17];
            Pricing basePricing = (Pricing)objects[18];
            VatRate vatRate = (VatRate)objects[19];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[20];
            ProductGroup productGroup = (ProductGroup)objects[21];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            income.Currency = currency;

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (supplyOrder != null) {
                supplyOrder.Client = client;

                organization.VatRate = vatRate;

                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                if (clientAgreement != null) {
                    if (pricing != null)
                        pricing.Pricing = basePricing;

                    agreement.ProviderPricing = pricing;
                    agreement.Currency = currency;
                    clientAgreement.Agreement = agreement;
                    supplyOrder.ClientAgreement = clientAgreement;
                }
            }

            if (productProductGroup != null && productGroup != null) {
                productProductGroup.ProductGroup = productGroup;
                product.ProductProductGroups.Add(productProductGroup);
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            supplyInvoice.SupplyOrder = supplyOrder;
            packingList.SupplyInvoice = supplyInvoice;
            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);
            packingListPackageOrderItem.TotalGrossWeight = Math.Round(packingListPackageOrderItem.GrossWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
            packingListPackageOrderItem.GrossWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty);

            income.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;

            income.TotalNetWithVat += packingListPackageOrderItem.TotalNetPrice + packingListPackageOrderItem.VatAmount;

            income.TotalVatAmount += packingListPackageOrderItem.VatAmount;

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
            income.TotalGrossWeight =
                Math.Round(income.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        #endregion

        // #region SupplyOrderUkraineItemsQuery
        //
        // string ukraineOrdersSqlExpression =
        //     "SELECT * " +
        //     "FROM [ProductIncomeItem] " +
        //     "LEFT JOIN [SupplyOrderUkraineItem] " +
        //     "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
        //     "LEFT JOIN [SupplyOrderUkraine] " +
        //     "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
        //     "LEFT JOIN [Product] " +
        //     "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
        //     "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
        //     "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
        //     "AND [MeasureUnit].CultureCode = @Culture " +
        //     "LEFT JOIN [User] " +
        //     "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
        //     "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
        //     "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
        //     "AND [Organization].CultureCode = @Culture " +
        //     "LEFT JOIN [Client] AS [Supplier] " +
        //     "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
        //     "LEFT JOIN [ClientAgreement] " +
        //     "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
        //     "LEFT JOIN [Agreement] " +
        //     "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
        //     "LEFT JOIN [Currency] " +
        //     "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
        //     "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
        //     "AND [SupplyOrderUkraineItem].ID IS NOT NULL";
        //
        // Type[] ukraineOrdersTypes = {
        //     typeof(ProductIncomeItem),
        //     typeof(SupplyOrderUkraineItem),
        //     typeof(SupplyOrderUkraine),
        //     typeof(Product),
        //     typeof(MeasureUnit),
        //     typeof(User),
        //     typeof(Organization),
        //     typeof(Client),
        //     typeof(ClientAgreement),
        //     typeof(Agreement),
        //     typeof(Currency)
        // };
        //
        // Func<object[], ProductIncomeItem> ukraineOrdersMapper = objects => {
        //     ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
        //     SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
        //     SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[2];
        //     Product product = (Product)objects[3];
        //     MeasureUnit measureUnit = (MeasureUnit)objects[4];
        //     User responsible = (User)objects[5];
        //     Organization organization = (Organization)objects[6];
        //     Client supplier = (Client)objects[7];
        //     Currency currency = (Currency)objects[10];
        //
        //     ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));
        //
        //     income.Currency = currency;
        //
        //     if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;
        //
        //     if (culture.Equals("pl")) {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     } else {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     }
        //
        //     product.MeasureUnit = measureUnit;
        //
        //     supplyOrderUkraine.Organization = organization;
        //     supplyOrderUkraine.Supplier = supplier;
        //     supplyOrderUkraine.Responsible = responsible;
        //
        //     supplyOrderUkraineItem.Product = product;
        //     supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;
        //
        //     supplyOrderUkraineItem.TotalNetWeight = Math.Round(supplyOrderUkraineItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);
        //
        //     supplyOrderUkraineItem.NetWeight = Math.Round(supplyOrderUkraineItem.NetWeight, 3, MidpointRounding.AwayFromZero);
        //
        //     supplyOrderUkraineItem.NetPrice =
        //         decimal.Round(supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);
        //
        //     supplyOrderUkraineItem.NetPriceLocal =
        //         decimal.Round(supplyOrderUkraineItem.UnitPriceLocal * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);
        //
        //     supplyOrderUkraineItem.GrossPrice =
        //         decimal.Round(supplyOrderUkraineItem.GrossUnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);
        //
        //     income.TotalNetPrice =
        //         decimal.Round(income.TotalNetPrice + supplyOrderUkraineItem.NetPriceLocal + supplyOrderUkraineItem.VatAmountLocal, 2, MidpointRounding.AwayFromZero);
        //
        //     income.AccountingTotalNetPrice =
        //         decimal.Round(income.AccountingTotalNetPrice + supplyOrderUkraineItem.GrossPrice, 2, MidpointRounding.AwayFromZero);
        //
        //     income.TotalQty += productIncomeItem.Qty;
        //
        //     income.TotalNetWeight =
        //         Math.Round(income.TotalNetWeight + supplyOrderUkraineItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
        //
        //     productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;
        //
        //     income.ProductIncomeItems.Add(productIncomeItem);
        //
        //     return productIncomeItem;
        // };
        //
        // _connection.Query(
        //     ukraineOrdersSqlExpression,
        //     ukraineOrdersTypes,
        //     ukraineOrdersMapper,
        //     new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        // );
        // #endregion
        //
        // #region ActReconciliationItemsQuery
        // string reconciliationsSqlExpression =
        //     "SELECT " +
        //     "[ProductIncomeItem].* " +
        //     ", [ActReconciliationItem].* " +
        //     ", [Product].* " +
        //     ", [MeasureUnit].* " +
        //     ", [ActReconciliation].* " +
        //     ", [ActUser].* " +
        //     ", [SupplyInvoice].* " +
        //     ", [SupplyOrder].* " +
        //     ", [SupplyOrderOrganization].* " +
        //     ", [Client].* " +
        //     ", [Currency].* " +
        //     "FROM [ProductIncomeItem] " +
        //     "LEFT JOIN [ActReconciliationItem] " +
        //     "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
        //     "LEFT JOIN [Product] " +
        //     "ON [Product].ID = [ActReconciliationItem].ProductID " +
        //     "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
        //     "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
        //     "AND [MeasureUnit].CultureCode = @Culture " +
        //     "LEFT JOIN [ActReconciliation] " +
        //     "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
        //     "LEFT JOIN [User] AS [ActUser] " +
        //     "ON [ActReconciliation].ResponsibleID = [ActUser].ID " +
        //     "LEFT JOIN [SupplyInvoice] " +
        //     "ON [ActReconciliation].SupplyInvoiceID = [SupplyInvoice].ID " +
        //     "LEFT JOIN [SupplyOrder] " +
        //     "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
        //     "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
        //     "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
        //     "AND [SupplyOrderOrganization].CultureCode = @Culture " +
        //     "LEFT JOIN [Client] " +
        //     "ON [Client].ID = [SupplyOrder].ClientID " +
        //     "LEFT JOIN [ClientAgreement] " +
        //     "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
        //     "LEFT JOIN [Agreement] " +
        //     "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
        //     "LEFT JOIN [Currency] " +
        //     "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
        //     "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
        //     "AND [ActReconciliationItem].ID IS NOT NULL ";
        //
        // Type[] reconciliationsTypes = {
        //     typeof(ProductIncomeItem),
        //     typeof(ActReconciliationItem),
        //     typeof(Product),
        //     typeof(MeasureUnit),
        //     typeof(ActReconciliation),
        //     typeof(User),
        //     typeof(SupplyInvoice),
        //     typeof(SupplyOrder),
        //     typeof(Organization),
        //     typeof(Client),
        //     typeof(Currency)
        // };
        //
        // Func<object[], ProductIncomeItem> reconciliationsMapper = objects => {
        //     ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
        //     ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[1];
        //     Product product = (Product)objects[2];
        //     MeasureUnit measureUnit = (MeasureUnit)objects[3];
        //     ActReconciliation actReconciliation = (ActReconciliation)objects[4];
        //     User user = (User)objects[5];
        //     SupplyInvoice supplyInvoice = (SupplyInvoice)objects[6];
        //     SupplyOrder supplyOrder = (SupplyOrder)objects[7];
        //     Organization supplyOrderOrganization = (Organization)objects[8];
        //     Client supplyOrderSupplier = (Client)objects[9];
        //     Currency currency = (Currency)objects[10];
        //
        //     ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));
        //
        //     income.Currency = currency;
        //
        //     if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;
        //
        //     if (culture.Equals("pl")) {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     } else {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     }
        //
        //     product.MeasureUnit = measureUnit;
        //
        //     if (supplyInvoice != null) {
        //         supplyOrder.Organization = supplyOrderOrganization;
        //         supplyOrder.Client = supplyOrderSupplier;
        //
        //         supplyInvoice.SupplyOrder = supplyOrder;
        //     }
        //
        //     actReconciliation.Responsible = user;
        //     actReconciliation.SupplyInvoice = supplyInvoice;
        //
        //     actReconciliationItem.Product = product;
        //     actReconciliationItem.ActReconciliation = actReconciliation;
        //
        //     actReconciliationItem.TotalNetWeight = Math.Round(actReconciliationItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);
        //     actReconciliationItem.NetWeight = Math.Round(actReconciliationItem.NetWeight, 3, MidpointRounding.AwayFromZero);
        //     actReconciliationItem.TotalAmount =
        //         decimal.Round(actReconciliationItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);
        //
        //     productIncomeItem.ActReconciliationItem = actReconciliationItem;
        //
        //     income.TotalNetPrice =
        //         decimal.Round(income.TotalNetPrice + actReconciliationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
        //
        //     income.TotalQty += productIncomeItem.Qty;
        //
        //     income.TotalNetWeight =
        //         Math.Round(income.TotalNetWeight + actReconciliationItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
        //
        //     income.ProductIncomeItems.Add(productIncomeItem);
        //
        //     return productIncomeItem;
        // };
        //
        // _connection.Query(
        //     reconciliationsSqlExpression,
        //     reconciliationsTypes,
        //     reconciliationsMapper,
        //     new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        // );
        //
        // string reconciliationsOrderSqlQuery =
        //     "SELECT " +
        //     "[ProductIncome].* " +
        //     ", [SupplyOrderUkraine].* " +
        //     ", [User].* " +
        //     ", [Organization].* " +
        //     ", [Supplier].* " +
        //     ", [CurrencySupplyOrderUkraine].* " +
        //     "FROM [SupplyOrderUkraine] " +
        //     "LEFT JOIN [User] " +
        //     "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
        //     "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
        //     "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
        //     "AND [Organization].CultureCode = @Culture " +
        //     "LEFT JOIN [Client] AS [Supplier] " +
        //     "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
        //     "LEFT JOIN [ClientAgreement] AS [ClientAgreementSupplyOrderUkraine] " +
        //     "ON [ClientAgreementSupplyOrderUkraine].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
        //     "LEFT JOIN [Agreement] AS [AgreementSupplyOrderUkraine] " +
        //     "ON [AgreementSupplyOrderUkraine].[ID] = [ClientAgreementSupplyOrderUkraine].[AgreementID] " +
        //     "LEFT JOIN [Currency] AS [CurrencySupplyOrderUkraine] " +
        //     "ON [CurrencySupplyOrderUkraine].[ID] = [AgreementSupplyOrderUkraine].[CurrencyID] " +
        //     "LEFT JOIN [ActReconciliation] " +
        //     "ON [ActReconciliation].[SupplyOrderUkraineID] = [SupplyOrderUkraine].[ID] " +
        //     "LEFT JOIN [ActReconciliationItem] " +
        //     "ON [ActReconciliationItem].[ActReconciliationID] = [ActReconciliation].[ID] " +
        //     "LEFT JOIN [ProductIncomeItem] " +
        //     "ON  [ProductIncomeItem].[ActReconciliationItemID] = [ActReconciliationItem].[ID] " +
        //     "LEFT JOIN [ProductIncome] " +
        //     "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
        //     "WHERE [ProductIncome].[ID] IN @Ids ";
        //
        // Type[] reconciliationsOrderOrders = {
        //     typeof(ProductIncome),
        //     typeof(SupplyOrderUkraine),
        //     typeof(User),
        //     typeof(Organization),
        //     typeof(Client),
        //     typeof(Currency)
        // };
        //
        // Func<object[], ProductIncome> reconciliationsOrderMapper = objects => {
        //     ProductIncome productIncome = (ProductIncome)objects[0];
        //     SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[1];
        //     User responsible = (User)objects[2];
        //     Organization organization = (Organization)objects[3];
        //     Client supplier = (Client)objects[4];
        //     Currency currencySupplyOrderUkraine = (Currency)objects[5];
        //
        //     ProductIncome existProductIncome = incomes.First(x => x.Id.Equals(productIncome.Id));
        //
        //     if (existProductIncome.Currency == null)
        //         existProductIncome.Currency = currencySupplyOrderUkraine;
        //
        //     supplyOrderUkraine.Organization = organization;
        //     supplyOrderUkraine.Responsible = responsible;
        //     supplyOrderUkraine.Supplier = supplier;
        //
        //     if (existProductIncome.ProductIncomeItems.Any(x =>
        //             x.SupplyOrderUkraineItem.SupplyOrderUkraineId.Equals(supplyOrderUkraine.Id)))
        //         existProductIncome.ProductIncomeItems.First(x =>
        //                 x.SupplyOrderUkraineItem.SupplyOrderUkraineId.Equals(supplyOrderUkraine.Id))
        //             .SupplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;
        //
        //     return productIncome;
        // };
        //
        // _connection.Query(
        //     reconciliationsOrderSqlQuery,
        //     reconciliationsOrderOrders,
        //     reconciliationsOrderMapper,
        //     new {
        //         Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
        //         Ids = incomes.Select(x => x.Id)
        //     });
        // #endregion
        //
        // Currency eur = _connection.Query<Currency>(
        //     "SELECT TOP 1 * FROM [Currency] " +
        //     "WHERE [Currency].[Deleted] = 0 " +
        //     "AND [Currency].[Code] = 'EUR' ").FirstOrDefault();
        //
        // #region CapitalizationItemsQuery
        // string capitalizationsSqlExpression =
        //     "SELECT * " +
        //     "FROM [ProductIncomeItem] " +
        //     "LEFT JOIN [ProductCapitalizationItem] " +
        //     "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
        //     "LEFT JOIN [Product] " +
        //     "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
        //     "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
        //     "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
        //     "AND [MeasureUnit].CultureCode = @Culture " +
        //     "LEFT JOIN [ProductCapitalization] " +
        //     "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
        //     "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
        //     "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
        //     "AND [Organization].CultureCode = @Culture " +
        //     "LEFT JOIN [User] AS [Responsible] " +
        //     "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
        //     "LEFT JOIN [Storage] " +
        //     "ON [Storage].ID = [ProductCapitalization].StorageID " +
        //     "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
        //     "AND [ProductCapitalizationItem].ID IS NOT NULL";
        //
        // Type[] capitalizationsTypes = {
        //     typeof(ProductIncomeItem),
        //     typeof(ProductCapitalizationItem),
        //     typeof(Product),
        //     typeof(MeasureUnit),
        //     typeof(ProductCapitalization),
        //     typeof(Organization),
        //     typeof(User),
        //     typeof(Storage)
        // };
        //
        // Func<object[], ProductIncomeItem> capitalizationsMapper = objects => {
        //     ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
        //     ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[1];
        //     Product product = (Product)objects[2];
        //     MeasureUnit measureUnit = (MeasureUnit)objects[3];
        //     ProductCapitalization productCapitalization = (ProductCapitalization)objects[4];
        //     Organization organization = (Organization)objects[5];
        //     User responsible = (User)objects[6];
        //     Storage storage = (Storage)objects[7];
        //
        //     ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));
        //
        //     income.Currency = eur;
        //
        //     if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;
        //
        //     if (culture.Equals("pl")) {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     } else {
        //         product.Name = product.NameUA;
        //         product.Description = product.DescriptionUA;
        //     }
        //
        //     product.MeasureUnit = measureUnit;
        //
        //     productCapitalization.Organization = organization;
        //     productCapitalization.Responsible = responsible;
        //     productCapitalization.Storage = storage;
        //
        //     productCapitalizationItem.Product = product;
        //     productCapitalizationItem.ProductCapitalization = productCapitalization;
        //
        //     productCapitalizationItem.TotalAmount =
        //         decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);
        //
        //     productCapitalizationItem.TotalNetWeight = Math.Round(productCapitalizationItem.Qty * productCapitalizationItem.Weight, 3, MidpointRounding.AwayFromZero);
        //
        //     productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;
        //
        //     income.TotalQty += productIncomeItem.Qty;
        //
        //     income.TotalNetPrice =
        //         decimal.Round(income.TotalNetPrice + productCapitalizationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
        //
        //     income.TotalNetWeight =
        //         Math.Round(
        //             income.TotalNetWeight + productCapitalizationItem.TotalNetWeight,
        //             3,
        //             MidpointRounding.AwayFromZero
        //         );
        //
        //     income.ProductIncomeItems.Add(productIncomeItem);
        //
        //     return productIncomeItem;
        // };
        //
        // _connection.Query(
        //     capitalizationsSqlExpression,
        //     capitalizationsTypes,
        //     capitalizationsMapper,
        //     new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        // );
        //
        // #endregion

        return incomes;
    }

    public List<ProductIncome> GetAllByProductNetId(Guid netId) {
        List<ProductIncome> incomes =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                ";WITH [Search_CTE] " +
                "AS ( " +
                "SELECT [ProductIncome].ID " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [SaleReturnItem] " +
                "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
                "LEFT JOIN [ActReconciliationItem] " +
                "ON [ActReconciliationItem].ID = [ProductIncomeItem].ActReconciliationItemID " +
                "LEFT JOIN [ProductCapitalizationItem] " +
                "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
                "LEFT JOIN [Product] " +
                "ON ( " +
                "([SupplyOrderUkraineItem].ID IS NOT NULL AND [SupplyOrderUkraineItem].ProductID = [Product].ID) " +
                "OR " +
                "([SupplyInvoiceOrderItem].ID IS NOT NULL AND [SupplyInvoiceOrderItem].ProductID = [Product].ID) " +
                "OR " +
                "([OrderItem].ID IS NOT NULL AND [OrderItem].ProductID = [Product].ID) " +
                "OR " +
                "([ActReconciliationItem].ID IS NOT NULL AND [ActReconciliationItem].ProductID = [Product].ID) " +
                "OR " +
                "([ProductCapitalizationItem].ID IS NOT NULL AND [ProductCapitalizationItem].ProductID = [Product].ID) " +
                ") " +
                "WHERE [Product].NetUID = @NetId " +
                "GROUP BY [ProductIncome].ID " +
                ") " +
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [ProductIncome].UserID = [User].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "WHERE [ProductIncome].ID IN ( " +
                "SELECT [Search_CTE].ID " +
                "FROM [Search_CTE] " +
                ") " +
                "ORDER BY [ProductIncome].FromDate DESC, [ProductIncome].Number DESC",
                (income, user, storage) => {
                    income.User = user;
                    income.Storage = storage;

                    return income;
                },
                new { NetId = netId }
            ).ToList();

        if (!incomes.Any()) return incomes;

        string returnsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [SaleReturnItem].ID IS NOT NULL";

        Type[] returnsTypes = {
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage)
        };

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        Func<object[], ProductIncomeItem> returnsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[1];
            SaleReturn saleReturn = (SaleReturn)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            User returnCreatedBy = (User)objects[5];
            User itemCreatedBy = (User)objects[6];
            User moneyReturnedBy = (User)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            Product product = (Product)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            Order order = (Order)objects[11];
            Sale sale = (Sale)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Agreement agreement = (Agreement)objects[14];
            Organization organization = (Organization)objects[15];
            Currency currency = (Currency)objects[16];
            Pricing pricing = (Pricing)objects[17];
            SaleNumber saleNumber = (SaleNumber)objects[18];
            Storage storage = (Storage)objects[19];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            agreement.Pricing = pricing;
            agreement.Organization = organization;
            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            order.Sale = sale;

            product.MeasureUnit = measureUnit;
            product.CurrentPrice = orderItem.PricePerItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            orderItem.Product = product;
            orderItem.Order = order;

            orderItem.Product.CurrentLocalPrice =
                decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount =
                decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

            orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += orderItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            client.RegionCode = regionCode;

            saleReturn.CreatedBy = returnCreatedBy;
            saleReturn.Client = client;

            saleReturnItem.OrderItem = orderItem;
            saleReturnItem.CreatedBy = itemCreatedBy;
            saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
            saleReturnItem.Storage = storage;
            saleReturnItem.SaleReturn = saleReturn;

            productIncomeItem.SaleReturnItem = saleReturnItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            returnsSqlExpression,
            returnsTypes,
            returnsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        string packListsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL";

        Type[] packListsTypes = {
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(Organization)
        };

        Func<object[], ProductIncomeItem> packListsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackage package = (PackingListPackage)objects[3];
            PackingList packagePackingList = (PackingList)objects[4];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrder supplyOrder = (SupplyOrder)objects[10];
            Client client = (Client)objects[11];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[12];
            Organization organization = (Organization)objects[13];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (package != null) package.PackingList = packagePackingList;

            if (supplyOrder != null) {
                supplyOrder.Client = client;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            }

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            if (supplyOrderItem != null) {
                supplyOrderItem.Product = product;
                supplyOrderItem.SupplyOrder = supplyOrder;
            }

            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;

            packingListPackageOrderItem.PackingList = packingList;
            packingListPackageOrderItem.PackingListPackage = package;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = Math.Round(packingListPackageOrderItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            packingListPackageOrderItem.TotalNetPrice =
                decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            packListsSqlExpression,
            packListsTypes,
            packListsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        string ukraineOrdersSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [SupplyOrderUkraineItem].ID IS NOT NULL";

        Type[] ukraineOrdersTypes = {
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(Organization)
        };

        Func<object[], ProductIncomeItem> ukraineOrdersMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            User responsible = (User)objects[5];
            Organization organization = (Organization)objects[6];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Responsible = responsible;

            supplyOrderUkraineItem.Product = product;
            supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

            supplyOrderUkraineItem.TotalNetWeight = Math.Round(supplyOrderUkraineItem.NetWeight * productIncomeItem.Qty, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetWeight = Math.Round(supplyOrderUkraineItem.NetWeight, 3, MidpointRounding.AwayFromZero);

            supplyOrderUkraineItem.NetPrice =
                decimal.Round(supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + supplyOrderUkraineItem.NetPrice, 2, MidpointRounding.AwayFromZero);

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetWeight =
                Math.Round(income.TotalNetWeight + supplyOrderUkraineItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            ukraineOrdersSqlExpression,
            ukraineOrdersTypes,
            ukraineOrdersMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        string reconciliationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ActReconciliationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [User] AS [ActUser] " +
            "ON [ActReconciliation].ResponsibleID = [ActUser].ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [ActReconciliation].SupplyOrderUkraineID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [ActReconciliationItem].ID IS NOT NULL";

        Type[] reconciliationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ActReconciliationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ActReconciliation),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization)
        };

        Func<object[], ProductIncomeItem> reconciliationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ActReconciliation actReconciliation = (ActReconciliation)objects[4];
            User user = (User)objects[5];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
            User responsible = (User)objects[7];
            Organization organization = (Organization)objects[8];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Responsible = responsible;

            actReconciliation.Responsible = user;
            actReconciliation.SupplyOrderUkraine = supplyOrderUkraine;

            actReconciliationItem.Product = product;
            actReconciliationItem.ActReconciliation = actReconciliation;

            productIncomeItem.ActReconciliationItem = actReconciliationItem;

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            reconciliationsSqlExpression,
            reconciliationsTypes,
            reconciliationsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        string capitalizationsSqlExpression =
            "SELECT * " +
            "FROM [ProductIncomeItem] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductIncomeItem].ProductIncomeID IN @Ids " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL";

        Type[] capitalizationsTypes = {
            typeof(ProductIncomeItem),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductCapitalization),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], ProductIncomeItem> capitalizationsMapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[4];
            Organization organization = (Organization)objects[5];
            User responsible = (User)objects[6];
            Storage storage = (Storage)objects[7];

            ProductIncome income = incomes.First(i => i.Id.Equals(productIncomeItem.ProductIncomeId));

            if (income.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncomeItem;

            if (culture.Equals("pl")) {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            } else {
                product.Name = product.NameUA;
                product.Description = product.DescriptionUA;
            }

            product.MeasureUnit = measureUnit;

            productCapitalization.Organization = organization;
            productCapitalization.Responsible = responsible;
            productCapitalization.Storage = storage;

            productCapitalizationItem.Product = product;
            productCapitalizationItem.ProductCapitalization = productCapitalization;

            productCapitalizationItem.TotalAmount =
                decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);

            productCapitalizationItem.TotalNetWeight = Math.Round(productCapitalizationItem.Qty * productCapitalizationItem.Weight, 3, MidpointRounding.AwayFromZero);

            productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;

            income.TotalQty += productIncomeItem.Qty;

            income.TotalNetPrice =
                decimal.Round(income.TotalNetPrice + productCapitalizationItem.TotalAmount, 2, MidpointRounding.AwayFromZero);

            income.TotalNetWeight =
                Math.Round(
                    income.TotalNetWeight + productCapitalizationItem.TotalNetWeight,
                    3,
                    MidpointRounding.AwayFromZero
                );

            income.ProductIncomeItems.Add(productIncomeItem);

            return productIncomeItem;
        };

        _connection.Query(
            capitalizationsSqlExpression,
            capitalizationsTypes,
            capitalizationsMapper,
            new { Culture = culture, Ids = incomes.Select(i => i.Id) }
        );

        return incomes;
    }

    public ProductIncome GetLastByCulture(string culture) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [ProductIncome] ";

        sqlExpression +=
            culture.ToLower().Equals("pl")
                ? "WHERE [ProductIncome].Number like 'P%' "
                : "WHERE [ProductIncome].Number NOT like 'P%' ";

        sqlExpression += "ORDER BY [ProductIncome].ID DESC";

        return _connection.Query<ProductIncome>(
            sqlExpression
        ).SingleOrDefault();
    }

    public ProductIncome GetLastByTypeAndPrefix(ProductIncomeType incomeType, string prefix) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [ProductIncome] " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "AND [ProductIncome].ProductIncomeType = @IncomeType ";

        if (string.IsNullOrEmpty(prefix))
            sqlExpression += "AND [ProductIncome].[Number] like N'0%' ";
        else
            sqlExpression += $"AND [ProductIncome].[Number] like N'{prefix}%' ";

        sqlExpression += "ORDER BY [ProductIncome].ID DESC";

        return _connection.Query<ProductIncome>(
            sqlExpression,
            new { IncomeType = incomeType }
        ).SingleOrDefault();
    }

    public ProductIncome GetSupplyOrderUkraineProductIncomeByNetId(Guid netId) {
        ProductIncome toReturn =
            _connection.Query<ProductIncome, User, Storage, ProductIncome>(
                "SELECT * FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [User].[ID] = [ProductIncome].[UserID] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].[ID]=  [ProductIncome].[StorageID] " +
                "WHERE [ProductIncome].[NetUID] = @NetId",
                (productIncome, user, storage) => {
                    productIncome.User = user;
                    productIncome.Storage = storage;
                    return productIncome;
                }, new { NetId = netId }).FirstOrDefault();

        if (toReturn == null) return null;

        Type[] types = {
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(ProductSpecification),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> mapper = objects => {
            ProductIncomeItem incomeItem = (ProductIncomeItem)objects[0];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[1];
            ProductSpecification productSpecification = (ProductSpecification)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
            User user = (User)objects[6];
            Organization organization = (Organization)objects[7];
            Client client = (Client)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];

            if (toReturn.ProductIncomeItems.Any(x => x.Id.Equals(incomeItem.Id))) return incomeItem;

            clientAgreement.Agreement = agreement;
            agreement.Currency = currency;
            supplyOrderUkraine.ClientAgreement = clientAgreement;
            supplyOrderUkraine.Supplier = client;
            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.Responsible = user;

            supplyOrderUkraineItem.GrossPriceLocal = Convert.ToDecimal(supplyOrderUkraineItem.Qty) * supplyOrderUkraineItem.GrossUnitPriceLocal;
            supplyOrderUkraineItem.NetPriceLocal = Convert.ToDecimal(supplyOrderUkraineItem.Qty) * supplyOrderUkraineItem.UnitPriceLocal;
            supplyOrderUkraineItem.TotalNetWeight = supplyOrderUkraineItem.Qty * supplyOrderUkraineItem.NetWeight;
            supplyOrderUkraineItem.TotalGrossWeight = supplyOrderUkraineItem.Qty * supplyOrderUkraineItem.GrossWeight;

            toReturn.TotalQty += supplyOrderUkraineItem.Qty;
            toReturn.TotalNetPrice += supplyOrderUkraineItem.NetPriceLocal;
            toReturn.TotalNetWeight += supplyOrderUkraineItem.TotalNetWeight;
            toReturn.TotalGrossWeight += supplyOrderUkraineItem.TotalGrossWeight;
            toReturn.TotalGrossPrice += supplyOrderUkraineItem.GrossPriceLocal;

            supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;
            supplyOrderUkraineItem.ProductSpecification = productSpecification;
            product.MeasureUnit = measureUnit;
            supplyOrderUkraineItem.Product = product;

            incomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

            toReturn.ProductIncomeItems.Add(incomeItem);

            return incomeItem;
        };

        _connection.Query(
            "SELECT * FROM [ProductIncomeItem] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [SupplyOrderUkraineItem].[ProductSpecificationID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyOrderUkraineItem].[ProductID] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [SupplyOrderUkraine].[ResponsibleID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrderUkraine].[OrganizationID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrderUkraine].[SupplierID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncomeItem].[ProductIncomeID] = @Id " +
            "AND [ProductIncomeItem].[Deleted] = 0 ",
            types, mapper,
            new { toReturn.Id });

        ProductIncomeItem firstEl = toReturn.ProductIncomeItems.FirstOrDefault();

        if (firstEl != null) {
            Currency uah =
                _connection.Query<Currency>(
                    "SELECT TOP 1 * FROM [Currency] " +
                    "WHERE [Currency].[Code] = 'uah' " +
                    "AND [Currency].[Deleted] = 0; ").FirstOrDefault();

            if (uah == null) {
                toReturn.ExchangeRateToUah = 1;
            } else {
                GovExchangeRate exchangeRateToUah =
                    _connection.Query<GovExchangeRate>(
                        "SELECT TOP(1) " +
                        "[GovExchangeRate].ID, " +
                        "(CASE " +
                        "WHEN [GovExchangeRateHistory].Amount IS NOT NULL " +
                        "THEN [GovExchangeRateHistory].Amount " +
                        "ELSE [GovExchangeRate].Amount " +
                        "END) AS [Amount] " +
                        "FROM [GovExchangeRate] " +
                        "LEFT JOIN [GovExchangeRateHistory] " +
                        "ON [GovExchangeRateHistory].GovExchangeRateID = [GovExchangeRate].ID " +
                        "AND [GovExchangeRateHistory].Created <= @FromDate " +
                        "WHERE [GovExchangeRate].[CurrencyID] = @ToId " +
                        "AND [GovExchangeRate].[Code] = @FromCode " +
                        "ORDER BY [GovExchangeRateHistory].Created DESC",
                        new {
                            ToId = uah.Id,
                            FromCode = firstEl.SupplyOrderUkraineItem.SupplyOrderUkraine.ClientAgreement.Agreement.Currency.Code,
                            toReturn.FromDate
                        }).FirstOrDefault();

                toReturn.ExchangeRateToUah = exchangeRateToUah?.Amount ?? 1;
            }
        } else {
            toReturn.ExchangeRateToUah = 1;
        }

        return toReturn;
    }

    public ProductIncome GetSupplyOrderProductIncomeByNetId(Guid netId) {
        ProductIncome toReturn = null;

        Type[] productIncomeTypes = {
            typeof(ProductIncome),
            typeof(Storage),
            typeof(Organization),
            typeof(User),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncome> productIncomeMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            Storage storage = (Storage)objects[1];
            Organization organization = (Organization)objects[2];
            User user = (User)objects[3];
            PackingList packingList = (PackingList)objects[4];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[5];
            SupplyOrder supplyOrder = (SupplyOrder)objects[6];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[7];
            Client supplyOrderClient = (Client)objects[8];
            Organization supplyOrderOrganization = (Organization)objects[9];
            ClientAgreement clientAgreement = (ClientAgreement)objects[10];
            Client client = (Client)objects[11];
            Agreement agreement = (Agreement)objects[12];
            Currency currency = (Currency)objects[13];

            productIncome.Storage = storage;
            productIncome.User = user;
            productIncome.Organization = organization;
            productIncome.PackingList = packingList;
            packingList.SupplyInvoice = supplyInvoice;
            supplyInvoice.SupplyOrder = supplyOrder;
            supplyOrder.Organization = supplyOrderOrganization;
            supplyOrder.ClientAgreement = clientAgreement;
            supplyOrder.Client = supplyOrderClient;
            supplyOrder.SupplyOrderNumber = supplyOrderNumber;
            clientAgreement.Client = client;
            clientAgreement.Agreement = agreement;
            agreement.Currency = currency;

            if (toReturn == null)
                toReturn = productIncome;

            return productIncome;
        };

        _connection.Query(
            ";WITH [PACKING_LIST_CTE] AS ( " +
            "SELECT TOP(1) [PackingList].[ID] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ProductIncomeID] = [ProductIncome].[ID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "WHERE [ProductIncome].[NetUID] = @NetId " +
            ") " +
            "SELECT * FROM [ProductIncome] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductIncome].[StorageID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Storage].[OrganizationID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [ProductIncome].[UserID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = ( " +
            "SELECT [PACKING_LIST_CTE].[ID] " +
            "FROM [PACKING_LIST_CTE] " +
            ") " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
            "LEFT JOIN [Client] AS [SupplyOrderClient] " +
            "ON [SupplyOrderClient].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [Organization] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].[ID] = [SupplyOrder].[OrganizationID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncome].[NetUID] = @NetId; ",
            productIncomeTypes, productIncomeMapper,
            new { NetId = netId });

        if (toReturn == null) return null;

        Type[] types = {
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(ProductSpecification),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], ProductIncomeItem> mapper = objects => {
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[0];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[1];
            ProductSpecification productSpecification = (ProductSpecification)objects[2];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[3];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[4];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];

            if (packingListPackageOrderItem != null) packingListPackageOrderItem.VatAmount = decimal.Round(packingListPackageOrderItem.VatAmount, 3, MidpointRounding.AwayFromZero);

            if (!toReturn.ProductIncomeItems.Any(x => x.Id.Equals(productIncomeItem.Id))) {
                packingListPackageOrderItem.TotalNetWeight =
                    packingListPackageOrderItem.NetWeight * productIncomeItem.Qty;
                packingListPackageOrderItem.TotalGrossWeight =
                    packingListPackageOrderItem.GrossWeight * productIncomeItem.Qty;
                packingListPackageOrderItem.TotalNetWithVat = packingListPackageOrderItem.TotalNetPrice + packingListPackageOrderItem.VatAmount;

                packingListPackageOrderItem.TotalNetPrice =
                    decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 3, MidpointRounding.AwayFromZero);

                decimal totalPriceWithVat = decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 3, MidpointRounding.AwayFromZero) +
                                            packingListPackageOrderItem.VatAmount;

                toReturn.PackingList.TotalQuantity += productIncomeItem.Qty;
                toReturn.PackingList.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;
                toReturn.PackingList.TotalNetPriceWithVat += totalPriceWithVat;
                toReturn.PackingList.TotalVatAmount += packingListPackageOrderItem.VatAmount;
                toReturn.PackingList.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;
                toReturn.PackingList.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;

                toReturn.ProductIncomeItems.Add(productIncomeItem);
            } else {
                productIncomeItem = toReturn.ProductIncomeItems.First(x => x.Id.Equals(productIncomeItem.Id));
            }

            if (!productIncomeItem.ConsignmentItems.Any(x => x.Id.Equals(consignmentItem.Id)) && consignmentItem != null)
                productIncomeItem.ConsignmentItems.Add(consignmentItem);

            product.MeasureUnit = measureUnit;
            if (supplyOrderItem != null)
                supplyOrderItem.Product = product;
            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            supplyInvoiceOrderItem.Product = product;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

            if (consignmentItem != null)
                consignmentItem.ProductSpecification = productSpecification;

            return productIncomeItem;
        };

        _connection.Query(
            "SELECT * FROM [ProductIncomeItem] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "AND [ConsignmentItem].[Deleted] = 0 " +
            "LEFT JOIn [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[ID] = [PackingListPackageOrderItem].[SupplyInvoiceOrderItemID] " +
            "LEFt JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFt JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "WHERE [ProductIncomeItem].[ProductIncomeID] = @Id " +
            "AND [ProductIncomeItem].[Deleted] = 0; ",
            types, mapper,
            new { toReturn.Id });

        return toReturn;
    }
}