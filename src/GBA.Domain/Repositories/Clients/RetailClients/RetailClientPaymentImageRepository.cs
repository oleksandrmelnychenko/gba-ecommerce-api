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
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;

namespace GBA.Domain.Repositories.Clients.RetailClients;

public sealed class RetailClientPaymentImageRepository : IRetailClientPaymentImageRepository {
    private readonly IDbConnection _connection;

    public RetailClientPaymentImageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(RetailClientPaymentImage paymentImage) {
        return _connection.Query<long>(
                "INSERT INTO [RetailClientPaymentImage] " +
                "([RetailClientId], [SaleId], [RetailPaymentStatusId], [Updated]) " +
                "VALUES (@RetailClientId, @SaleId, @RetailPaymentStatusId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY() ",
                paymentImage)
            .First();
    }

    public void Update(RetailClientPaymentImage paymentImage) {
        _connection.Execute(
            "UPDATE [RetailClientPaymentImage] SET " +
            "[RetailClientId] = @RetailClientId, " +
            "[SaleId] = @SaleId, " +
            "[RetailPaymentStatusId] = @RetailPaymentStatusId, " +
            "[Updated] = GETUTCDATE() " +
            "WHERE NetUID = @NetUid ",
            paymentImage);
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [RetailClientPaymentImage] SET Deleted = 1 " +
            "WHERE ID = @Id ",
            new { Id = id });
    }

    public IEnumerable<RetailClientPaymentImage> GetAllByRetailClientId(long id) {
        return _connection.Query<RetailClientPaymentImage>(
            "SELECT * FROM [RetailClientPaymentImage] " +
            "WHERE RetailClientId = @Id " +
            "AND [RetailClientPaymentImage].Deleted = 0",
            new { Id = id });
    }

    public IEnumerable<RetailClientPaymentImage> GetAllRetailClientNetId(Guid netId) {
        return _connection.Query<RetailClientPaymentImage>(
            "SELECT [RetailClientPaymentImage].* FROM [RetailClientPaymentImage] " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [RetailClientPaymentImage].RetailClientId " +
            "WHERE [RetailClient].NetUID = @NetId " +
            "AND [RetailClientPaymentImage].Deleted",
            new { NetId = netId });
    }

    public IEnumerable<RetailClientPaymentImage> GetAll() {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, RetailClientPaymentImage> paymentImageDict = new();
        // Track order items to avoid O(n) OrderItems.Any check
        HashSet<(long, long)> orderItemIds = new();

        Type[] types = {
            typeof(RetailClientPaymentImage),
            typeof(RetailClientPaymentImageItem),
            typeof(RetailClient),
            typeof(Sale),
            typeof(BaseSalePaymentStatus),
            typeof(BaseLifeCycleStatus),
            typeof(Order),
            typeof(OrderItem),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(User),
            typeof(Product)
        };

        Func<object[], RetailClientPaymentImage> mapper = objects => {
            RetailClientPaymentImage retailClientPaymentImage = (RetailClientPaymentImage)objects[0];
            RetailClientPaymentImageItem retailClientPaymentImageItem = (RetailClientPaymentImageItem)objects[1];
            RetailClient retailClient = (RetailClient)objects[2];
            Sale sale = (Sale)objects[3];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[4];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[5];
            Order order = (Order)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            SaleNumber saleNumber = (SaleNumber)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Organization organization = (Organization)objects[11];
            Currency currency = (Currency)objects[12];
            User user = (User)objects[13];
            Product product = (Product)objects[14];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (paymentImageDict.TryGetValue(retailClientPaymentImage.Id, out RetailClientPaymentImage existingImage)) {
                retailClientPaymentImage = existingImage;

                if (retailClientPaymentImageItem != null
                    && !retailClientPaymentImage.RetailClientPaymentImageItems.Any(r => r.Id.Equals(retailClientPaymentImageItem.Id))) {
                    retailClientPaymentImage.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                    retailClientPaymentImageItem.User = user;
                }

                // O(1) check for order item
                if (orderItemIds.Contains((retailClientPaymentImage.Id, orderItem.Id))) return retailClientPaymentImage;

                orderItem.Product = product;
                orderItem.TotalAmount = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                retailClientPaymentImage.Sale.Order.TotalAmount += orderItem.TotalAmount;
                retailClientPaymentImage.Sale.Order.TotalAmountLocal += orderItem.TotalAmountLocal;

                retailClientPaymentImage.Sale.Order.OrderItems.Add(orderItem);
                orderItemIds.Add((retailClientPaymentImage.Id, orderItem.Id));
            } else {
                orderItem.Product = product;
                orderItem.TotalAmount = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                order.OrderItems.Add(orderItem);
                order.TotalAmount = orderItem.TotalAmount;
                order.TotalAmountLocal = orderItem.TotalAmountLocal;

                agreement.Currency = currency;
                agreement.Organization = organization;
                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.Order = order;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;

                if (retailClient != null) {
                    retailClient.ShoppingCartJson = string.Empty;
                    retailClientPaymentImage.RetailClient = retailClient;
                }

                retailClientPaymentImage.Sale = sale;

                if (retailClientPaymentImageItem != null) {
                    retailClientPaymentImageItem.User = user;
                    retailClientPaymentImage.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                }

                paymentImageDict[retailClientPaymentImage.Id] = retailClientPaymentImage;
                orderItemIds.Add((retailClientPaymentImage.Id, orderItem.Id));
            }

            return retailClientPaymentImage;
        };

        _connection.Query(
            "SELECT " +
            "[RetailClientPaymentImage].* " +
            ", [RetailClientPaymentImageItem].* " +
            ", [RetailClient].* " +
            ", [Sale].* " +
            ", [BaseSalePaymentStatus].* " +
            ", [BaseLifeCycleStatus].* " +
            ", [Order].* " +
            ", [OrderItem].* " +
            ", [SaleNumber].* " +
            ", [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Organization].* " +
            ", [Currency].* " +
            ", [User].* " +
            ", [Product].* " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentPrice] " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentLocalPrice] " +
            "FROM [RetailClientPaymentImage] " +
            "LEFT JOIN [RetailClientPaymentImageItem] " +
            "ON [RetailClientPaymentImageItem].RetailClientPaymentImageID = [RetailClientPaymentImage].ID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [RetailClientPaymentImage].RetailClientId " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [RetailClientPaymentImage].SaleId " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN Product " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [RetailClientPaymentImageItem].UserID " +
            "WHERE [RetailClient].Deleted = 0 " +
            "AND [Sale].Deleted = 0 " +
            "ORDER BY [RetailClientPaymentImage].Created DESC ",
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return paymentImageDict.Values;
    }

    public IEnumerable<RetailClientPaymentImage> GetAllFiltered(
        DateTime? saleDateFrom = null,
        DateTime? saleDateTo = null,
        string saleNumber = "",
        string clientName = "",
        string phoneNumber = "") {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, RetailClientPaymentImage> paymentImageDict = new();
        // Track order items to avoid O(n) OrderItems.Any check
        HashSet<(long, long)> orderItemIds = new();

        string idsQuery =
            ";WITH [IDS_CTE] AS (SELECT ROW_NUMBER() OVER (ORDER BY [RetailClientPaymentImage].ID DESC) AS [RowNumber] " +
            ", [RetailClientPaymentImage].ID " +
            "FROM [RetailClientPaymentImage] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [RetailClientPaymentImage].SaleId " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [RetailClientPaymentImage].RetailClientId " +
            "WHERE PATINDEX(N'%' + @SaleNumber + N'%', [SaleNumber].[Value]) > 0 " +
            "AND PATINDEX(N'%' + @ClientName + N'%', [RetailClient].[Name]) > 0 " +
            "AND PATINDEX(N'%' + @PhoneNumber + N'%', [RetailClient].[PhoneNumber]) > 0 ";

        idsQuery += saleDateFrom != null && saleDateTo != null
            ? "AND [Sale].Created >= @SaleDateFrom " +
              "AND [Sale].Created <= @SaleDateTo "
            : "";

        idsQuery +=
            "GROUP BY [RetailClientPaymentImage].ID, [RetailClientPaymentImage].Created) " +
            "SELECT [RetailClientPaymentImage].ID " +
            "FROM [RetailClientPaymentImage] " +
            "WHERE [RetailClientPaymentImage].ID IN ( " +
            "SELECT [IDS_CTE].ID FROM [IDS_CTE] " +
            ") " +
            "GROUP BY [RetailClientPaymentImage].ID, [RetailClientPaymentImage].Created " +
            "ORDER BY [RetailClientPaymentImage].Created DESC ";

        IEnumerable<long> ids = _connection.Query<long>(
            idsQuery,
            new {
                SaleDateFrom = saleDateFrom,
                SaleDateTo = saleDateTo,
                SaleNumber = saleNumber,
                ClientName = clientName,
                PhoneNumber = phoneNumber
            });

        Type[] types = {
            typeof(RetailClientPaymentImage),
            typeof(RetailClientPaymentImageItem),
            typeof(RetailClient),
            typeof(RetailPaymentStatus),
            typeof(Sale),
            typeof(BaseSalePaymentStatus),
            typeof(BaseLifeCycleStatus),
            typeof(Order),
            typeof(OrderItem),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(VatRate),
            typeof(Currency),
            typeof(User),
            typeof(Product)
        };

        Func<object[], RetailClientPaymentImage> mapper = objects => {
            RetailClientPaymentImage retailClientPaymentImage = (RetailClientPaymentImage)objects[0];
            RetailClientPaymentImageItem retailClientPaymentImageItem = (RetailClientPaymentImageItem)objects[1];
            RetailClient retailClient = (RetailClient)objects[2];
            RetailPaymentStatus retailPaymentStatus = (RetailPaymentStatus)objects[3];
            Sale sale = (Sale)objects[4];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[5];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[6];
            Order order = (Order)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            SaleNumber number = (SaleNumber)objects[9];
            ClientAgreement clientAgreement = (ClientAgreement)objects[10];
            Agreement agreement = (Agreement)objects[11];
            Organization organization = (Organization)objects[12];
            VatRate vatRate = (VatRate)objects[13];
            Currency currency = (Currency)objects[14];
            User user = (User)objects[15];
            Product product = (Product)objects[16];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (paymentImageDict.TryGetValue(retailClientPaymentImage.Id, out RetailClientPaymentImage existingImage)) {
                retailClientPaymentImage = existingImage;

                if (retailClientPaymentImageItem != null
                    && !retailClientPaymentImage.RetailClientPaymentImageItems.Any(r => r.Id.Equals(retailClientPaymentImageItem.Id))) {
                    retailClientPaymentImage.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                    retailClientPaymentImageItem.User = user;
                }

                // O(1) check for order item
                if (orderItemIds.Contains((retailClientPaymentImage.Id, orderItem.Id))) return retailClientPaymentImage;

                orderItem.Product = product;
                orderItem.TotalAmount = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                retailClientPaymentImage.Sale.Order.TotalAmount += orderItem.TotalAmount;
                retailClientPaymentImage.Sale.Order.TotalAmountLocal += orderItem.TotalAmountLocal;

                retailClientPaymentImage.Sale.Order.OrderItems.Add(orderItem);
                orderItemIds.Add((retailClientPaymentImage.Id, orderItem.Id));
            } else {
                orderItem.Product = product;
                orderItem.TotalAmount = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                order.OrderItems.Add(orderItem);
                order.TotalAmount = orderItem.TotalAmount;
                order.TotalAmountLocal = orderItem.TotalAmountLocal;

                agreement.Currency = currency;
                organization.VatRate = vatRate;
                agreement.Organization = organization;
                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.Order = order;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = number;

                if (retailClient != null) {
                    retailClient.ShoppingCartJson = string.Empty;
                    retailClientPaymentImage.RetailClient = retailClient;
                }

                retailClientPaymentImage.Sale = sale;
                retailPaymentStatus.AmountToPay = retailPaymentStatus.Amount - retailPaymentStatus.PaidAmount;
                retailClientPaymentImage.RetailPaymentStatus = retailPaymentStatus;

                if (retailClientPaymentImageItem != null) {
                    retailClientPaymentImageItem.User = user;
                    retailClientPaymentImage.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                }

                paymentImageDict[retailClientPaymentImage.Id] = retailClientPaymentImage;
                orderItemIds.Add((retailClientPaymentImage.Id, orderItem.Id));
            }

            return retailClientPaymentImage;
        };

        _connection.Query(
            "SELECT " +
            "[RetailClientPaymentImage].* " +
            ", [RetailClientPaymentImageItem].* " +
            ", [RetailClient].* " +
            ", [RetailPaymentStatus].* " +
            ", [Sale].* " +
            ", [BaseSalePaymentStatus].* " +
            ", [BaseLifeCycleStatus].* " +
            ", [Order].* " +
            ", [OrderItem].* " +
            ", [SaleNumber].* " +
            ", [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Organization].* " +
            ", [VatRate].* " +
            ", [Currency].* " +
            ", [User].* " +
            ", [Product].* " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentPrice] " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, 1, [OrderItem].[ID]) " +
            "END) AS [CurrentLocalPrice] " +
            "FROM [RetailClientPaymentImage] " +
            "LEFT JOIN [RetailClientPaymentImageItem] " +
            "ON [RetailClientPaymentImageItem].RetailClientPaymentImageID = [RetailClientPaymentImage].ID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [RetailClientPaymentImage].RetailClientId " +
            "LEFT JOIN [RetailPaymentStatus] " +
            "ON [RetailPaymentStatus].ID = [RetailClientPaymentImage].RetailPaymentStatusId " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [RetailClientPaymentImage].SaleId " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN Product " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].ID = [Organization].VatRateID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [RetailClientPaymentImageItem].UserID " +
            "WHERE [RetailClientPaymentImage].ID IN @Ids " +
            "AND [RetailClient].Deleted = 0 " +
            "AND [Sale].Deleted = 0 " +
            "ORDER BY [RetailClientPaymentImage].Created DESC ",
            types,
            mapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = ids
            }
        );

        return paymentImageDict.Values;
    }

    public RetailClientPaymentImage GetPaymentImageBySaleNetId(Guid netId) {
        RetailClientPaymentImage toReturn = null;

        _connection.Query<RetailClientPaymentImage, RetailClientPaymentImageItem, RetailClientPaymentImage>(
            "SELECT [RetailClientPaymentImage].*, [RetailClientPaymentImageItem].* FROM [RetailClientPaymentImage] " +
            "LEFT JOIN [RetailClientPaymentImageItem] " +
            "ON [RetailClientPaymentImageItem].RetailClientPaymentImageID = [RetailClientPaymentImage].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [RetailClientPaymentImage].SaleID " +
            "AND [Sale].Deleted = 0 " +
            "WHERE [Sale].NetUID = @NetId " +
            "AND [RetailClientPaymentImage].Deleted = 0 ",
            (retailClientPaymentImage, retailClientPaymentImageItem) => {
                if (toReturn == null) {
                    retailClientPaymentImage.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                    toReturn = retailClientPaymentImage;
                } else {
                    toReturn.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                }

                return retailClientPaymentImage;
            },
            new {
                NetId = netId
            }
        );

        return toReturn;
    }

    public RetailClientPaymentImage GetById(long id) {
        RetailClientPaymentImage toReturn = null;

        Type[] types = {
            typeof(RetailClientPaymentImage),
            typeof(RetailClientPaymentImageItem),
            typeof(RetailClient),
            typeof(RetailPaymentStatus),
            typeof(Sale),
            typeof(BaseSalePaymentStatus),
            typeof(BaseLifeCycleStatus),
            typeof(Order),
            typeof(OrderItem),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(VatRate),
            typeof(Currency),
            typeof(User),
            typeof(Product)
        };

        Func<object[], RetailClientPaymentImage> mapper = objects => {
            RetailClientPaymentImage retailClientPaymentImage = (RetailClientPaymentImage)objects[0];
            RetailClientPaymentImageItem retailClientPaymentImageItem = (RetailClientPaymentImageItem)objects[1];
            RetailClient retailClient = (RetailClient)objects[2];
            RetailPaymentStatus retailPaymentStatus = (RetailPaymentStatus)objects[3];
            Sale sale = (Sale)objects[4];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[5];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[6];
            Order order = (Order)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            SaleNumber saleNumber = (SaleNumber)objects[9];
            ClientAgreement clientAgreement = (ClientAgreement)objects[10];
            Agreement agreement = (Agreement)objects[11];
            Organization organization = (Organization)objects[12];
            VatRate vatRate = (VatRate)objects[13];
            Currency currency = (Currency)objects[14];
            User user = (User)objects[15];
            Product product = (Product)objects[16];

            if (toReturn != null) {
                if (!toReturn.RetailClientPaymentImageItems.Any(r => r.Id.Equals(retailClientPaymentImageItem.Id))) {
                    retailClientPaymentImageItem.User = user;
                    toReturn.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);
                }

                if (toReturn.Sale.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) return retailClientPaymentImage;

                decimal vatRateValue = Convert.ToDecimal(vatRate?.Value ?? 0) / 100;

                orderItem.Product = product;
                orderItem.TotalAmount = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (toReturn.Sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRateValue / (vatRateValue + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                toReturn.Sale.Order.TotalAmount += orderItem.TotalAmount;
                toReturn.Sale.Order.TotalAmountLocal += orderItem.TotalAmountLocal;

                toReturn.Sale.Order.OrderItems.Add(orderItem);
            } else {
                decimal vatRateValue = Convert.ToDecimal(vatRate?.Value ?? 0) / 100;

                orderItem.Product = product;
                orderItem.TotalAmount = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(Convert.ToDecimal(orderItem.Qty) * orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRateValue / (vatRateValue + 1)),
                            14,
                            MidpointRounding.AwayFromZero);


                order.OrderItems.Add(orderItem);
                order.TotalAmount = orderItem.TotalAmount;
                order.TotalAmountLocal = orderItem.TotalAmountLocal;

                organization.VatRate = vatRate;
                agreement.Currency = currency;
                agreement.Organization = organization;
                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.Order = order;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;

                if (retailClient != null) {
                    retailClient.ShoppingCartJson = string.Empty;
                    retailClientPaymentImage.RetailClient = retailClient;
                }

                retailClientPaymentImage.Sale = sale;
                retailPaymentStatus.AmountToPay = retailPaymentStatus.Amount - retailPaymentStatus.PaidAmount;
                retailClientPaymentImage.RetailPaymentStatus = retailPaymentStatus;
                retailClientPaymentImageItem.User = user;
                retailClientPaymentImage.RetailClientPaymentImageItems.Add(retailClientPaymentImageItem);

                toReturn = retailClientPaymentImage;
            }

            return retailClientPaymentImage;
        };

        _connection.Query(
            "SELECT " +
            "[RetailClientPaymentImage].* " +
            ", [RetailClientPaymentImageItem].* " +
            ", [RetailClient].* " +
            ", [RetailPaymentStatus].* " +
            ", [Sale].* " +
            ", [BaseSalePaymentStatus].* " +
            ", [BaseLifeCycleStatus].* " +
            ", [Order].* " +
            ", [OrderItem].* " +
            ", [SaleNumber].* " +
            ", [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Organization].* " +
            ", [VatRate].* " +
            ", [Currency].* " +
            ", [User].* " +
            ", [Product].* " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentPrice] " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentLocalPrice] " +
            "FROM [RetailClientPaymentImage] " +
            "LEFT JOIN [RetailClientPaymentImageItem] " +
            "ON [RetailClientPaymentImageItem].RetailClientPaymentImageID = [RetailClientPaymentImage].ID " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [RetailClientPaymentImage].RetailClientId " +
            "LEFT JOIN [RetailPaymentStatus] " +
            "ON [RetailPaymentStatus].ID = [RetailClientPaymentImage].RetailPaymentStatusId " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [RetailClientPaymentImage].SaleId " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN Product " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [RetailClientPaymentImageItem].UserID " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].ID = [Organization].VatRateID " +
            "WHERE [RetailClientPaymentImage].ID = @Id " +
            "AND [Sale].Deleted = 0 " +
            "AND [RetailClient].Deleted = 0 ",
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Id = id }
        );

        return toReturn;
    }
}