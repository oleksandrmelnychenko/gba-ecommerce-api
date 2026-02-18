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
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class OrderRepository : IOrderRepository {
    private readonly IDbConnection _connection;

    public OrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Order order) {
        return _connection.Query<long>(
                "INSERT INTO [Order] (OrderSource, OrderStatus, UserId, ClientAgreementId, ClientShoppingCartId, Updated) " +
                "VALUES(@OrderSource, @OrderStatus, @UserId, @ClientAgreementId, @ClientShoppingCartId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                order
            )
            .Single();
    }

    public List<Order> GetAll() {
        return _connection.Query<Order>(
                "SELECT * FROM [Order] WHERE Deleted = 0"
            )
            .ToList();
    }

    public List<Order> GetAllShopOrders(long limit, long offset) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, Order> orderDict = new();

        var props = new { Limit = limit, Offset = offset, Shop = (int)OrderSource.Shop };

        string sqlExpression = ";WITH [Search_CTE] " +
                               "AS " +
                               "( " +
                               "SELECT ROW_NUMBER() OVER (ORDER BY [Order].ID DESC) AS RowNumber " +
                               ", [Order].ID " +
                               "FROM [Order] " +
                               "WHERE [Order].Deleted = 0 AND [Order].OrderSource = @Shop " +
                               ") " +
                               "SELECT ID " +
                               "FROM [Search_CTE] " +
                               "WHERE [Search_CTE].RowNumber > @Offset " +
                               "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
                               "ORDER BY RowNumber ";

        IEnumerable<long> orderIds = _connection.Query<long>(
            sqlExpression,
            props
        );

        string fullSqlExpression = "SELECT * FROM [Order] " +
                                   "LEFT OUTER JOIN [User] " +
                                   "ON [User].Id = [Order].UserId " +
                                   "LEFT OUTER JOIN OrderItem " +
                                   "ON OrderItem.OrderId = [Order].Id " +
                                   "LEFT OUTER JOIN Product " +
                                   "ON Product.Id = OrderItem.ProductId " +
                                   "LEFT OUTER JOIN ProductPricing " +
                                   "ON ProductPricing.ProductId = Product.Id AND ProductPricing.Deleted = 0 " +
                                   "LEFT OUTER JOIN Pricing " +
                                   "ON ProductPricing.PricingId = Pricing.Id AND Pricing.Deleted = 0 " +
                                   "LEFT OUTER JOIN Currency " +
                                   "ON Pricing.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
                                   "LEFT OUTER JOIN ClientAgreement " +
                                   "ON [Order].ClientAgreementId = ClientAgreement.Id AND ClientAgreement.Deleted = 0 " +
                                   "LEFT OUTER JOIN Agreement " +
                                   "ON ClientAgreement.AgreementId = Agreement.Id AND Agreement.Deleted = 0 " +
                                   "LEFT OUTER JOIN Currency AgreementCurrency " +
                                   "ON Agreement.CurrencyId = AgreementCurrency.Id AND AgreementCurrency.Deleted = 0 " +
                                   "LEFT OUTER JOIN Client " +
                                   "ON ClientAgreement.ClientId = Client.Id " +
                                   "LEFT OUTER JOIN RegionCode " +
                                   "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
                                   "WHERE [Order].Id IN @Ids " +
                                   "ORDER BY [Order].Created DESC";

        Type[] types = {
            typeof(Order),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], Order> mapper = objects => {
            Order order = (Order)objects[0];
            User user = (User)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ProductPricing productPricing = (ProductPricing)objects[4];
            Pricing pricing = (Pricing)objects[5];
            Currency currencyProductPricing = (Currency)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Agreement agreement = (Agreement)objects[8];
            Currency currencyAgreement = (Currency)objects[9];
            Client client = (Client)objects[10];
            RegionCode regionCode = (RegionCode)objects[11];

            if (user != null) order.User = user;

            if (orderItem != null && product != null) {
                if (productPricing != null && pricing != null) {
                    if (currencyProductPricing != null) pricing.Currency = currencyProductPricing;

                    productPricing.Pricing = pricing;
                    product.ProductPricings.Add(productPricing);
                }

                orderItem.Product = product;
                order.OrderItems.Add(orderItem);
            }

            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currencyAgreement != null) agreement.Currency = currencyAgreement;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                order.ClientAgreement = clientAgreement;
            }

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (orderDict.TryGetValue(order.Id, out Order existingOrder))
                existingOrder.OrderItems.Add(orderItem);
            else
                orderDict[order.Id] = order;

            return order;
        };

        var fullProps = new { Ids = orderIds };

        _connection.Query(fullSqlExpression, types, mapper, fullProps);

        List<Order> orders = orderDict.Values.ToList();
        orders.ForEach(o => o.TotalCount = o.OrderItems.Sum(i => i.Qty));
        orders.ForEach(o => o.TotalAmount = o.OrderItems.Sum(i => i.Product.ProductPricings.First().Price));

        return orders;
    }

    public List<Order> GetAllShopOrders() {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, Order> orderDict = new();

        string sqlExpression = "SELECT [Order].* " +
                               ",[User].* " +
                               ",[OrderItem].* " +
                               ",[Product].ID " +
                               ", [Product].Created " +
                               ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression += ", [Product].HasAnalogue " +
                         ", [Product].HasComponent " +
                         ", [Product].HasImage " +
                         ", [Product].[Image] " +
                         ", [Product].IsForSale " +
                         ", [Product].IsForWeb " +
                         ", [Product].IsForZeroSale " +
                         ", [Product].MainOriginalNumber " +
                         ", [Product].MeasureUnitID " +
                         ", [Product].NetUID " +
                         ", [Product].OrderStandard " +
                         ", [Product].PackingStandard " +
                         ", [Product].Size " +
                         ", [Product].[Top] " +
                         ", [Product].UCGFEA " +
                         ", [Product].Updated " +
                         ", [Product].VendorCode " +
                         ", [Product].Volume " +
                         ", [Product].[Weight] " +
                         ",[ClientAgreement].* " +
                         ",[Agreement].* " +
                         ",[Currency].* " +
                         ",[Client].* " +
                         ",[RegionCode].* " +
                         ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].[WithVATAccounting], [OrderItem].[ID]) AS ProductCurrentPrice " +
                         ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].[WithVATAccounting], [OrderItem].[ID]) AS ProductCurrentLocalPrice " +
                         "FROM [Order] " +
                         "LEFT JOIN [User] " +
                         "ON [User].Id = [Order].UserId " +
                         "LEFT JOIN OrderItem " +
                         "ON OrderItem.OrderId = [Order].Id " +
                         "LEFT JOIN Product " +
                         "ON Product.Id = OrderItem.ProductId " +
                         "LEFT JOIN ClientAgreement " +
                         "ON [Order].ClientAgreementId = ClientAgreement.Id AND ClientAgreement.Deleted = 0 " +
                         "LEFT JOIN Agreement " +
                         "ON ClientAgreement.AgreementId = Agreement.Id AND Agreement.Deleted = 0 " +
                         "LEFT JOIN Currency " +
                         "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
                         "LEFT JOIN Client " +
                         "ON ClientAgreement.ClientId = Client.Id " +
                         "LEFT JOIN RegionCode " +
                         "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
                         "WHERE [Order].OrderSource = @Shop " +
                         "AND [Order].Deleted = 0";

        Type[] types = {
            typeof(Order),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], Order> mapper = objects => {
            Order order = (Order)objects[0];
            User user = (User)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Currency currency = (Currency)objects[6];
            Client client = (Client)objects[7];
            RegionCode regionCode = (RegionCode)objects[8];
            decimal currentPrice = (decimal)objects[9];
            decimal currentLocalPrice = (decimal)objects[10];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (orderDict.TryGetValue(order.Id, out Order existingOrder) && orderItem != null) {
                if (product != null) {
                    product.CurrentPrice = currentPrice;
                    product.CurrentLocalPrice = currentLocalPrice;

                    orderItem.Product = product;
                    orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2);
                    orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2);

                    existingOrder.TotalAmount = Math.Round(order.TotalAmount + orderItem.TotalAmount, 2);
                    existingOrder.TotalAmountLocal = Math.Round(order.TotalAmountLocal + orderItem.TotalAmountLocal, 2);
                }

                order.OrderItems.Add(orderItem);

                existingOrder.OrderItems.Add(orderItem);

                ++existingOrder.TotalCount;
            } else {
                if (currency != null) agreement.Currency = currency;

                if (regionCode != null) client.RegionCode = regionCode;

                if (orderItem != null) {
                    if (product != null) {
                        product.CurrentPrice = currentPrice;
                        product.CurrentLocalPrice = currentLocalPrice;

                        orderItem.Product = product;
                        orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2);
                        orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2);

                        order.TotalAmount = Math.Round(order.TotalAmount + orderItem.TotalAmount, 2);
                        order.TotalAmountLocal = Math.Round(order.TotalAmountLocal + orderItem.TotalAmountLocal, 2);
                    }

                    order.OrderItems.Add(orderItem);

                    ++order.TotalCount;
                }

                clientAgreement.Agreement = agreement;
                clientAgreement.Client = client;

                order.ClientAgreement = clientAgreement;
                order.User = user;

                orderDict[order.Id] = order;
            }

            return order;
        };

        var props = new { OrderSource.Shop, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props, splitOn: "Id,ProductCurrentPrice,ProductCurrentLocalPrice");

        return orderDict.Values.ToList();
    }

    public List<Order> GetAllShopOrdersByClientNetId(Guid clientNetId) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, Order> orderDict = new();

        string sqlExpression = "SELECT * FROM [Order] " +
                               "LEFT OUTER JOIN [User] " +
                               "ON [User].Id = [Order].UserId " +
                               "LEFT OUTER JOIN OrderItem " +
                               "ON OrderItem.OrderId = [Order].Id " +
                               "LEFT OUTER JOIN Product " +
                               "ON Product.Id = OrderItem.ProductId " +
                               "LEFT OUTER JOIN ClientAgreement " +
                               "ON [Order].ClientAgreementId = ClientAgreement.Id AND ClientAgreement.Deleted = 0 " +
                               "LEFT OUTER JOIN Agreement " +
                               "ON ClientAgreement.AgreementId = Agreement.Id AND Agreement.Deleted = 0 " +
                               "LEFT OUTER JOIN Currency " +
                               "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
                               "LEFT OUTER JOIN Client " +
                               "ON ClientAgreement.ClientId = Client.Id " +
                               "LEFT OUTER JOIN RegionCode " +
                               "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
                               "LEFT JOIN ProductPricing " +
                               "ON ProductPricing.ProductID = Product.ID " +
                               "WHERE [Order].OrderSource = @Shop " +
                               "AND Client.NetUid = @ClientNetId " +
                               "AND [Order].Deleted = 0";

        Type[] types = {
            typeof(Order),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(ProductPricing)
        };

        Func<object[], Order> mapper = objects => {
            Order order = (Order)objects[0];
            User user = (User)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Currency currency = (Currency)objects[6];
            Client client = (Client)objects[7];
            RegionCode regionCode = (RegionCode)objects[8];
            ProductPricing productPricing = (ProductPricing)objects[9];

            if (user != null) order.User = user;

            if (orderItem != null) {
                if (product != null) {
                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.Product = product;
                }

                order.OrderItems.Add(orderItem);
            }

            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currency != null) agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                order.ClientAgreement = clientAgreement;
            }

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (orderDict.TryGetValue(order.Id, out Order existingOrder))
                existingOrder.OrderItems.Add(orderItem);
            else
                orderDict[order.Id] = order;

            return order;
        };

        var props = new {
            OrderSource.Shop, ClientNetId = clientNetId
        };

        _connection.Query(sqlExpression, types, mapper, props);

        return orderDict.Values.ToList();
    }

    public List<Order> GetAllShopOrdersByUserNetId(Guid userNetId) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, Order> orderDict = new();

        string sqlExpression = "SELECT * FROM [Order] " +
                               "LEFT OUTER JOIN [User] " +
                               "ON [User].Id = [Order].UserId " +
                               "LEFT OUTER JOIN OrderItem " +
                               "ON OrderItem.OrderId = [Order].Id " +
                               "LEFT OUTER JOIN Product " +
                               "ON Product.Id = OrderItem.ProductId " +
                               "LEFT OUTER JOIN ProductPricing " +
                               "ON ProductPricing.ProductId = Product.Id AND ProductPricing.Deleted = 0 " +
                               "LEFT OUTER JOIN Pricing " +
                               "ON ProductPricing.PricingId = Pricing.Id AND Pricing.Deleted = 0 " +
                               "LEFT OUTER JOIN Currency " +
                               "ON Pricing.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
                               "LEFT OUTER JOIN ClientAgreement " +
                               "ON [Order].ClientAgreementId = ClientAgreement.Id AND ClientAgreement.Deleted = 0 " +
                               "LEFT OUTER JOIN Agreement " +
                               "ON ClientAgreement.AgreementId = Agreement.Id AND Agreement.Deleted = 0 " +
                               "LEFT OUTER JOIN Currency AgreementCurrency " +
                               "ON Agreement.CurrencyId = AgreementCurrency.Id AND AgreementCurrency.Deleted = 0 " +
                               "LEFT OUTER JOIN Client " +
                               "ON ClientAgreement.ClientId = Client.Id " +
                               "LEFT OUTER JOIN RegionCode " +
                               "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
                               "WHERE [Order].OrderSource = @Shop " +
                               "AND [User].NetUid = @UserNetId " +
                               "AND [Order].Deleted = 0 " +
                               "ORDER BY [Order].Created DESC";

        Type[] types = {
            typeof(Order),
            typeof(User),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], Order> mapper = objects => {
            Order order = (Order)objects[0];
            User user = (User)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ProductPricing productPricing = (ProductPricing)objects[4];
            Pricing pricing = (Pricing)objects[5];
            Currency currencyProductPricing = (Currency)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Agreement agreement = (Agreement)objects[8];
            Currency currencyAgreement = (Currency)objects[9];
            Client client = (Client)objects[10];
            RegionCode regionCode = (RegionCode)objects[11];

            if (user != null) order.User = user;

            if (orderItem != null && product != null) {
                if (productPricing != null && pricing != null) {
                    if (currencyProductPricing != null) pricing.Currency = currencyProductPricing;

                    productPricing.Pricing = pricing;
                    product.ProductPricings.Add(productPricing);
                }

                orderItem.Product = product;
                order.OrderItems.Add(orderItem);
            }

            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currencyAgreement != null) agreement.Currency = currencyAgreement;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                order.ClientAgreement = clientAgreement;
            }

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (orderDict.TryGetValue(order.Id, out Order existingOrder))
                existingOrder.OrderItems.Add(orderItem);
            else
                orderDict[order.Id] = order;

            return order;
        };

        var props = new {
            OrderSource.Shop, UserNetId = userNetId
        };

        _connection.Query(sqlExpression, types, mapper, props);

        List<Order> orders = orderDict.Values.ToList();
        orders.ForEach(o => o.TotalCount = o.OrderItems.Sum(i => i.Qty));
        orders.ForEach(o => o.TotalAmount = o.OrderItems.Sum(i => i.Product.ProductPricings.First().Price));

        return orders;
    }

    public Order GetById(long id) {
        return _connection.Query<Order>(
                "SELECT * FROM [Order] WHERE Id = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Order GetByNetId(Guid netId) {
        return _connection.Query<Order>(
                "SELECT * FROM [Order] WHERE NetUid = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [Order] SET Deleted = 1 WHERE NetUid = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(Order order) {
        _connection.Execute(
            "UPDATE [Order] SET OrderSource = @OrderSource, OrderStatus = @OrderStatus, UserId = @UserId, ClientAgreementId = @ClientAgreementId, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid"
            , order);
    }

    public void UpdateClientAgreementByIds(long orderId, long clientAgreementId) {
        _connection.Execute(
            "UPDATE [Order] " +
            "SET ClientAgreementId = @ClientAgreementId, Updated = GETUTCDATE() " +
            "WHERE ID = @OrderId",
            new { OrderId = orderId, ClientAgreementId = clientAgreementId }
        );
    }

    public long GetAllShopOrdersTotalAmount() {
        return _connection.Query<long>(
                "SELECT COUNT(*) FROM [Order] " +
                "WHERE Deleted = 0 AND [Order].OrderSource = @Shop",
                new {
                    OrderSource.Shop
                }
            )
            .SingleOrDefault();
    }

    public long GetAllShopOrdersTotalAmountByUserNetId(Guid userNetId) {
        return _connection.Query<long>(
                "SELECT COUNT(*) FROM [Order] " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].Id = [Order].UserId " +
                "WHERE [User].NetUid = @UserNetId " +
                "AND [Order].Deleted = 0 " +
                "AND [Order].OrderSource = @Shop",
                new { UserNetId = userNetId, OrderSource.Shop }
            )
            .SingleOrDefault();
    }
}