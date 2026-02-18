using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class MisplacedSaleRepository : IMisplacedSaleRepository {
    private readonly IDbConnection _connection;

    public MisplacedSaleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(MisplacedSale misplacedSale) {
        return _connection.Query<long>(
                "INSERT INTO [MisplacedSale] (SaleID, RetailClientID, MisplacedSaleStatus, UserID, Updated) " +
                "VALUES (@SaleId, @RetailClientId, @MisplacedSaleStatus, @UserId, GETUTCDATE()) " +
                "SELECT SCOPE_IDENTITY() ",
                misplacedSale)
            .Single();
    }

    public MisplacedSale GetBySaleNetId(Guid netId) {
        MisplacedSale misplacedSaleToReturn = null;

        Type[] types = {
            typeof(MisplacedSale),
            typeof(OrderItem),
            typeof(Product),
            typeof(RetailClient),
            typeof(User),
            typeof(ClientAgreement),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], MisplacedSale> mapper = objects => {
            MisplacedSale misplacedSale = (MisplacedSale)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            RetailClient retailClient = (RetailClient)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            decimal currentPrice = (decimal)objects[6];
            decimal currentLocalPrice = (decimal)objects[7];

            if (misplacedSaleToReturn == null) {
                if (orderItem != null)
                    if (product != null) {
                        product.CurrentPrice = currentPrice;
                        product.CurrentLocalPrice = currentLocalPrice;

                        orderItem.Product = product;
                        orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                        orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14);

                        misplacedSale.OrderItems.Add(orderItem);
                    }

                if (user != null) misplacedSale.User = user;

                misplacedSale.RetailClient = retailClient;

                misplacedSaleToReturn = misplacedSale;
            } else {
                if (orderItem == null) return misplacedSale;

                product.CurrentPrice = currentPrice;
                product.CurrentLocalPrice = currentLocalPrice;
                orderItem.Product = product;
                orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14);
                misplacedSaleToReturn.OrderItems.Add(orderItem);
            }

            return misplacedSale;
        };

        _connection.Query(
            "SELECT " +
            "[MisplacedSale].* " +
            ",[OrderItem].* " +
            ",[Product].* " +
            ",[RetailClient].* " +
            ",[User].* " +
            ",[ClientAgreement].* " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, [ClientAgreement].NetUID, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, [ClientAgreement].NetUID, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
            "FROM [MisplacedSale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [MisplacedSale].SaleID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [MisplacedSale].UserID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "WHERE [MisplacedSale].Deleted = 0 " +
            "AND [Sale].NetUID = @SaleNetId ",
            types,
            mapper,
            new {
                SaleNetId = netId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = true
            },
            splitOn: "ID,CurrentPrice,CurrentLocalPrice");

        return misplacedSaleToReturn;
    }

    public void Update(MisplacedSale misplacedSale) {
        _connection.Execute(
            "UPDATE [MisplacedSale] SET " +
            "[SaleID] = @SaleId, [RetailClientID] = @RetailClientId, MisplacedSaleStatus = @MisplacedSaleStatus, UserID = @UserId, [Updated] = GETUTCDATE() " +
            "WHERE [NetUID] = @NetUid",
            misplacedSale);
    }

    public List<MisplacedSale> GetByRetailClientNetId(Guid netId) {
        List<MisplacedSale> misplacedSales = new();

        _connection.Query<MisplacedSale, OrderItem, Product, RetailClient, MisplacedSale>(
            "SELECT * FROM [MisplacedSale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
            "WHERE [MisplacedSale].Deleted = 0 " +
            "AND [MisplacedSale].RetailClientID = @RetailClientId ",
            (misplacedSale, orderItem, product, retailClient) => {
                if (!misplacedSales.Any(m => m.Id.Equals(misplacedSale.Id))) {
                    if (orderItem != null) {
                        orderItem.Product = product;
                        misplacedSale.OrderItems.Add(orderItem);
                    }

                    misplacedSale.RetailClient = retailClient;

                    misplacedSales.Add(misplacedSale);
                } else {
                    MisplacedSale currentMisplacedSale = misplacedSales.First(m => m.Id.Equals(misplacedSale.Id));

                    if (currentMisplacedSale.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) return misplacedSale;

                    orderItem.Product = product;
                    currentMisplacedSale.OrderItems.Add(orderItem);
                }

                return misplacedSale;
            },
            new {
                NetId = netId
            });

        return misplacedSales;
    }

    public List<MisplacedSale> GetByRetailClientId(long id) {
        List<MisplacedSale> misplacedSales = new();

        _connection.Query<MisplacedSale, OrderItem, Product, RetailClient, MisplacedSale>(
            "SELECT * FROM [MisplacedSale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
            "WHERE [MisplacedSale].Deleted = 0 ",
            (misplacedSale, orderItem, product, retailClient) => {
                if (!misplacedSales.Any(m => m.Id.Equals(misplacedSale.Id))) {
                    if (orderItem != null) {
                        orderItem.Product = product;
                        misplacedSale.OrderItems.Add(orderItem);
                    }

                    misplacedSale.RetailClient = retailClient;

                    misplacedSales.Add(misplacedSale);
                } else {
                    MisplacedSale currentMisplacedSale = misplacedSales.First(m => m.Id.Equals(misplacedSale.Id));

                    if (currentMisplacedSale.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) return misplacedSale;

                    orderItem.Product = product;
                    currentMisplacedSale.OrderItems.Add(orderItem);
                }

                return misplacedSale;
            },
            new {
                RetailClientId = id
            });

        return misplacedSales;
    }

    public List<MisplacedSale> GetAll() {
        List<MisplacedSale> misplacedSales = new();

        Guid clientAgreementNetId = _connection.Query<Guid>(
            "SELECT [ClientAgreement].NetUID " +
            "FROM [Client] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [Client].IsForRetail = 1 " +
            "AND [Client].Deleted = 0 " +
            "AND [Agreement].WithVATAccounting = 1 ").First();

        Type[] types = {
            typeof(MisplacedSale),
            typeof(OrderItem),
            typeof(Product),
            typeof(RetailClient),
            typeof(User),
            typeof(ClientAgreement),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], MisplacedSale> mapper = objects => {
            MisplacedSale misplacedSale = (MisplacedSale)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            RetailClient retailClient = (RetailClient)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            decimal currentPrice = (decimal)objects[6];
            decimal currentLocalPrice = (decimal)objects[7];

            if (!misplacedSales.Any(m => m.Id.Equals(misplacedSale.Id))) {
                if (orderItem != null)
                    if (product != null) {
                        product.CurrentPrice = currentPrice;
                        product.CurrentLocalPrice = currentLocalPrice;

                        orderItem.Product = product;
                        orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                        orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14);

                        misplacedSale.OrderItems.Add(orderItem);
                    }

                if (user != null) misplacedSale.User = user;

                misplacedSale.RetailClient = retailClient;

                misplacedSales.Add(misplacedSale);
            } else {
                misplacedSale = misplacedSales.First(m => m.Id.Equals(misplacedSale.Id));

                if (orderItem == null) return misplacedSale;

                product.CurrentPrice = currentPrice;
                product.CurrentLocalPrice = currentLocalPrice;

                orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14);

                orderItem.Product = product;
                misplacedSale.OrderItems.Add(orderItem);
            }

            return misplacedSale;
        };

        _connection.Query(
            "SELECT " +
            "[MisplacedSale].* " +
            ",[OrderItem].* " +
            ",[Product].* " +
            ",[RetailClient].* " +
            ",[User].* " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
            "FROM [MisplacedSale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [MisplacedSale].UserID " +
            "WHERE [MisplacedSale].Deleted = 0 " +
            "AND [MisplacedSale].SaleID IS NULL " +
            "ORDER BY [MisplacedSale].Created DESC ",
            types,
            mapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = true,
                ClientAgreementNetId = clientAgreementNetId
            },
            splitOn: "ID,UserDetailsId,CurrentPrice,CurrentLocalPrice");

        return misplacedSales;
    }

    public List<MisplacedSale> GetAllFiltered(string number, DateTime from, DateTime to, bool isAccepted, Guid netId) {
        List<MisplacedSale> misplacedSales = new();

        Guid clientAgreementNetId = _connection.Query<Guid>(
            "SELECT [ClientAgreement].NetUID " +
            "FROM [Client] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [Client].IsForRetail = 1 " +
            "AND [Client].Deleted = 0 " +
            "AND [Agreement].WithVATAccounting = 1 ").First();

        Type[] types = {
            typeof(MisplacedSale),
            typeof(OrderItem),
            typeof(Product),
            typeof(RetailClient),
            typeof(User),
            typeof(decimal),
            typeof(decimal),
            typeof(bool)
        };

        Func<object[], MisplacedSale> mapper = objects => {
            MisplacedSale misplacedSale = (MisplacedSale)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            RetailClient retailClient = (RetailClient)objects[3];
            User user = (User)objects[4];
            decimal currentPrice = (decimal)objects[5];
            decimal currentLocalPrice = (decimal)objects[6];
            bool withSales = (bool)objects[7];

            if (!misplacedSales.Any(m => m.Id.Equals(misplacedSale.Id))) {
                if (orderItem != null)
                    if (product != null) {
                        product.CurrentPrice = currentPrice;
                        product.CurrentLocalPrice = currentLocalPrice;

                        orderItem.Product = product;

                        orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                        orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14);

                        misplacedSale.OrderItems.Add(orderItem);
                    }

                misplacedSale.WithSales = withSales;

                if (user != null) misplacedSale.User = user;

                misplacedSale.RetailClient = retailClient;

                misplacedSales.Add(misplacedSale);
            } else {
                misplacedSale = misplacedSales.First(m => m.Id.Equals(misplacedSale.Id));

                misplacedSale.WithSales = withSales;

                if (orderItem == null) return misplacedSale;

                product.CurrentPrice = currentPrice;
                product.CurrentLocalPrice = currentLocalPrice;

                orderItem.Product = product;
                orderItem.TotalAmount = Math.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                orderItem.TotalAmountLocal = Math.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14);

                misplacedSale.OrderItems.Add(orderItem);
            }

            return misplacedSale;
        };

        string query = "SELECT " +
                       "[MisplacedSale].* " +
                       ",[OrderItem].* " +
                       ",[Product].* " +
                       ",[RetailClient].* " +
                       ",[User].* " +
                       ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                       ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                       ",CAST(( " +
                       "CASE " +
                       "WHEN " +
                       "(SELECT COUNT(1) FROM [Sale] " +
                       "WHERE [RetailClient].[ID] = [Sale].[RetailClientId]) > 0 " +
                       "THEN 1 " +
                       "ELSE 0 " +
                       "END " +
                       ") as bit) AS WithSales " +
                       "FROM [MisplacedSale] " +
                       "LEFT JOIN [OrderItem] " +
                       "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
                       "LEFT JOIN [Product] " +
                       "ON [Product].ID = [OrderItem].ProductID " +
                       "LEFT JOIN [RetailClient] " +
                       "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
                       "LEFT JOIN [User] " +
                       "ON [User].ID = [MisplacedSale].UserID " +
                       "WHERE [MisplacedSale].Deleted = 0 " +
                       "AND [RetailClient].PhoneNumber LIKE '%' + @Number + '%' " +
                       "AND [MisplacedSale].Created >= @From " +
                       "AND [MisplacedSale].Created <= @To ";
        if (isAccepted) query += "AND [User].NetUID = @NetId ";

        query += "ORDER BY [MisplacedSale].Created DESC ";

        _connection.Query(
            query,
            types,
            mapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = true,
                ClientAgreementNetId = clientAgreementNetId,
                Number = number ?? string.Empty,
                From = from,
                To = to,
                NetId = netId
            },
            splitOn: "ID,CurrentPrice,CurrentLocalPrice,WithSales");

        return misplacedSales;
    }

    public MisplacedSale GetByRetailClientAndSaleIds(long retailClientId, long saleId) {
        MisplacedSale misplacedSaleToReturn = null;

        _connection.Query<MisplacedSale, OrderItem, Product, RetailClient, MisplacedSale>(
            "SELECT * FROM [MisplacedSale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
            "WHERE [MisplacedSale].Deleted = 0 " +
            "AND [MisplacedSale].SaleID = @SaleId " +
            "AND [MisplacedSale].RetailClientID = @RetailClientId ",
            (misplacedSale, orderItem, product, retailClient) => {
                if (misplacedSaleToReturn == null) {
                    if (orderItem != null) {
                        orderItem.Product = product;
                        misplacedSale.OrderItems.Add(orderItem);
                    }

                    misplacedSale.RetailClient = retailClient;

                    misplacedSaleToReturn = misplacedSale;
                } else {
                    if (orderItem == null) return misplacedSale;

                    orderItem.Product = product;
                    misplacedSaleToReturn.OrderItems.Add(orderItem);
                }

                return misplacedSale;
            },
            new {
                RetailClientId = retailClientId,
                SaleId = saleId
            });

        return misplacedSaleToReturn;
    }

    public MisplacedSale GetById(long id) {
        MisplacedSale misplacedSaleToReturn = null;

        _connection.Query<MisplacedSale, OrderItem, Product, RetailClient, MisplacedSale>(
            "SELECT * FROM [MisplacedSale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].MisplacedSaleId = [MisplacedSale].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [MisplacedSale].RetailClientID " +
            "WHERE [MisplacedSale].Deleted = 0 " +
            "AND [MisplacedSale].ID = @Id ",
            (misplacedSale, orderItem, product, retailClient) => {
                if (misplacedSaleToReturn == null) {
                    if (orderItem != null) {
                        orderItem.Product = product;
                        misplacedSale.OrderItems.Add(orderItem);
                    }

                    misplacedSale.RetailClient = retailClient;

                    misplacedSaleToReturn = misplacedSale;
                } else {
                    if (orderItem == null) return misplacedSale;

                    orderItem.Product = product;
                    misplacedSaleToReturn.OrderItems.Add(orderItem);
                }

                return misplacedSale;
            },
            new { Id = id });

        return misplacedSaleToReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [MisplacedSale] SET " +
            "[SaleID] = @SaleId, [RetailClientID] = @RetailClientId, [Updated] = GETUTCDATE(), [Deleted] = 1 " +
            "WHERE [NetUID] = @NetUid",
            new { NetUid = netId });
    }
}