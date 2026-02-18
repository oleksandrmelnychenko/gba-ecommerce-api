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
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.Repositories.SaleReturns.Contracts;

namespace GBA.Domain.Repositories.SaleReturns;

public sealed class SaleReturnRepository : ISaleReturnRepository {
    private readonly IDbConnection _connection;

    public SaleReturnRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleReturn saleReturn) {
        return _connection.Query<long>(
            "INSERT INTO [SaleReturn] (FromDate, ClientID, CreatedById, UpdatedById, Number, Updated, [ClientAgreementID]) " +
            "VALUES (@FromDate, @ClientId, @CreatedById, @UpdatedById, @Number, GETUTCDATE(), @ClientAgreementId); " +
            "SELECT SCOPE_IDENTITY()",
            saleReturn
        ).Single();
    }

    public void Update(SaleReturn saleReturn) {
        _connection.Execute(
            "UPDATE [SaleReturn] " +
            "SET FromDate = @FromDate, ClientID = @ClientID, UpdatedById = @UpdatedById, Updated = GETUTCDATE(), [ClientAgreementID] = @ClientAgreementId " +
            "WHERE ID = @Id",
            saleReturn
        );
    }

    public void SetCanceled(SaleReturn saleReturn) {
        _connection.Execute(
            "UPDATE [SaleReturn] " +
            "SET CanceledById = @CanceledById, IsCanceled = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            saleReturn
        );
    }

    public SaleReturn GetLastReturnByCulture() {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [SaleReturn] ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl"))
            sqlExpression += "WHERE [Number] like 'P%' ";
        else
            sqlExpression += "WHERE [Number] not like 'P%' ";

        sqlExpression += "ORDER BY ID DESC";

        return _connection.Query<SaleReturn>(
            sqlExpression
        ).SingleOrDefault();
    }

    public SaleReturn GetLastReturnByPrefix(string prefix) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [SaleReturn] ";

        if (string.IsNullOrEmpty(prefix))
            sqlExpression += "WHERE [Number] like N'0%' ";
        else
            sqlExpression += $"WHERE [Number] like N'{prefix}%' ";

        sqlExpression += "ORDER BY ID DESC";

        return _connection.Query<SaleReturn>(
            sqlExpression
        ).SingleOrDefault();
    }

    public SaleReturn GetById(long id) {
        SaleReturn toReturn = null;

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(SaleReturnItem),
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

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            Client client = (Client)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            User returnCreatedBy = (User)objects[3];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[4];
            User itemCreatedBy = (User)objects[5];
            User moneyReturnedBy = (User)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            Order order = (Order)objects[10];
            Sale sale = (Sale)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Agreement agreement = (Agreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];
            Pricing pricing = (Pricing)objects[16];
            SaleNumber saleNumber = (SaleNumber)objects[17];
            Storage storage = (Storage)objects[18];

            if (toReturn != null) {
                if (saleReturnItem != null && !toReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) {
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

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    toReturn.TotalAmount =
                        decimal.Round(
                            toReturn.SaleReturnItems.Sum(i => decimal.Round(i.Amount * i.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero))
                            , 2
                            , MidpointRounding.AwayFromZero
                        );

                    toReturn.SaleReturnItems.Add(saleReturnItem);
                }
            } else {
                if (saleReturnItem != null) {
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

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    saleReturn.TotalAmount = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                    saleReturn.SaleReturnItems.Add(saleReturnItem);
                }

                client.RegionCode = regionCode;

                saleReturn.Client = client;
                saleReturn.CreatedBy = returnCreatedBy;

//                    saleReturn.Created = TimeZoneInfo.ConvertTimeFromUtc(saleReturn.Created, currentTimeZone);
//                    saleReturn.Updated = TimeZoneInfo.ConvertTimeFromUtc(saleReturn.Updated, currentTimeZone);
//                    saleReturn.FromDate = TimeZoneInfo.ConvertTimeFromUtc(saleReturn.FromDate, currentTimeZone);

                toReturn = saleReturn;
            }

            return saleReturn;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
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
            "WHERE [SaleReturn].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = culture }
        );

        return toReturn;
    }

    public SaleReturn GetByNetId(Guid netId) {
        SaleReturn toReturn = null;

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(SaleReturnItem),
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
            typeof(User)
        };

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            Client client = (Client)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            User returnCreatedBy = (User)objects[3];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[4];
            User itemCreatedBy = (User)objects[5];
            User moneyReturnedBy = (User)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            Order order = (Order)objects[10];
            Sale sale = (Sale)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Agreement agreement = (Agreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];
            Pricing pricing = (Pricing)objects[16];
            SaleNumber saleNumber = (SaleNumber)objects[17];
            Storage storage = (Storage)objects[18];
            User canceledBy = (User)objects[19];

            if (toReturn != null) {
                if (saleReturnItem == null || toReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) return saleReturn;

                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;
                toReturn.Currency = currency;
                toReturn.Storage = storage;

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

                saleReturnItem.OrderItem = orderItem;
                saleReturnItem.CreatedBy = itemCreatedBy;
                saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                saleReturnItem.Storage = storage;

                toReturn.TotalAmount =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => decimal.Round(i.Amount * i.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero))
                        , 2
                        , MidpointRounding.AwayFromZero
                    );

                toReturn.SaleReturnItems.Add(saleReturnItem);
            } else {
                if (saleReturnItem != null) {
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;
                    agreement.Currency = currency;

                    saleReturn.Currency = currency;
                    saleReturn.Storage = storage;

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

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    saleReturn.TotalAmount = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                    saleReturn.SaleReturnItems.Add(saleReturnItem);
                }

                client.RegionCode = regionCode;

                saleReturn.Client = client;
                saleReturn.CreatedBy = returnCreatedBy;
                saleReturn.CanceledBy = canceledBy;

                toReturn = saleReturn;
            }

            return saleReturn;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
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
            "LEFT JOIN [User] AS [CanceledBy] " +
            "ON [CanceledBy].ID = [SaleReturn].CanceledByID " +
            "WHERE [SaleReturn].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = culture }
        );

        return toReturn;
    }

    public SaleReturn GetByNetIdForPrinting(Guid netId) {
        SaleReturn toReturn = null;

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(SaleReturnItem),
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
            typeof(User),
            typeof(SaleInvoiceDocument),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(ConsignmentItem),
            typeof(VatRate)
        };

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            Client client = (Client)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            User returnCreatedBy = (User)objects[3];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[4];
            User itemCreatedBy = (User)objects[5];
            User moneyReturnedBy = (User)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            Order order = (Order)objects[10];
            Sale sale = (Sale)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Agreement agreement = (Agreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];
            Pricing pricing = (Pricing)objects[16];
            SaleNumber saleNumber = (SaleNumber)objects[17];
            Storage storage = (Storage)objects[18];
            User canceledBy = (User)objects[19];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[20];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[21];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[22];
            ConsignmentItem rootConsignmentItem = (ConsignmentItem)objects[23];
            VatRate organizationVatRate = (VatRate)objects[24];

            decimal vatRate = Convert.ToDecimal(organizationVatRate?.Value ?? 0) / 100;

            if (toReturn == null) {
                client.RegionCode = regionCode;

                saleReturn.Client = client;
                saleReturn.CreatedBy = returnCreatedBy;
                saleReturn.CanceledBy = canceledBy;
                saleReturn.Storage = storage;

                toReturn = saleReturn;
            }

            toReturn.Currency = currency;
            toReturn.ClientAgreement = clientAgreement;
            if (saleReturnItem == null) return saleReturn;

            if (!toReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) {
                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;
                clientAgreement.Agreement = agreement;

                sale.SaleInvoiceDocument = saleInvoiceDocument;
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
                    decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice =
                    decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                saleReturnItem.OrderItem = orderItem;
                saleReturnItem.CreatedBy = itemCreatedBy;
                saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                saleReturnItem.ProductIncomeItem = productIncomeItem;
                saleReturnItem.Storage = storage;

                saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                saleReturnItem.Amount = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);
                if (sale.IsVatSale) {
                    saleReturnItem.VatAmount =
                        decimal.Round(
                            saleReturnItem.Amount * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                    saleReturnItem.VatAmountLocal =
                        decimal.Round(
                            saleReturnItem.AmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);
                }

                toReturn.TotalAmount += saleReturnItem.Amount;

                toReturn.SaleReturnItems.Add(saleReturnItem);

                toReturn.Sale = sale;
            } else {
                saleReturnItem = toReturn.SaleReturnItems.First(i => i.Id.Equals(saleReturnItem.Id));
            }

            if (consignmentItem == null || saleReturnItem.ProductIncomeItem == null) return saleReturn;

            consignmentItem.RootConsignmentItem = rootConsignmentItem;

            saleReturnItem.ProductIncomeItem.ConsignmentItems.Add(consignmentItem);

            return saleReturn;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
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
            "LEFT JOIN [User] AS [CanceledBy] " +
            "ON [CanceledBy].ID = [SaleReturn].CanceledByID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [Sale].SaleInvoiceDocumentID = [SaleInvoiceDocument].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SaleReturnItemID = [SaleReturnItem].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ProductIncomeItemID = [ProductIncomeItem].ID " +
            "AND [ConsignmentItem].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
            "ON [RootConsignmentItem].ID = [ConsignmentItem].RootConsignmentItemID " +
            "LEFT JOIN [VatRate] " +
            "ON [Organization].[VatRateID] = [VatRate].[ID] " +
            "WHERE [SaleReturn].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = culture }
        );
        foreach (SaleReturnItem saleReturnItem in toReturn.SaleReturnItems)
            _connection.Query<SaleReturnItemProductPlacement, ProductPlacement, SaleReturnItemProductPlacement>(
                "SELECT * " +
                "FROM [SaleReturnItemProductPlacement] " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [SaleReturnItemProductPlacement].ProductPlacementID " +
                "WHERE [SaleReturnItemProductPlacement].SaleReturnItemId = @SaleReturnItemId " +
                "AND [SaleReturnItemProductPlacement].Deleted = 0 ",
                (saleReturnItemProductPlacement, productPlacement) => {
                    saleReturnItemProductPlacement.ProductPlacement = productPlacement;
                    saleReturnItem.SaleReturnItemProductPlacements.Add(saleReturnItemProductPlacement);
                    return saleReturnItemProductPlacement;
                },
                new {
                    SaleReturnItemId = saleReturnItem.Id
                }
            );

        if (toReturn?.Sale?.ClientAgreement?.Agreement?.Organization == null || !toReturn.Sale.IsVatSale) return toReturn;

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, PaymentRegister>(
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].Deleted = 0 " +
            "WHERE [PaymentRegister].OrganizationID = @OrganizationId " +
            "AND [PaymentRegister].IsActive = 1 " +
            "AND [PaymentRegister].Deleted = 0 " +
            "AND [PaymentRegister].[Type] = 2 " +
            "AND [PaymentCurrencyRegister].CurrencyID = @CurrencyId",
            (paymentRegister, currencyRegister) => {
                paymentRegister.PaymentCurrencyRegisters.Add(currencyRegister);

                toReturn.Sale.ClientAgreement.Agreement.Organization.PaymentRegisters.Add(paymentRegister);

                return paymentRegister;
            },
            new {
                OrganizationId = toReturn.Sale.ClientAgreement.Agreement.Organization.Id,
                CurrencyId = toReturn.Sale.ClientAgreement.Agreement.CurrencyId ?? 0
            }
        );


        return toReturn;
    }

    public List<SaleReturn> GetAll() {
        List<SaleReturn> toReturns = new();

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(SaleReturnItem),
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

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            Client client = (Client)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            User returnCreatedBy = (User)objects[3];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[4];
            User itemCreatedBy = (User)objects[5];
            User moneyReturnedBy = (User)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];
            Order order = (Order)objects[10];
            Sale sale = (Sale)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Agreement agreement = (Agreement)objects[13];
            Organization organization = (Organization)objects[14];
            Currency currency = (Currency)objects[15];
            Pricing pricing = (Pricing)objects[16];
            SaleNumber saleNumber = (SaleNumber)objects[17];
            Storage storage = (Storage)objects[18];

            if (toReturns.Any(r => r.Id.Equals(saleReturn.Id))) {
                SaleReturn toReturn = toReturns.First(r => r.Id.Equals(saleReturn.Id));

                if (saleReturnItem != null && !toReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) {
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

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    toReturn.SaleReturnItems.Add(saleReturnItem);
                }
            } else {
                if (saleReturnItem != null) {
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

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    saleReturn.SaleReturnItems.Add(saleReturnItem);
                }

                client.RegionCode = regionCode;

                saleReturn.Client = client;
                saleReturn.CreatedBy = returnCreatedBy;

//                    saleReturn.Created = TimeZoneInfo.ConvertTimeFromUtc(saleReturn.Created, currentTimeZone);
//                    saleReturn.Updated = TimeZoneInfo.ConvertTimeFromUtc(saleReturn.Updated, currentTimeZone);
//                    saleReturn.FromDate = TimeZoneInfo.ConvertTimeFromUtc(saleReturn.FromDate, currentTimeZone);

                toReturns.Add(saleReturn);
            }

            return saleReturn;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
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
            "WHERE [SaleReturn].Deleted = 0 " +
            "ORDER BY [SaleReturn].FromDate DESC",
            types,
            mapper,
            new { Culture = culture }
        );

        return toReturns;
    }

    public List<SaleReturn> GetAllFiltered(DateTime from, DateTime to) {
        List<SaleReturn> toReturns = new();

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(SaleReturnItem),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage),
            typeof(User),
            typeof(VatRate)
        };

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Organization organization = (Organization)objects[3];
            Currency currency = (Currency)objects[4];
            Client client = (Client)objects[5];
            RegionCode regionCode = (RegionCode)objects[6];
            User returnCreatedBy = (User)objects[7];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[8];
            User itemCreatedBy = (User)objects[9];
            User moneyReturnedBy = (User)objects[10];
            OrderItem orderItem = (OrderItem)objects[11];
            Product product = (Product)objects[12];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[13];
            ProductGroup productGroup = (ProductGroup)objects[14];
            MeasureUnit measureUnit = (MeasureUnit)objects[15];
            Order order = (Order)objects[16];
            Sale sale = (Sale)objects[17];
            Pricing pricing = (Pricing)objects[18];
            SaleNumber saleNumber = (SaleNumber)objects[19];
            Storage storage = (Storage)objects[20];
            User canceledBy = (User)objects[21];
            VatRate organizationVatRate = (VatRate)objects[22];

            decimal vatRate = Convert.ToDecimal(organizationVatRate?.Value ?? 0) / 100;

            if (toReturns.Any(r => r.Id.Equals(saleReturn.Id))) {
                SaleReturn toReturn = toReturns.First(r => r.Id.Equals(saleReturn.Id));

                if (saleReturnItem == null || toReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) return saleReturn;

                organization.VatRate = organizationVatRate;

                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                toReturn.ClientAgreement = clientAgreement;

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

                if (productProductGroup != null && productGroup != null) {
                    productProductGroup.ProductGroup = productGroup;
                    product.ProductProductGroups.Add(productProductGroup);
                }

                orderItem.Product = product;
                orderItem.Order = order;

                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                saleReturnItem.OrderItem = orderItem;
                saleReturnItem.CreatedBy = itemCreatedBy;
                saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                saleReturnItem.Storage = storage;

                saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale) {
                    saleReturnItem.VatAmount =
                        decimal.Round(
                            saleReturnItem.Amount * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                    saleReturnItem.VatAmountLocal =
                        decimal.Round(
                            saleReturnItem.AmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);
                }

                toReturn.SaleReturnItems.Add(saleReturnItem);

                toReturn.TotalAmount =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => i.Amount),
                        2,
                        MidpointRounding.AwayFromZero
                    );

                toReturn.TotalAmountLocal =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => i.AmountLocal),
                        2,
                        MidpointRounding.AwayFromZero
                    );

                toReturn.TotalVatAmount =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => i.VatAmount),
                        2,
                        MidpointRounding.AwayFromZero
                    );

                toReturn.TotalVatAmountLocal =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => i.VatAmountLocal),
                        2,
                        MidpointRounding.AwayFromZero
                    );
            } else {
                if (saleReturnItem != null) {
                    organization.VatRate = organizationVatRate;
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;
                    agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;

                    saleReturn.ClientAgreement = clientAgreement;

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

                    if (productProductGroup != null && productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        product.ProductProductGroups.Add(productProductGroup);
                    }

                    orderItem.Product = product;
                    orderItem.Order = order;

                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 2,
                        MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                    if (sale.IsVatSale) {
                        saleReturnItem.VatAmount =
                            decimal.Round(
                                saleReturnItem.Amount * (vatRate / (vatRate + 1)),
                                14,
                                MidpointRounding.AwayFromZero);

                        saleReturnItem.VatAmountLocal =
                            decimal.Round(
                                saleReturnItem.AmountLocal * (vatRate / (vatRate + 1)),
                                14,
                                MidpointRounding.AwayFromZero);
                    }


                    saleReturn.SaleReturnItems.Add(saleReturnItem);

                    saleReturn.TotalAmount = saleReturnItem.Amount;

                    saleReturn.TotalAmountLocal = saleReturnItem.AmountLocal;

                    saleReturn.Currency = currency;
                }

                client.RegionCode = regionCode;

                saleReturn.Client = client;
                saleReturn.CreatedBy = returnCreatedBy;
                saleReturn.CanceledBy = canceledBy;
                saleReturn.Storage = storage;

                toReturns.Add(saleReturn);
            }

            return saleReturn;
        };

        dynamic props = new ExpandoObject();

        props.From = from;
        props.To = to;
        props.Culture = culture;

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [SaleReturn].ID " +
            ", [SaleReturn].FromDate " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To ";

        sqlExpression +=
            "GROUP BY [SaleReturn].ID, [SaleReturn].FromDate " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER (ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SaleReturn].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [User] AS [MoneyReturnedBy] " +
            "ON [MoneyReturnedBy].ID = [SaleReturnItem].MoneyReturnedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "LEFT JOIN [User] AS [CanceledBy] " +
            "ON [CanceledBy].ID = [SaleReturn].CanceledByID " +
            "LEFT JOIN [VatRate] " +
            "ON [Organization].[VatRateID] = [VatRate].[ID] " +
            "WHERE [SaleReturn].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            // "WHERE [Rowed_CTE].RowNumber > @Offset " +
            // "AND [Rowed_CTE].RowNumber <= @Offset + @Limit " +
            ") " +
            "ORDER BY [SaleReturn].FromDate DESC, [SaleReturn].[Number] DESC";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            (object)props,
            commandTimeout: 120
        );

        return toReturns;
    }

    public List<SaleReturn> GetAllFiltered(DateTime from, DateTime to, long limit, long offset, string value) {
        List<SaleReturn> toReturns = new();

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(User),
            typeof(SaleReturnItem),
            typeof(User),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Order),
            typeof(Sale),
            typeof(Pricing),
            typeof(SaleNumber),
            typeof(Storage),
            typeof(User),
            typeof(VatRate)
        };

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Organization organization = (Organization)objects[3];
            Currency currency = (Currency)objects[4];
            Client client = (Client)objects[5];
            RegionCode regionCode = (RegionCode)objects[6];
            User returnCreatedBy = (User)objects[7];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[8];
            User itemCreatedBy = (User)objects[9];
            User moneyReturnedBy = (User)objects[10];
            OrderItem orderItem = (OrderItem)objects[11];
            Product product = (Product)objects[12];
            MeasureUnit measureUnit = (MeasureUnit)objects[13];
            Order order = (Order)objects[14];
            Sale sale = (Sale)objects[15];
            Pricing pricing = (Pricing)objects[16];
            SaleNumber saleNumber = (SaleNumber)objects[17];
            Storage storage = (Storage)objects[18];
            User canceledBy = (User)objects[19];
            VatRate organizationVatRate = (VatRate)objects[20];

            decimal vatRate = Convert.ToDecimal(organizationVatRate?.Value ?? 0) / 100;

            if (toReturns.Any(r => r.Id.Equals(saleReturn.Id))) {
                SaleReturn toReturn = toReturns.First(r => r.Id.Equals(saleReturn.Id));

                if (saleReturnItem == null || toReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) return saleReturn;

                organization.VatRate = organizationVatRate;

                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                toReturn.ClientAgreement = clientAgreement;

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
                    decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                saleReturnItem.OrderItem = orderItem;
                saleReturnItem.CreatedBy = itemCreatedBy;
                saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                saleReturnItem.Storage = storage;

                saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale) {
                    saleReturnItem.VatAmount =
                        decimal.Round(
                            saleReturnItem.Amount * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                    saleReturnItem.VatAmountLocal =
                        decimal.Round(
                            saleReturnItem.AmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);
                }

                toReturn.SaleReturnItems.Add(saleReturnItem);

                toReturn.TotalAmount =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => i.Amount),
                        2,
                        MidpointRounding.AwayFromZero
                    );

                toReturn.TotalAmountLocal =
                    decimal.Round(
                        toReturn.SaleReturnItems.Sum(i => i.AmountLocal),
                        2,
                        MidpointRounding.AwayFromZero
                    );
            } else {
                if (saleReturnItem != null) {
                    organization.VatRate = organizationVatRate;
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;
                    agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;

                    saleReturn.ClientAgreement = clientAgreement;

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
                        decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 2,
                        MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    saleReturnItem.OrderItem = orderItem;
                    saleReturnItem.CreatedBy = itemCreatedBy;
                    saleReturnItem.MoneyReturnedBy = moneyReturnedBy;
                    saleReturnItem.Storage = storage;

                    saleReturnItem.AmountLocal = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                    if (sale.IsVatSale) {
                        saleReturnItem.VatAmount =
                            decimal.Round(
                                saleReturnItem.Amount * (vatRate / (vatRate + 1)),
                                14,
                                MidpointRounding.AwayFromZero);

                        saleReturnItem.VatAmountLocal =
                            decimal.Round(
                                saleReturnItem.AmountLocal * (vatRate / (vatRate + 1)),
                                14,
                                MidpointRounding.AwayFromZero);
                    }


                    saleReturn.SaleReturnItems.Add(saleReturnItem);

                    saleReturn.TotalAmount = saleReturnItem.Amount;

                    saleReturn.TotalAmountLocal = saleReturnItem.AmountLocal;

                    saleReturn.Currency = currency;
                }

                client.RegionCode = regionCode;

                saleReturn.Client = client;
                saleReturn.CreatedBy = returnCreatedBy;
                saleReturn.CanceledBy = canceledBy;
                saleReturn.Storage = storage;

                toReturns.Add(saleReturn);
            }

            return saleReturn;
        };

        dynamic props = new ExpandoObject();

        props.Limit = limit;
        props.Offset = offset;
        props.From = from;
        props.To = to;
        props.Culture = culture;

        string[] concreteValues = value.Trim().Split(' ');

        for (int i = 0; i < concreteValues.Length; i++) (props as ExpandoObject).AddProperty($"Var{i}", concreteValues[i]);

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [SaleReturn].ID " +
            ", [SaleReturn].FromDate " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To ";

        for (int i = 0; i < concreteValues.Length; i++)
            sqlExpression +=
                "AND ( " +
                $"[Client].FullName like '%' + @Var{i} + '%' " +
                "OR " +
                $"[RegionCode].[Value] like '%' + @Var{i} + '%' " +
                "OR " +
                $"[ReturnCreatedBy].LastName like '%' + @Var{i} + '%' " +
                "OR " +
                $"[ItemCreatedBy].LastName like '%' + @Var{i} + '%' " +
                "OR " +
                $"[Product].VendorCode like '%' + @Var{i} + '%' " +
                "OR " +
                $"[Product].MainOriginalNumber like '%' + @Var{i} + '%' " +
                "OR " +
                $"[Storage].[Name] like '%' + @Var{i} + '%' " +
                ") ";

        sqlExpression +=
            "GROUP BY [SaleReturn].ID, [SaleReturn].FromDate " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER (ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SaleReturn].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
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
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "LEFT JOIN [User] AS [CanceledBy] " +
            "ON [CanceledBy].ID = [SaleReturn].CanceledByID " +
            "LEFT JOIN [VatRate] " +
            "ON [Organization].[VatRateID] = [VatRate].[ID] " +
            "WHERE [SaleReturn].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Offset + @Limit " +
            ") " +
            "ORDER BY [SaleReturn].FromDate DESC, [SaleReturn].[Number] DESC";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            (object)props
        );

        return toReturns;
    }

    public List<Client> GetFilteredDetailReportByClient(DateTime from, DateTime to, bool forMyClient, long userId, Guid? clientNetId, List<SaleReturnItemStatusName> reasons) {
        List<Client> toReturn = new();

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        Type[] types = {
            typeof(SaleReturn),
            typeof(Client),
            typeof(RegionCode),
            typeof(SaleReturnItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(Order),
            typeof(ClientUserProfile)
        };

        Func<object[], SaleReturn> mapper = objects => {
            SaleReturn saleReturn = (SaleReturn)objects[0];
            Client client = (Client)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[3];
            OrderItem orderItem = (OrderItem)objects[4];
            Product product = (Product)objects[5];
            Order order = (Order)objects[6];

            if (client != null) {
                if (toReturn.Any(i => i.Id.Equals(client.Id))) {
                    client = toReturn.FirstOrDefault(i => i.Id.Equals(client.Id));
                } else {
                    if (regionCode != null)
                        client.RegionCode = regionCode;

                    toReturn.Add(client);
                }

                if (client != null)
                    if (client.SaleReturns.Any(i => i.Id.Equals(saleReturn.Id)))
                        saleReturn = client.SaleReturns.FirstOrDefault(i => i.Id.Equals(saleReturn.Id));
                    else
                        client.SaleReturns.Add(saleReturn);
            }

            if (saleReturn != null)
                if (saleReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id)))
                    saleReturnItem = saleReturn.SaleReturnItems.FirstOrDefault(i => i.Id.Equals(saleReturnItem.Id));
                else
                    saleReturn.SaleReturnItems.Add(saleReturnItem);

            if (saleReturnItem != null && reasons.Any(x => x.SaleReturnItemStatus == saleReturnItem.SaleReturnItemStatus))
                saleReturnItem.StatusName = reasons.First(x => x.SaleReturnItemStatus == saleReturnItem.SaleReturnItemStatus).NameUK;

            product.Name = product.NameUA;
            product.Description = product.DescriptionUA;

            orderItem.Product = product;
            orderItem.Order = order;

            if (saleReturnItem == null) return saleReturn;

            saleReturnItem.OrderItem = orderItem;

            saleReturn?.SaleReturnItems.Add(saleReturnItem);

            return saleReturn;
        };

        string sqlQuery =
            "SELECT * " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SaleReturn].[ClientID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].[ClientID] = [Client].[ID] " +
            "WHERE [SaleReturn].[Deleted] = 0 AND " +
            "[SaleReturn].[FromDate] >= @From AND " +
            "[SaleReturn].[FromDate] <= @To ";

        object param = new { From = from, To = to };

        if (clientNetId != null) {
            sqlQuery += "AND [Client].[NetUID] = @ClientNetId ";
            param = new { From = from, To = to, ClientNetId = clientNetId };
        }

        if (forMyClient && userId != 0) {
            sqlQuery += "AND [ClientUserProfile].[UserProfileID] = @UserId ";

            param = new { From = from, To = to, UserId = userId };

            if (clientNetId != null)
                param = new { From = from, To = to, ClientNetId = clientNetId, UserId = userId };
        }

        _connection.Query(
            sqlQuery,
            types,
            mapper,
            param
        );

        return toReturn;
    }

    public List<SaleReturn> GetFilteredGroupedByReasonReport(
        DateTime from,
        DateTime to,
        bool forMyClient,
        long userId,
        Guid? clientNetId) {
        List<SaleReturn> toReturn = new();

        string sqlQuery =
            "SELECT " +
            "CONVERT(DATE, [SaleReturn].[FromDate]) AS DateFrom " +
            ", SUM([SaleReturnItem].[Qty]) AS Qty " +
            ", [SaleReturnItem].[SaleReturnItemStatus] " +
            "FROM [SaleReturn]  " +
            "LEFT JOIN [Client]  " +
            "ON [Client].[ID] = [SaleReturn].[ClientID]  " +
            "LEFT JOIN [SaleReturnItem]  " +
            "ON [SaleReturnItem].[SaleReturnID] = [SaleReturn].[ID]  " +
            "AND [SaleReturnItem].Deleted = 0  " +
            "LEFT JOIN [ClientUserProfile]  " +
            "ON [ClientUserProfile].[ClientID] = [Client].[ID]  " +
            "WHERE [SaleReturn].[Deleted] = 0 AND  " +
            "[SaleReturn].[FromDate] >= @From AND " +
            "[SaleReturn].[FromDate] <= @To ";

        object param = new { From = from, To = to };

        if (clientNetId != null) {
            sqlQuery += "AND [Client].[NetUID] = @ClientNetId ";
            param = new { From = from, To = to, ClientNetId = clientNetId };
        }

        if (forMyClient && userId != 0) {
            sqlQuery += "AND [ClientUserProfile].[UserProfileID] = @UserId ";

            param = new { From = from, To = to, UserId = userId };

            if (clientNetId != null)
                param = new { From = from, To = to, ClientNetId = clientNetId, UserId = userId };
        }

        sqlQuery +=
            "GROUP BY CONVERT(DATE, [SaleReturn].[FromDate]) " +
            ", [SaleReturnItem].[SaleReturnItemStatus] " +
            "ORDER BY DateFrom";

        _connection.Query(
            sqlQuery,
            (DateTime dateFrom, double qty, SaleReturnItemStatus saleReturnItemStatus) => {
                SaleReturn saleReturn;

                if (toReturn.Any(x => x.FromDate == dateFrom)) {
                    saleReturn = toReturn.FirstOrDefault(x => x.FromDate == dateFrom);
                } else {
                    saleReturn = new SaleReturn { FromDate = dateFrom };

                    toReturn.Add(saleReturn);
                }

                if (saleReturn == null) return saleReturnItemStatus;

                saleReturn.SaleReturnItems.Add(new SaleReturnItem {
                    Qty = qty,
                    SaleReturnItemStatus = saleReturnItemStatus
                });

                return saleReturnItemStatus;
            }, param, splitOn: "DateFrom,Qty,SaleReturnItemStatus");

        return toReturn;
    }
}