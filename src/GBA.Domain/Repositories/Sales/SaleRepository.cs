using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.EntityHelpers.TotalDashboards;
using GBA.Domain.FilterEntities;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleRepository : ISaleRepository {
    private const string SALE_IDS_FOR_SHIPMENT_EXPRESSION =
        ";WITH [SALE_IDS_CTE] AS ( " +
        "SELECT ROW_NUMBER() OVER (ORDER BY [Sale].[ID] DESC) AS [RowNumber] " +
        ", [Sale].ID " +
        ", COUNT(*) OVER() [TotalRowsQty] " +
        "FROM [Sale] " +
        "LEFT JOIN [SaleNumber] ON [SaleNumber].ID = [Sale].SaleNumberID " +
        "LEFT JOIN [BaseLifeCycleStatus] ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
        "LEFT JOIN [User] AS [SaleUser] ON [SaleUser].ID = [Sale].UserID " +
        "LEFT JOIN [ClientAgreement] ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
        "LEFT JOIN [Client] ON [Client].ID = [ClientAgreement].ClientID " +
        "LEFT JOIN [Agreement] ON [ClientAgreement].AgreementID = [Agreement].ID " +
        "LEFT JOIN [Organization] ON [Organization].ID = [Agreement].OrganizationID " +
        "LEFT JOIN [Order] ON [Sale].OrderID = [Order].ID " +
        "LEFT JOIN [OrderItem] ON [OrderItem].OrderID = [Order].ID AND [OrderItem].Deleted = 0 AND [OrderItem].Qty > 0 " +
        "LEFT JOIN [Product] ON [Product].ID = [OrderItem].ProductID " +
        "LEFT JOIN [BaseSalePaymentStatus] " +
        "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
        "WHERE [Sale].ChangedToInvoice >= @From " +
        "AND [Sale].ChangedToInvoice <= @To " +
        "AND ( " +
        "[Sale].IsVatSale = 0 " +
        "OR " +
        "( " +
        "[Sale].IsVatSale = 1 " +
        "AND " +
        "[BaseSalePaymentStatus].SalePaymentStatusType > 0 " +
        "AND " +
        "[BaseSalePaymentStatus].SalePaymentStatusType <= 3 " +
        ") " +
        "OR ([Sale].[IsAcceptedToPacking] = 1) " +
        ") " +
        "AND [Sale].Deleted = 0 " +
        "AND [Sale].IsMerged = 0 " +
        "AND [Organization].Culture = @Culture " +
        "AND [BaseLifeCycleStatus].SaleLifeCycleType = @SaleLifeCycleType " +
        "GROUP BY [Sale].ID) " +
        "SELECT [Sale].ID " +
        ", (SELECT TOP 1 [TotalRowsQty] FROM [SALE_IDS_CTE]) AS [TotalRowsQty] " +
        "FROM [Sale] " +
        "WHERE [Sale].[ID] IN ( " +
        "SELECT [SALE_IDS_CTE].[ID] " +
        "FROM [SALE_IDS_CTE] " +
        "WHERE [SALE_IDS_CTE].[RowNumber] > @Offset " +
        "AND [SALE_IDS_CTE].[RowNumber] <= @Limit + @Offset " +
        ") " +
        "GROUP BY [Sale].ID, [Sale].Updated, [Sale].ChangedToInvoice " +
        "ORDER BY ISNULL(Sale.ChangedToInvoice, Sale.Updated) DESC ";

    private readonly IDbConnection _connection;

    public SaleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void UpdateSaleInvoiceNumber(Sale sale) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET SaleInvoiceNumberId = @SaleInvoiceNumberId " +
            "WHERE NetUID = @NetUid",
            sale
        );
    }

    public void UpdateSaleExpiredDays(Sale sale) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET ExpiredDays = @ExpiredDays, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            sale
        );
    }

    public long Add(Sale sale) {
        return _connection.Query<long>(
            "INSERT INTO Sale (ClientAgreementId, OrderId, UserId, BaseLifeCycleStatusId, BaseSalePaymentStatusId, Comment, DeliveryRecipientId, DeliveryRecipientAddressId, " +
            "TransporterId, SaleNumberId, ParentNetId, IsMerged, SaleInvoiceDocumentId, IsCashOnDelivery, HasDocuments, CashOnDeliveryAmount, IsPrinted, TTN, " +
            "ShippingAmount, TaxFreePackListId, SadId, IsVatSale, ShippingAmountEur, IsLocked, IsPaymentBillDownloaded, ExpiredDays, Updated, [IsPrintedPaymentInvoice], " +
            "[IsAcceptedToPacking], [RetailClientId], [IsFullPayment], [MisplacedSaleId], [WorkplaceID], [CustomersOwnTtnID]) " +
            "VALUES(@ClientAgreementId, @OrderId, @UserId, @BaseLifeCycleStatusId, @BaseSalePaymentStatusId, @Comment, @DeliveryRecipientId, @DeliveryRecipientAddressId, " +
            "@TransporterId, @SaleNumberId, @ParentNetId, @IsMerged, @SaleInvoiceDocumentId, @IsCashOnDelivery, @HasDocuments, @CashOnDeliveryAmount, 0, ''," +
            "@ShippingAmount, NULL, NULL, @IsVatSale, 0.00, 0, 0, 0.00, getutcdate(), @IsPrintedPaymentInvoice, @IsAcceptedToPacking, @RetailClientId, @IsFullPayment, " +
            "@MisplacedSaleId, @WorkplaceId, @CustomersOwnTtnId); " +
            "SELECT SCOPE_IDENTITY()",
            sale
        ).Single();
    }

    public List<long> GetAllSaleIdsThatNeedToBeMergedByRootClientNetId(Guid clientNetId, string culture, bool withVat = false) {
        return _connection.Query<long>(
            ";WITH " +
            "ClientSubClient_CTE " +
            "AS " +
            "( " +
            "SELECT SelectedClient.ID AS RootClientID " +
            "FROM Client AS SelectedClient " +
            "WHERE SelectedClient.NetUID = @ClientNetId " +
            "UNION ALL " +
            "SELECT SubClient.ID " +
            "FROM ClientSubClient " +
            "JOIN Client AS SubClient " +
            "ON ClientSubClient.SubClientID = SubClient.ID " +
            "JOIN ClientSubClient_CTE " +
            "ON ClientSubClient.RootClientID = ClientSubClient_CTE.RootClientID " +
            "WHERE ClientSubClient.Deleted = 0 " +
            "), " +
            "ClientAgreement_CTE " +
            "AS " +
            "( " +
            "SELECT ClientAgreement.ID " +
            "FROM ClientAgreement " +
            "WHERE ClientAgreement.ClientID IN (SELECT * FROM ClientSubClient_CTE) " +
            "AND ClientAgreement.Deleted = 0 " +
            "), " +
            "Sale_CTE " +
            "AS " +
            "( " +
            "SELECT Sale.ID " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "WHERE Sale.ClientAgreementID IN (SELECT * FROM ClientAgreement_CTE) " +
            "AND Sale.IsMerged = 0 " +
            "AND [Sale].IsVatSale = @WithVat " +
            "AND CONVERT(date, Sale.Updated) < CONVERT(date, GETUTCDATE()) " +
            "AND BaseLifeCycleStatus.SaleLifeCycleType = 0 " +
            "AND Organization.Culture = @Culture " +
            ") " +
            "SELECT * FROM Sale_CTE",
            new { ClientNetId = clientNetId, Culture = culture, WithVat = withVat }
        ).ToList();
    }

    public List<SalesRegisterModel> GetAllSalesWithReturnsByClientNetIdFiltered(GetSalesRegisterByClientNetIdQuery message) {
        List<SalesRegisterModel> toReturn = new();

        SaleLifeCycleType saleLifeCycleType = message.SaleRegisterType is SaleRegisterType.Order ? SaleLifeCycleType.New : SaleLifeCycleType.Packaging;

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        object parameters = new {
            message.ClientNetId,
            message.Value,
            message.From,
            message.To,
            message.Limit,
            message.Offset,
            Type = saleLifeCycleType,
            Culture = culture
        };

        string idsQuery =
            ";WITH [IDS_CTE] AS ( " +
            "SELECT [Sale].ID " +
            ", 0 AS [Type] " +
            ", ISNULL(Sale.ChangedToInvoice, Sale.Updated) AS [FromDate] " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleNumber] ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [User] AS [SaleUser] ON [SaleUser].ID = [Sale].UserID " +
            "LEFT JOIN [ClientAgreement] ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Order] ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [OrderItem] ON [OrderItem].OrderID = [Order].ID AND [OrderItem].Deleted = 0 AND [OrderItem].Qty > 0 " +
            "LEFT JOIN [Product] ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [Client].NetUID = @ClientNetId " +
            "AND ISNULL([Sale].ChangedToInvoice, [Sale].Updated) >= @From " +
            "AND ISNULL([Sale].ChangedToInvoice, [Sale].Updated) <= @To " +
            "AND [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [Organization].Culture = @Culture ";

        if (!message.ClientNetId.Equals(Guid.Empty)) idsQuery += "AND Client.NetUID = @ClientNetId ";

        if (message.SaleRegisterType != null
            && Enum.IsDefined(typeof(SaleRegisterType), message.SaleRegisterType)
            && message.SaleRegisterType == SaleRegisterType.SaleReturn)
            idsQuery += "AND [Sale].ID IS NULL ";
        else if (message.SaleRegisterType != null && Enum.IsDefined(typeof(SaleRegisterType), message.SaleRegisterType))
            idsQuery += "AND BaseLifeCycleStatus.SaleLifeCycleType = @Type ";

        if (!string.IsNullOrEmpty(message.Value))
            idsQuery +=
                "AND " +
                "( " +
                "Product.VendorCode like '%' + @Value + '%' " +
                "OR Product.Name like '%' + @Value + '%' " +
                "OR Product.MainOriginalNumber like '%' + @Value + '%' " +
                "OR SaleNumber.Value like '%' + @Value + '%' " +
                ") ";

        idsQuery +=
            "GROUP BY [Sale].[ID], [Sale].[Updated], [Sale].[ChangedToInvoice] " +
            "UNION ALL " +
            "SELECT [SaleReturn].ID " +
            ", 1 AS [Type] " +
            ", [SaleReturn].[FromDate] " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [User] AS [ReturnCreatedBy] " +
            "ON [ReturnCreatedBy].ID = [SaleReturn].CreatedByID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "LEFT JOIN [User] AS [ItemCreatedBy] " +
            "ON [ItemCreatedBy].ID = [SaleReturnItem].CreatedByID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SaleReturnItem].StorageID " +
            "WHERE [Client].NetUID = @ClientNetId " +
            "AND [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To ";

        if (message.SaleRegisterType != null
            && Enum.IsDefined(typeof(SaleRegisterType), message.SaleRegisterType)
            && message.SaleRegisterType != SaleRegisterType.SaleReturn)
            idsQuery += "AND [SaleReturn].ID IS NULL ";

        if (!message.ClientNetId.Equals(Guid.Empty)) idsQuery += "AND Client.NetUID = @ClientNetId ";

        if (!string.IsNullOrEmpty(message.Value))
            idsQuery +=
                "AND " +
                "( " +
                "Product.VendorCode like '%' + @Value + '%' " +
                "OR Product.Name like '%' + @Value + '%' " +
                "OR Product.MainOriginalNumber like '%' + @Value + '%' " +
                //"OR SaleNumber.Value like '%' + @Value + '%' " +
                ") ";

        idsQuery +=
            "GROUP BY [SaleReturn].ID, [SaleReturn].FromDate ), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [IDS_CTE].[ID] DESC) AS [RowNumber] " +
            ", [IDS_CTE].[ID] " +
            ", [IDS_CTE].[Type] " +
            ", [IDS_CTE].[FromDate] " +
            ", COUNT(*) OVER() [TotalRowsQty] " +
            "FROM [IDS_CTE] " +
            ") " +
            "SELECT [Rowed_CTE].[ID] " +
            ", [Rowed_CTE].[Type] " +
            ", [Rowed_CTE].[TotalRowsQty] " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].[RowNumber] > @Offset " +
            "AND [Rowed_CTE].[RowNumber] <= @Limit + @Offset " +
            "GROUP BY [Rowed_CTE].[ID], [Rowed_CTE].[Type], [Rowed_CTE].[TotalRowsQty], [Rowed_CTE].[FromDate] " +
            "ORDER BY [Rowed_CTE].[FromDate] ";

        IEnumerable<dynamic> documents = _connection.Query(idsQuery, parameters);

        if (!documents.Any()) return new List<SalesRegisterModel>();

        if (documents.Any(e => e.Type == 0)) {
            string sqlExpression =
                "SELECT [Sale].* " +
                ",[Order].* " +
                ",[OrderItem].* " +
                ",[Product].ID " +
                ",[Product].Created " +
                ",[Product].Deleted ";

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                sqlExpression += ",[Product].[NameUA] AS [Name] ";
                sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
            } else {
                sqlExpression += ",[Product].[NameUA] AS [Name] ";
                sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
            }

            sqlExpression +=
                ",[Product].HasAnalogue " +
                ",[Product].HasComponent " +
                ",[Product].HasImage " +
                ",[Product].[Image] " +
                ",[Product].IsForSale " +
                ",[Product].IsForWeb " +
                ",[Product].IsForZeroSale " +
                ",[Product].MainOriginalNumber " +
                ",[Product].MeasureUnitID " +
                ",[Product].NetUID " +
                ",[Product].OrderStandard " +
                ",[Product].PackingStandard " +
                ",[Product].Size " +
                ",[Product].[Top] " +
                ",[Product].UCGFEA " +
                ",[Product].Updated " +
                ",[Product].VendorCode " +
                ",[Product].Volume " +
                ",[Product].[Weight] " +
                ", (CASE " +
                "WHEN [OrderItem].IsFromReSale = 1 " +
                "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [SaleClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [SaleClientAgreement].NetUID, @Culture, [SaleAgreement].WithVATAccounting, [OrderItem].[ID]) " +
                "END) AS [CurrentPrice] " +
                ", (CASE " +
                "WHEN [OrderItem].IsFromReSale = 1 " +
                "THEN dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, [SaleClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [SaleClientAgreement].NetUID, @Culture, [SaleAgreement].WithVATAccounting, [OrderItem].[ID]) " +
                "END) AS [CurrentLocalPrice] " +
                ",[SaleClientAgreement].* " +
                ",[Client].* " +
                ",[SaleAgreement].* " +
                ",[SaleAgreementCurrency].* " +
                ",[SaleNumber].* " +
                ",[BaseLifeCycleStatus].* " +
                ",[BaseSalePaymentStatus].* " +
                ",[Organization].* " +
                "FROM [Sale] " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "AND [OrderItem].Deleted = 0 " +
                "AND [OrderItem].Qty > 0 " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [OrderItem].ProductID " +
                "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
                "ON [SaleClientAgreement].ID = [Sale].ClientAgreementID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SaleClientAgreement].ClientID " +
                "LEFT JOIN [Agreement] AS [SaleAgreement] " +
                "ON [SaleAgreement].ID = [SaleClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [SaleAgreementCurrency] " +
                "ON [SaleAgreementCurrency].ID = [SaleAgreement].CurrencyID " +
                "AND [SaleAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
                "LEFT JOIN [BaseSalePaymentStatus] " +
                "ON [Sale].BaseSalePaymentStatusID = [BaseSalePaymentStatus].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SaleAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [Sale].ID IN @Ids";

            Type[] types = {
                typeof(Sale),
                typeof(Order),
                typeof(OrderItem),
                typeof(Product),
                typeof(ClientAgreement),
                typeof(Client),
                typeof(Agreement),
                typeof(Currency),
                typeof(SaleNumber),
                typeof(BaseLifeCycleStatus),
                typeof(BaseSalePaymentStatus),
                typeof(Organization)
            };

            Func<object[], Sale> mapper = objects => {
                Sale sale = (Sale)objects[0];
                Order order = (Order)objects[1];
                OrderItem orderItem = (OrderItem)objects[2];
                Product product = (Product)objects[3];
                ClientAgreement clientAgreement = (ClientAgreement)objects[4];
                Client saleClient = (Client)objects[5];
                Agreement agreement = (Agreement)objects[6];
                Currency currency = (Currency)objects[7];
                SaleNumber saleNumber = (SaleNumber)objects[8];
                BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[9];
                BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[10];
                Organization organization = (Organization)objects[11];

                if (toReturn.Any(e => e.SaleStatistic != null) && toReturn.Any(e => e.SaleStatistic.Sale.Id.Equals(sale.Id))) {
                    Sale saleFromList = toReturn.First(e => e.SaleStatistic.Sale.Id.Equals(sale.Id)).SaleStatistic.Sale;

                    if (orderItem == null || saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) return sale;

                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else {
                    SalesRegisterModel item = new();

                    if (orderItem != null) {
                        orderItem.Product = product;

                        order.OrderItems.Add(orderItem);
                    }

                    if (clientAgreement != null) {
                        agreement.Currency = currency;
                        agreement.Organization = organization;

                        clientAgreement.Client = saleClient;
                        clientAgreement.Agreement = agreement;
                    }

                    sale.Order = order;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    sale.ClientAgreement = clientAgreement;
                    sale.SaleNumber = saleNumber;

                    SaleStatistic saleStatistic = new();
                    saleStatistic.Sale = sale;

                    item.SaleStatistic = saleStatistic;
                    toReturn.Add(item);
                }

                return sale;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new { Ids = documents.Where(s => s.Type == 0).Select(s => s.ID), Culture = culture }
            );
        }

        if (documents.Any(e => e.Type == 1)) {
            string sqlExpression =
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
                "WHERE [SaleReturn].ID IN @Ids";

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
                typeof(VatRate)
            };

            Func<object[], SaleReturn> mapper = objects => {
                SaleReturn saleReturn = (SaleReturn)objects[0];
                Client returnClient = (Client)objects[1];
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
                VatRate vatRate = (VatRate)objects[19];

                decimal vatRatePercent = Convert.ToDecimal(vatRate?.Value ?? 0) / 100;

                if (toReturn.Any(e => e.SaleReturn != null && e.SaleReturn.Id.Equals(saleReturn.Id))) {
                    SaleReturn saleReturnFromList = toReturn.First(e => e.SaleReturn != null && e.SaleReturn.Id.Equals(saleReturn.Id)).SaleReturn;

                    if (saleReturnFromList.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) return saleReturn;

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

                    saleReturnItem.AmountLocal = saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount;

                    if (sale.IsVatSale) {
                        saleReturnItem.VatAmount =
                            decimal.Round(
                                saleReturnItem.Amount * (vatRatePercent / (vatRatePercent + 1)),
                                14,
                                MidpointRounding.AwayFromZero);

                        saleReturnItem.VatAmountLocal =
                            decimal.Round(
                                saleReturnItem.AmountLocal * (vatRatePercent / (vatRatePercent + 1)),
                                14,
                                MidpointRounding.AwayFromZero);
                    }

                    saleReturnFromList.TotalCount += saleReturnItem.Qty;

                    saleReturnFromList.TotalAmount =
                        decimal.Round(saleReturnFromList.TotalAmount + saleReturnItem.Amount, 2, MidpointRounding.AwayFromZero);

                    saleReturnFromList.TotalAmountLocal =
                        decimal.Round(
                            saleReturnFromList.SaleReturnItems.Sum(i => i.AmountLocal),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    saleReturnFromList.SaleReturnItems.Add(saleReturnItem);
                } else {
                    SalesRegisterModel salesRegisterModel = new();

                    if (saleReturnItem != null) {
                        agreement.Pricing = pricing;
                        agreement.Organization = organization;
                        agreement.Currency = currency;

                        clientAgreement.Agreement = agreement;

                        sale.ClientAgreement = clientAgreement;
                        sale.SaleNumber = saleNumber;

                        saleReturn.ClientAgreement = clientAgreement;

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

                        saleReturnItem.AmountLocal = saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount;

                        if (sale.IsVatSale) {
                            saleReturnItem.VatAmount =
                                decimal.Round(
                                    saleReturnItem.Amount * (vatRatePercent / (vatRatePercent + 1)),
                                    14,
                                    MidpointRounding.AwayFromZero);

                            saleReturnItem.VatAmountLocal =
                                decimal.Round(
                                    saleReturnItem.AmountLocal * (vatRatePercent / (vatRatePercent + 1)),
                                    14,
                                    MidpointRounding.AwayFromZero);
                        }

                        saleReturn.TotalCount = saleReturnItem.Qty;

                        saleReturn.TotalAmount = saleReturnItem.Amount;
                        saleReturn.TotalAmountLocal = saleReturnItem.AmountLocal;

                        saleReturn.SaleReturnItems.Add(saleReturnItem);
                    }

                    returnClient.RegionCode = regionCode;

                    saleReturn.Client = returnClient;
                    saleReturn.CreatedBy = returnCreatedBy;

                    salesRegisterModel.SaleReturn = saleReturn;

                    toReturn.Add(salesRegisterModel);
                }

                return saleReturn;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new { Ids = documents.Where(s => s.Type == 1).Select(s => s.ID), Culture = culture }
            );
        }

        toReturn.First().TotalRowsQty = documents.First().TotalRowsQty;

        return toReturn;
    }

    public List<Sale> GetAllByLifeCycleType(SaleLifeCycleType saleLifeCycleType) {
        List<Sale> sales = new();

        string sqlExpression =
            "SELECT * FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderId = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON [Order].Id = OrderItem.OrderId " +
            "LEFT JOIN Product " +
            "ON OrderItem.ProductId = Product.Id " +
            "LEFT JOIN [User] " +
            "ON Sale.UserId = [User].Id " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementId = ClientAgreement.Id " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementId = Agreement.Id " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientId = Client.Id AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusId = BaseLifeCycleStatus.Id " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.Id = Sale.BaseSalePaymentStatusId AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberId = SaleNumber.Id And SaleNumber.Deleted = 0 " +
            "WHERE BaseLifeCycleStatus.SaleLifeCycleType = @SaleLifeCycleType " +
            "AND Sale.Deleted = 0";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Client client = (Client)objects[8];
            RegionCode regionCode = (RegionCode)objects[9];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];
            SaleNumber saleNumber = (SaleNumber)objects[12];

            if (saleNumber != null) sale.SaleNumber = saleNumber;

            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currency != null) agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                sale.ClientAgreement = clientAgreement;
            }

            if (user != null) {
                sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                sale.User = user;
            }

            if (orderItem != null && product != null) {
                orderItem.Product = product;
                order.OrderItems.Add(orderItem);
            }

            if (order != null) sale.Order = order;

            if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

            if (sales.Any(c => c.Id.Equals(sale.Id))) {
                Sale saleFromDb = sales.First(c => c.Id.Equals(sale.Id));

                if (orderItem != null && !saleFromDb.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) saleFromDb.Order.OrderItems.Add(orderItem);
            } else {
                sales.Add(sale);
            }

            return sale;
        };

        var props = new { SaleLifeCycleType = saleLifeCycleType };

        _connection.Query(sqlExpression, types, mapper, props);

        sales.ForEach(s => s.TotalCount = s.Order.OrderItems.Sum(i => i.Qty));

        return sales;
    }

    public IEnumerable<Sale> GetAllExpiredOrLockedSales() {
        return _connection.Query<Sale, SaleNumber, ClientAgreement, Client, RegionCode, ClientUserProfile, Sale>(
            "SELECT [Sale].* " +
            ", [SaleNumber].* " +
            ", [ClientAgreement].* " +
            ", [Client].* " +
            ", [RegionCode].* " +
            ", [ClientUserProfile].* " +
            "FROM [Sale] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "LEFT JOIN [ExpiredBillUserNotification] " +
            "ON [ExpiredBillUserNotification].SaleID = [Sale].ID " +
            "LEFT JOIN [SaleMerged] " +
            "ON [SaleMerged].OutputSaleID = [Sale].ID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "WHERE [Sale].Deleted = 0 " +
            "AND [ExpiredBillUserNotification].ID IS NULL " +
            "AND [SaleMerged].ID IS NULL " +
            "AND (" +
            "(" +
            "[Sale].IsVatSale = 1 " +
            "AND " +
            "[Sale].IsLocked = 1 " +
            ") " +
            "OR " +
            "(" +
            "[BaseLifeCycleStatus].SaleLifeCycleType = 0 " +
            "AND " +
            "[Sale].IsMerged = 1 " +
            "AND " +
            "(" +
            "[SaleBaseShiftStatus].ID IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1" +
            ")" +
            ") " +
            "OR " +
            "(" +
            "[Sale].IsVatSale = 0 " +
            "AND " +
            "[BaseLifeCycleStatus].SaleLifeCycleType = 0 " +
            "AND " +
            "(" +
            "[SaleBaseShiftStatus].ID IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1" +
            ")" +
            ")" +
            ")",
            (sale, saleNumber, clientAgreement, client, regionCode, manager) => {
                if (manager != null) client.ClientManagers.Add(manager);

                client.RegionCode = regionCode;

                clientAgreement.Client = client;

                sale.ClientAgreement = clientAgreement;
                sale.SaleNumber = saleNumber;

                return sale;
            }
        );
    }

    public IEnumerable<Sale> GetAllExpiredOrders() {
        return _connection.Query<Sale>(
            "SELECT [Sale].* FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [Sale].ChangedToInvoice IS NULL " +
            "AND [Order].OrderStatus <> @Status " +
            "AND [Client].OrderExpireDays <= DATEDIFF(day, [Sale].Created, GETUTCDATE()) ",
            new { Status = OrderStatus.Closed });
    }


    public List<Sale> GetAllByUserIds(IEnumerable<long> ids) {
        List<Sale> sales = new();

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in ids) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sql =
            "SELECT * FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN ClientAgreement " +
            "ON [Sale].ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON Agreement.PricingID = AgreementPricing.ID " +
            "LEFT JOIN Pricing AS BasePricing " +
            "ON AgreementPricing.BasePricingID = BasePricing.ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "LEFT JOIN Product " +
            "ON OrderItem.ProductID = Product.ID " +
            "LEFT JOIN ProductPricing " +
            "ON Product.ID = ProductPricing.ProductID " +
            "LEFT JOIN [User] " +
            "ON [Sale].UserID = [User].ID " +
            "WHERE [Sale].Deleted = 0 " +
            "AND [Sale].UserID IN " +
            inExpression;

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Pricing),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(User)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Pricing agreementPricing = (Pricing)objects[4];
            Pricing basePricing = (Pricing)objects[5];
            OrderItem orderItem = (OrderItem)objects[6];
            Product product = (Product)objects[7];
            ProductPricing productPricing = (ProductPricing)objects[8];
            User user = (User)objects[9];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

                if (saleFromList.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                    if (!saleFromList.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id)).Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                        saleFromList.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id)).Product.ProductPricings.Add(productPricing);
                } else {
                    product.ProductPricings.Add(productPricing);

                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                }
            } else {
                product.ProductPricings.Add(productPricing);

                orderItem.Product = product;

                order.OrderItems.Add(orderItem);

                agreementPricing.BasePricing = basePricing;

                agreement.Pricing = agreementPricing;

                clientAgreement.Agreement = agreement;

                sale.Order = order;
                sale.ClientAgreement = clientAgreement;
                sale.User = user;

                sales.Add(sale);
            }

            return sale;
        };

        _connection.Query(sql, types, mapper);

        return sales;
    }

    public IEnumerable<Sale> GetLastPaidSalesByClientAgreementId(long clientAgreementId, DateTime fromDate) {
        List<Sale> sales = new();

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
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
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",Transporter.* " +
            ",SaleMerged.* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [Sale].TransporterID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "WHERE Sale.Deleted = 0 " +
            "AND Sale.IsMerged = 0 " +
            "AND Sale.ClientAgreementId = @ClientAgreementId " +
            "AND Sale.Created >= @FromDate " +
            "AND (SaleBaseShiftStatus.ShiftStatus IS NULL OR SaleBaseShiftStatus.ShiftStatus = 1) " +
            "AND BaseLifeCycleStatus.SaleLifeCycleType > 0 " +
            "AND BaseSalePaymentStatus.SalePaymentStatusType > 0 " +
            "ORDER BY Sale.Created DESC";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(Transporter),
            typeof(SaleMerged)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Order order = (Order)objects[13];
            OrderItem orderItem = (OrderItem)objects[14];
            User orderItemUser = (User)objects[15];
            Product product = (Product)objects[16];
            ProductPricing productPricing = (ProductPricing)objects[17];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[18];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[19];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[20];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[21];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[22];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[23];
            Transporter transporter = (Transporter)objects[24];
            SaleMerged saleMerged = (SaleMerged)objects[25];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (saleMerged != null && !saleFromList.InputSaleMerges.Any(s => s.Id.Equals(saleMerged.Id))) saleFromList.InputSaleMerges.Add(saleMerged);

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);
                }

                sale.Transporter = transporter;
                sale.DeliveryRecipient = deliveryRecipient;
                sale.DeliveryRecipientAddress = deliveryRecipientAddress;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new {
            ClientAgreementId = clientAgreementId,
            FromDate = fromDate,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(sqlExpression, types, mapper, props);

        return sales.Where(s => s.Order.OrderItems.Any());
    }

    public List<Sale> GetAllByClientNetIdFiltered(GetAllSalesByClientNetIdQuery message) {
        List<Sale> sales = new();

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
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
            ",Organization.* " +
            ",VatRate.* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "WHERE Sale.Deleted = 0 " +
            "AND Sale.IsMerged = 0 ";

        if (!message.ClientNetId.Equals(Guid.Empty)) sqlExpression += "AND Client.NetUID = @ClientNetId ";

        if (message.SaleLifeCycleType != null && Enum.IsDefined(typeof(SaleLifeCycleType), message.SaleLifeCycleType))
            sqlExpression += "AND BaseLifeCycleStatus.SaleLifeCycleType = @Type ";

        if (!string.IsNullOrEmpty(message.Value))
            sqlExpression +=
                "AND " +
                "( " +
                "Product.VendorCode like '%' + @Value + '%' " +
                "OR Product.Name like '%' + @Value + '%' " +
                "OR Product.MainOriginalNumber like '%' + @Value + '%' " +
                "OR SaleNumber.Value like '%' + @Value + '%' " +
                ") ";

        if (message.From != null) sqlExpression += "AND Sale.Created >= @From ";

        if (message.To != null) sqlExpression += "AND Sale.Created <= @To ";

        sqlExpression += "ORDER BY Sale.Created DESC";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(Organization),
            typeof(VatRate)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Order order = (Order)objects[13];
            OrderItem orderItem = (OrderItem)objects[14];
            User orderItemUser = (User)objects[15];
            Product product = (Product)objects[16];
            Organization organization = (Organization)objects[17];
            VatRate vatRate = (VatRate)objects[18];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (orderItem == null) return sale;

                if (saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) return sale;

                orderItem.User = orderItemUser;
                orderItem.Product = product;

                orderItem.Qty -= orderItem.ReturnedQty;

                saleFromList.Order.OrderItems.Add(orderItem);
            } else {
                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    organization.VatRate = vatRate;
                    agreement.Organization = organization;
                    agreement.Currency = currency;
                }

                organization.VatRate = vatRate;
                agreement.Organization = organization;
                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    orderItem.Qty -= orderItem.ReturnedQty;

                    order.OrderItems.Add(orderItem);
                }

                sale.Order = order;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new {
            message.ClientNetId,
            message.Value,
            Type = message.SaleLifeCycleType,
            message.From,
            message.To,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(sqlExpression, types, mapper, props);

        Type[] saleTypes = {
            typeof(Sale),
            typeof(OrderItem),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], Sale> saleMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            ProductPricing productPricing = (ProductPricing)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[4];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[5];

            Sale existSale = sales.FirstOrDefault(x => x.Id.Equals(sale.Id));

            if (existSale == null) return sale;

            OrderItem existOrderItem = existSale.Order.OrderItems.FirstOrDefault(x => x.Id.Equals(orderItem.Id));

            if (productGroupDiscount != null && !existSale.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                existSale.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

            if (existOrderItem == null) return sale;

            if (productProductGroup != null) existOrderItem.Product.ProductProductGroups.Add(productProductGroup);

            if (productPricing != null) existOrderItem.Product.ProductPricings.Add(productPricing);

            if (orderItemBaseShiftStatus != null && !existOrderItem.ShiftStatuses.Any(x => x.Id.Equals(orderItemBaseShiftStatus.Id)))
                existOrderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

            return sale;
        };

        _connection.Query(
            "SELECT " +
            "[Sale].*" +
            ",[OrderItem].*" +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",OrderItemBaseShiftStatus.* " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [Sale].[OrderID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[OrderID] = [Order].[ID] " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "AND [OrderItem].[Deleted] = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "WHERE [Sale].[ID] IN @Ids ",
            saleTypes,
            saleMapper,
            new { Ids = sales.Select(x => x.Id) });

        Type[] saleInfoTypes = {
            typeof(Sale),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(Transporter),
            typeof(SaleMerged),
            typeof(SaleBaseShiftStatus)
        };

        Func<object[], Sale> saleInfoMapper = objects => {
            Sale sale = (Sale)objects[0];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[1];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[2];
            Transporter transporter = (Transporter)objects[3];
            SaleMerged saleMerged = (SaleMerged)objects[4];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[5];

            Sale existSale = sales.First(x => x.Id.Equals(sale.Id));

            existSale.Transporter = transporter;
            existSale.DeliveryRecipient = deliveryRecipient;
            existSale.DeliveryRecipientAddress = deliveryRecipientAddress;
            sale.ShiftStatus = saleBaseShiftStatus;

            if (saleMerged != null && !existSale.InputSaleMerges.Any(s => s.Id.Equals(saleMerged.Id))) existSale.InputSaleMerges.Add(saleMerged);

            return sale;
        };

        _connection.Query(
            "SELECT " +
            "[Sale].* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",Transporter.* " +
            ",SaleMerged.* " +
            ",SaleBaseShiftStatus.* " +
            "FROM [Sale] " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [Sale].TransporterID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "WHERE [Sale].[ID] IN @Ids ",
            saleInfoTypes,
            saleInfoMapper,
            new { Ids = sales.Select(x => x.Id) });

        return sales;
    }

    public double GetCalculatedTotalWeightFromConsignmentsBySaleIds(IEnumerable<long> ids) {
        return _connection.Query<double>(
            "SELECT ISNULL( " +
            "SUM([ConsignmentItemMovement].Qty * [ConsignmentItem].[Weight]) " +
            ", 0) " +
            "FROM [Sale] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Sale].OrderID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItemMovement] " +
            "ON [ConsignmentItemMovement].OrderItemID = [OrderItem].ID " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "WHERE [Sale].ID IN @Ids",
            new { Ids = ids }
        ).SingleOrDefault();
    }

    public List<Sale> GetAllByIds(List<long> ids, bool withCalculatedPrices = false) {
        List<Sale> sales = new();

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in ids) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sql =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* ";

        if (withCalculatedPrices) {
            sql +=
                ",[Product].ID " +
                ",[Product].Created " +
                ",[Product].Deleted ";

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                sql += ",[Product].[NameUA] AS [Name] ";
                sql += ",[Product].[DescriptionUA] AS [Description] ";
            } else {
                sql += ",[Product].[NameUA] AS [Name] ";
                sql += ",[Product].[DescriptionUA] AS [Description] ";
            }

            sql +=
                ",[Product].HasAnalogue " +
                ",[Product].HasComponent " +
                ",[Product].HasImage " +
                ",[Product].[Image] " +
                ",[Product].IsForSale " +
                ",[Product].IsForWeb " +
                ",[Product].IsForZeroSale " +
                ",[Product].MainOriginalNumber " +
                ",[Product].MeasureUnitID " +
                ",[Product].NetUID " +
                ",[Product].OrderStandard " +
                ",[Product].PackingStandard " +
                ",[Product].Size " +
                ",[Product].[Top] " +
                ",[Product].UCGFEA " +
                ",[Product].Updated " +
                ",[Product].VendorCode " +
                ",[Product].Volume " +
                ",[Product].[Weight] " +
                ", (CASE " +
                "WHEN [OrderItem].IsFromReSale = 1 " +
                "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
                "END) AS [CurrentPrice] " +
                ", (CASE " +
                "WHEN [OrderItem].IsFromReSale = 1 " +
                "THEN dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
                "END) AS [CurrentLocalPrice] ";
        } else {
            sql += ",Product.* ";
        }

        sql +=
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",CustomersOwnTtn.* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN CustomersOwnTtn " +
            "ON CustomersOwnTtn.ID = Sale.CustomersOwnTtnID " +
            "WHERE Sale.ID IN " +
            inExpression;

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(CustomersOwnTtn)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            Order order = (Order)objects[13];
            OrderItem orderItem = (OrderItem)objects[14];
            User orderItemUser = (User)objects[15];
            Product product = (Product)objects[16];
            ProductPricing productPricing = (ProductPricing)objects[17];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[18];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[19];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[20];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[21];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[22];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[23];
            CustomersOwnTtn customersOwnTtn = (CustomersOwnTtn)objects[24];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);
                }

                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;
                sale.CustomersOwnTtn = customersOwnTtn;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new {
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(sql, types, mapper, props);

        Type[] typesWarehousesShipment = {
            typeof(WarehousesShipment),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], WarehousesShipment> mapperWarehousesShipment = objects => {
            WarehousesShipment Shipment = (WarehousesShipment)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];
            Sale sale = sales.FirstOrDefault(x => x.WarehousesShipmentId.Equals(Shipment.Id));

            if (user != null) Shipment.User = user;

            if (transporter != null) Shipment.Transporter = transporter;

            sale.WarehousesShipment = Shipment;
            return Shipment;
        };

        _connection.Query(
            "SELECT " +
            "[WarehousesShipment].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [WarehousesShipment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [WarehousesShipment].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
            "WHERE [WarehousesShipment].SaleId IN @SaleIDs ",
            typesWarehousesShipment,
            mapperWarehousesShipment,
            new {
                SaleIDs = sales.Select(x => x.Id)
            });
        return sales;
    }

    public List<Sale> GetAllRegisterIvoiceType(DateTime from, DateTime to, string value, long offset, long limit) {
        List<Sale> sales = new();

        string sqlMapper =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "Select [Sale].ID " +
            "From Sale " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [Organization].Culture = @Culture ";
        if (from != DateTime.MinValue && to != DateTime.MinValue)
            sqlMapper += "AND [Sale].ChangedToInvoice >= @From  " +
                         "AND [Sale].ChangedToInvoice <= @To ";

        if (!string.IsNullOrEmpty(value))
            sqlMapper +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', [SaleNumber].Value) > 0  " +
                "OR PATINDEX('%' + @Value + '%', [Client].OriginalRegionCode) > 0 " +
                ") ";

        sqlMapper += "), " +
                     "[Rowed_CTE] " +
                     "AS ( " +
                     "SELECT [Search_CTE].ID " +
                     ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
                     "FROM [Search_CTE] " +
                     ") " +
                     "SELECT * " +
                     "FROM Sale " +
                     "LEFT JOIN [SaleNumber] " +
                     "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                     "LEFT JOIN [ClientAgreement] " +
                     "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
                     "LEFT JOIN [Client] " +
                     "ON [Client].ID = [ClientAgreement].ClientID " +
                     "LEFT JOIN [Agreement] " +
                     "ON [ClientAgreement].AgreementID = [Agreement].ID " +
                     "LEFT JOIN [Organization] " +
                     "ON [Organization].ID = [Agreement].OrganizationID " +
                     "WHERE [Sale].ID IN ( " +
                     "SELECT [Rowed_CTE].ID " +
                     "FROM [Rowed_CTE] ";
        if (limit != 0)
            sqlMapper +=
                "WHERE [Rowed_CTE].RowNumber > @Offset " +
                "AND [Rowed_CTE].RowNumber <= @Limit + @Offset ";

        sqlMapper +=
            ") " +
            "ORDER BY [Sale].ID DESC ";

        Type[] types = {
            typeof(Sale),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Organization)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            SaleNumber saleNumber = (SaleNumber)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Client client = (Client)objects[3];
            Agreement agreement = (Agreement)objects[4];
            Organization organization = (Organization)objects[5];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                sale = sales.First(s => s.Id.Equals(sale.Id));
            } else {
                clientAgreement.Client = client;
                sale.ClientAgreement = clientAgreement;
                sale.SaleNumber = saleNumber;
                sales.Add(sale);
            }

            return sale;
        };

        _connection.Query(sqlMapper, types, mapper,
            new {
                Limit = limit,
                Offset = offset,
                From = from,
                To = to,
                Value = value,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });


        string sqlMapperTotalCount =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [Sale].ID " +
            "FROM Sale " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID  " +
            "WHERE [Organization].Culture = @Culture ";
        if (from != DateTime.MinValue && to != DateTime.MinValue)
            sqlMapperTotalCount +=
                "AND [Sale].ChangedToInvoice >= @From " +
                "AND [Sale].ChangedToInvoice <= @To ";

        if (!string.IsNullOrEmpty(value))
            sqlMapperTotalCount +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', [SaleNumber].Value) > 0  " +
                "OR PATINDEX('%' + @Value + '%', [Client].OriginalRegionCode) > 0 " +
                ") ";

        sqlMapperTotalCount +=
            ") " +
            "SELECT COUNT(*) AS TotalCount " +
            "FROM [Search_CTE] ";

        int totalCount = _connection.ExecuteScalar<int>(
            sqlMapperTotalCount,
            new {
                Limit = limit,
                Offset = offset,
                From = from,
                To = to,
                Value = value,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });
        if (sales.Any()) sales.First().TotalRowsQty = totalCount;

        return sales;
    }

    public List<Sale> GetAllRangedByLifeCycleType(
        int limit,
        int offset,
        long? clientId,
        long[] organizationIds,
        SaleLifeCycleType? saleLifeCycleType,
        DateTime from,
        DateTime to,
        Guid? userNetId = null,
        string value = "",
        bool fromShipments = false,
        Guid? retailClientNetId = null,
        bool forEcommerce = false,
        bool fastEcommerce = false
    ) {
        List<Sale> sales = new();

        IEnumerable<SaleIdsWithTotalRows> saleIds =
            _connection.Query<SaleIdsWithTotalRows>(
                fromShipments
                    ? SALE_IDS_FOR_SHIPMENT_EXPRESSION
                    : BuildGetAllRangedSalesIdsByLifeCycleTypeQuery(value, saleLifeCycleType, organizationIds, fromShipments, forEcommerce, fastEcommerce, clientId,
                        retailClientNetId, userNetId),
                new {
                    SaleLifeCycleType = saleLifeCycleType,
                    From = from,
                    To = to,
                    Value = value,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    UserNetId = userNetId ?? Guid.Empty,
                    Limit = limit,
                    Offset = offset,
                    RetailClientNetId = retailClientNetId,
                    ClientId = clientId,
                    OrganizationIds = organizationIds,
                    OrderClosedStatus = OrderStatus.Closed
                }
            );

        if (!saleIds.Any()) return sales;

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in saleIds.Select(e => e.Id)) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sqlExpression =
            "SELECT [Sale].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[BaseSalePaymentStatus].* " +
            ",[SaleNumber].* " +
            ",[SaleUser].* " +
            ",[SaleUpdateUser].* " +
            ",[ClientAgreement].* " +
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[CurrencyTranslation].* " +
            ",[ExchangeRate].* " +
            ",[Client].* " +
            ",[RegionCode].* " +
            ",[ClientSubClient].* " +
            ",[SubClient].* " +
            ",[SubClientRegionCode].* " +
            ",[Order].* " +
            ",[DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            ",[SaleMerged].* " +
            ",[SaleInvoiceDocument].* " +
            ",[Organization].* " +
            ",[VatRate].* " +
            ",[RetailClient].* " +
            ",[Workplace].* " +
            ",[CustomersOwnTtn].* " +
            "FROM [Sale] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [User] AS [SaleUser] " +
            "ON [SaleUser].ID = [Sale].UserID " +
            "LEFT JOIN [User] AS [SaleUpdateUser] " +
            "ON [SaleUpdateUser].ID = [Sale].UpdateUserID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [CurrencyTranslation] " +
            "ON [CurrencyTranslation].CurrencyID = [Currency].ID " +
            "AND [CurrencyTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ExchangeRate] " +
            "ON [ExchangeRate].CurrencyID = [Currency].ID " +
            "AND [ExchangeRate].Culture = @Culture " +
            "AND [ExchangeRate].Code = 'EUR' " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeId = [RegionCode].ID " +
            "LEFT JOIN [ClientSubClient] " +
            "ON [ClientSubClient].RootClientID = [Client].ID " +
            "AND [ClientSubClient].Deleted = 0 " +
            "LEFT JOIN [Client] AS [SubClient] " +
            "ON [SubClient].ID = [ClientSubClient].SubClientID " +
            "LEFT JOIN [RegionCode] AS [SubClientRegionCode] " +
            "ON [SubClient].RegionCodeID = [SubClientRegionCode].ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN [DeliveryRecipient] " +
            "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
            "LEFT JOIN [SaleMerged] " +
            "ON [SaleMerged].OutputSaleID = Sale.ID " +
            "AND [SaleMerged].Deleted = 0 " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [Sale].RetailClientID " +
            "LEFT JOIN [Workplace] " +
            "ON [Workplace].ID = [Sale].WorkplaceID " +
            "LEFT JOIN [CustomersOwnTtn] " +
            "ON [CustomersOwnTtn].ID = [Sale].CustomersOwnTtnID " +
            "AND [CustomersOwnTtn].Deleted = 0 " +
            "WHERE Sale.ID IN " +
            inExpression +
            "ORDER BY ISNULL([Sale].ChangedToInvoice, Sale.Updated) DESC ";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(SaleInvoiceDocument),
            typeof(Organization),
            typeof(VatRate),
            typeof(RetailClient),
            typeof(Workplace),
            typeof(CustomersOwnTtn)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            User SaleUpdateUser = (User)objects[5];
            ClientAgreement clientAgreement = (ClientAgreement)objects[6];
            Agreement agreement = (Agreement)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            ClientSubClient clientSubClient = (ClientSubClient)objects[13];
            Client subClient = (Client)objects[14];
            RegionCode subClientRegionCode = (RegionCode)objects[15];
            Order order = (Order)objects[16];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[17];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[18];
            SaleMerged saleMerged = (SaleMerged)objects[19];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[20];
            Organization organization = (Organization)objects[21];
            VatRate vatRate = (VatRate)objects[22];
            RetailClient retailClient = (RetailClient)objects[23];
            Workplace workplace = (Workplace)objects[24];
            CustomersOwnTtn customersOwnTtn = (CustomersOwnTtn)objects[25];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                sale = sales.First(s => s.Id.Equals(sale.Id));
            } else {
                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                    organization.VatRate = vatRate;
                    agreement.Organization = organization;
                }

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                if (customersOwnTtn != null)
                    sale.CustomersOwnTtn = customersOwnTtn;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
                organization.VatRate = vatRate;
                agreement.Organization = organization;

                sale.Order = order;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.SaleInvoiceDocument = saleInvoiceDocument;
                sale.User = saleUser;
                sale.UpdateUser = SaleUpdateUser;
                sale.Workplace = workplace;

                if (retailClient != null) {
                    retailClient.ShoppingCartJson = string.Empty;
                    sale.RetailClient = retailClient;
                }

                sales.Add(sale);
            }

            if (saleMerged != null && !sale.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) sale.InputSaleMerges.Add(saleMerged);

            if (clientSubClient == null || sale.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id)) || subClient == null) return sale;

            subClient.RegionCode = subClientRegionCode;

            clientSubClient.SubClient = subClient;

            sale.ClientAgreement.Client.SubClients.Add(clientSubClient);

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        foreach (Sale item in sales) item.TotalRowsQty = saleIds.First().TotalRowsQty;

        string sqlQuerySale =
            "SELECT " +
            "[Sale].* " +
            ",[SaleBaseShiftStatus].* " +
            ",[Transporter].* " +
            ",[TransporterType].* " +
            ",[TransporterTypeTranslation].* " +
            ",[RootClient].* " +
            ",[RootRegionCode].* " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].[ID] = [Sale].[BaseLifeCycleStatusID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Transporter] " +
            "ON [Sale].TransporterID = [Transporter].ID " +
            "LEFT JOIN [TransporterType] " +
            "ON [Transporter].TransporterTypeID = [TransporterType].ID " +
            "LEFT JOIN [TransporterTypeTranslation] " +
            "ON [TransporterTypeTranslation].TransporterTypeID = [TransporterType].ID " +
            "AND [TransporterTypeTranslation].Deleted = 0 " +
            "AND [TransporterTypeTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ClientSubClient] AS [RootClientSubClient] " +
            "ON [RootClientSubClient].SubClientID = [ClientAgreement].[ClientID] " +
            "AND [RootClientSubClient].Deleted = 0 " +
            "LEFT JOIN [Client] AS [RootClient] " +
            "ON [RootClient].ID = [RootClientSubClient].RootClientID " +
            "LEFT JOIN [RegionCode] AS [RootRegionCode] " +
            "ON [RootRegionCode].ID = [RootClient].RegionCodeID " +
            "WHERE Sale.ID IN @Ids ";

        Type[] saleTypes = {
            typeof(Sale),
            typeof(SaleBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], Sale> saleMappers = objects => {
            Sale sale = (Sale)objects[0];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[1];
            Transporter transporter = (Transporter)objects[2];
            TransporterType transporterType = (TransporterType)objects[3];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[4];
            Client rootClient = (Client)objects[5];
            RegionCode rootRegionCode = (RegionCode)objects[6];

            Sale existSale = sales.First(x => x.Id.Equals(sale.Id));

            if (rootClient != null) {
                rootClient.RegionCode = rootRegionCode;

                existSale.ClientAgreement.Client.RootClient = rootClient;
            }

            if (transporterType != null) {
                transporterType.Name = transporterTypeTranslation.Name;

                transporter.TransporterType = transporterType;
            }

            existSale.Transporter = transporter;
            existSale.ShiftStatus = saleBaseShiftStatus;

            return sale;
        };

        _connection.Query(sqlQuerySale, saleTypes, saleMappers,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = sales.Select(x => x.Id)
            });

        if (!sales.Any()) return sales;

        string sqlQueryOrderItems =
            "SELECT " +
            "[OrderItem].* " +
            ",[OrderItemUser].* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlQueryOrderItems += ",[Product].[NameUA] AS [Name] ";
            sqlQueryOrderItems += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlQueryOrderItems += ",[Product].[NameUA] AS [Name] ";
            sqlQueryOrderItems += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlQueryOrderItems +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
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
            ",[OrderItemBaseShiftStatus].* " +
            ",[DiscountUpdatedUser].* " +
            ",[ProductSpecification].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT  JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [User] AS [OrderItemUser] " +
            "ON [OrderItemUser].ID = [OrderItem].UserID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderItem].[AssignedSpecificationID] " +
            "WHERE [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            //"AND [OrderItem].Qty > 0 " +
            "AND [Sale].[ID] IN @Ids";

        Type[] orderItemsTypes = {
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(OrderItemBaseShiftStatus),
            typeof(User),
            typeof(ProductSpecification)
        };

        Func<object[], OrderItem> orderItemMappers = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            User orderItemUser = (User)objects[1];
            Product product = (Product)objects[2];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[3];
            User discountUpdatedBy = (User)objects[4];
            ProductSpecification assignedSpecification = (ProductSpecification)objects[5];

            Sale sale = sales.First(x => x.OrderId.Equals(orderItem.OrderId));

            if (sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                orderItem = sale.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id));
            } else {
                orderItem.User = orderItemUser;
                orderItem.DiscountUpdatedBy = discountUpdatedBy;
                orderItem.Product = product;

                orderItem.AssignedSpecification = assignedSpecification;

                sale.Order.OrderItems.Add(orderItem);
            }

            if (orderItemBaseShiftStatus == null || orderItem.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) return orderItem;

            orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

            return orderItem;
        };

        _connection.Query(
            sqlQueryOrderItems,
            orderItemsTypes,
            orderItemMappers,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = sales.Select(x => x.Id)
            });

        types = new[] {
            typeof(Sale),
            typeof(Order),
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderPackage orderPackage = (OrderPackage)objects[2];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[3];
            User user = (User)objects[4];
            UserRole userRole = (UserRole)objects[5];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];

            if (orderPackage == null) return null;

            Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

            if (!saleFromList.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleFromList.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleFromList.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        string packagesExpression =
            "SELECT [Sale].ID " +
            ",[Order].ID " +
            ",[OrderPackage].* " +
            ",[OrderPackageUser].* " +
            ",[User].* " +
            ",[UserRole].* " +
            ",[OrderPackageItem].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        packagesExpression +=
            ", [Product].HasAnalogue " +
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
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderPackage] " +
            "ON [OrderPackage].OrderID = [Order].ID " +
            "AND [OrderPackage].Deleted = 0 " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Sale].ID IN " +
            inExpression;

        _connection.Query(
            packagesExpression,
            types,
            packagesMapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        Type[] typesUpdateDataCarrier = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapperUpdateDataCarrier = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];
            Sale saleFromList = sales.First(c => c.Id.Equals(updateDataCarrier.SaleId));

            if (user != null) updateDataCarrier.User = user;

            if (transporter != null) updateDataCarrier.Transporter = transporter;

            if (!updateDataCarrier.IsDevelopment)
                saleFromList.IsDevelopment = false;
            else
                saleFromList.IsDevelopment = true;

            saleFromList.UpdateDataCarrier.Add(updateDataCarrier);
            return updateDataCarrier;
        };

        _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "WHERE [UpdateDataCarrier].SaleId IN @Ids " +
            "AND [UpdateDataCarrier].IsEditTransporter = 0",
            typesUpdateDataCarrier,
            mapperUpdateDataCarrier,
            new {
                Ids = sales.Select(x => x.Id)
            });


        Type[] typesShipmentListItem = {
            typeof(ShipmentListItem),
            typeof(ShipmentList)
        };

        Func<object[], ShipmentListItem> mapperShipmentListItem = objects => {
            ShipmentListItem shipmentListItem = (ShipmentListItem)objects[0];
            ShipmentList shipmentList = (ShipmentList)objects[1];
            Sale saleFromList = sales.First(c => c.Id.Equals(shipmentListItem.SaleId));

            saleFromList.IsSent = shipmentList.IsSent;

            return shipmentListItem;
        };

        _connection.Query(
            "SELECT  * " +
            "FROM [ShipmentListItem] " +
            "LEFT JOIN [ShipmentList] " +
            "ON [ShipmentList].ID = ShipmentListItem.ShipmentListID " +
            "WHERE [ShipmentListItem].SaleID IN @Ids ",
            typesShipmentListItem,
            mapperShipmentListItem,
            new {
                Ids = sales.Select(x => x.Id)
            });

        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];

            Sale saleFromList = sales.First(c => c.Id.Equals(historyInvoice.SaleId));

            if (!saleFromList.HistoryInvoiceEdit.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                saleFromList.HistoryInvoiceEdit.Add(historyInvoice);
                if (!historyInvoice.IsDevelopment)
                    saleFromList.IsDevelopment = false;
                else
                    saleFromList.IsDevelopment = true;
            } else {
                HistoryInvoiceEdit historyinvoiceEditFromList = saleFromList.HistoryInvoiceEdit.First(s => s.Id.Equals(historyInvoice.Id));

                historyinvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                if (!historyinvoiceEditFromList.IsDevelopment)
                    saleFromList.IsDevelopment = false;
                else
                    saleFromList.IsDevelopment = true;
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditID = HistoryInvoiceEdit.ID " +
            "WHERE HistoryInvoiceEdit.SaleID IN @Ids " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new { Ids = sales.Select(x => x.Id) }
        );

        return sales;
    }

    public List<Sale> GetAllRanged(
        DateTime from,
        DateTime to,
        SaleLifeCycleType status = SaleLifeCycleType.Packaging
    ) {
        List<Sale> sales = new();

        IEnumerable<SaleIdsWithTotalRows> saleIds =
            _connection.Query<SaleIdsWithTotalRows>(
                BuildGetAllRanged(),
                new {
                    From = from,
                    To = to
                }
            );

        if (!saleIds.Any()) return sales;

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in saleIds.Select(e => e.Id)) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sqlExpression =
            "SELECT [Sale].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[BaseSalePaymentStatus].* " +
            ",[SaleNumber].* " +
            ",[SaleUser].* " +
            ",[SaleUpdateUser].* " +
            ",[ClientAgreement].* " +
            ",[Agreement].* " +
            ",[Pricing].* " +
            ",[Currency].* " +
            ",[CurrencyTranslation].* " +
            ",[ExchangeRate].* " +
            ",[Client].* " +
            ",[ClientInRole].* " +
            ",[ClientTypeRole].* " +
            ",[ClientTypeRoleTranslation].* " +
            ",[Region].* " +
            ",[RegionCode].* " +
            ",[ClientSubClient].* " +
            ",[SubClient].* " +
            ",[SubClientRegionCode].* " +
            ",[Order].* " +
            ",[DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            ",[SaleMerged].* " +
            ",[SaleInvoiceDocument].* " +
            ",[Organization].* " +
            ",[VatRate].* " +
            ",[RetailClient].* " +
            ",[Workplace].* " +
            ",[CustomersOwnTtn].* " +
            "FROM [Sale] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [User] AS [SaleUser] " +
            "ON [SaleUser].ID = [Sale].UserID " +
            "LEFT JOIN [User] AS [SaleUpdateUser] " +
            "ON [SaleUpdateUser].ID = [Sale].UpdateUserID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [CurrencyTranslation] " +
            "ON [CurrencyTranslation].CurrencyID = [Currency].ID " +
            "AND [CurrencyTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ExchangeRate] " +
            "ON [ExchangeRate].CurrencyID = [Currency].ID " +
            "AND [ExchangeRate].Culture = @Culture " +
            "AND [ExchangeRate].Code = 'EUR' " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN ClientInRole " +
            "ON ClientInRole.ClientID = Client.ID " +
            "LEFT JOIN ClientTypeRole " +
            "ON ClientTypeRole.ID = ClientInRole.ClientTypeRoleID " +
            "LEFT JOIN ClientTypeRoleTranslation " +
            "ON ClientTypeRoleTranslation.ClientTypeRoleID = ClientTypeRole.ID " +
            "AND ClientTypeRoleTranslation.CultureCode = 'uk' " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [Client].RegionID " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeId = [RegionCode].ID " +
            "LEFT JOIN [ClientSubClient] " +
            "ON [ClientSubClient].RootClientID = [Client].ID " +
            "AND [ClientSubClient].Deleted = 0 " +
            "LEFT JOIN [Client] AS [SubClient] " +
            "ON [SubClient].ID = [ClientSubClient].SubClientID " +
            "LEFT JOIN [RegionCode] AS [SubClientRegionCode] " +
            "ON [SubClient].RegionCodeID = [SubClientRegionCode].ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN [DeliveryRecipient] " +
            "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
            "LEFT JOIN [SaleMerged] " +
            "ON [SaleMerged].OutputSaleID = Sale.ID " +
            "AND [SaleMerged].Deleted = 0 " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [Sale].RetailClientID " +
            "LEFT JOIN [Workplace] " +
            "ON [Workplace].ID = [Sale].WorkplaceID " +
            "LEFT JOIN [CustomersOwnTtn] " +
            "ON [CustomersOwnTtn].ID = [Sale].CustomersOwnTtnID " +
            "AND [CustomersOwnTtn].Deleted = 0 " +
            "WHERE Sale.ID IN " +
            inExpression +
            (status.Equals(SaleLifeCycleType.New) ? "AND [BaseLifeCycleStatus].SaleLifeCycleType = 0 " : "AND [BaseLifeCycleStatus].SaleLifeCycleType <> 0") +
            "AND [Order].OrderStatus <> @OrderStatus " +
            "ORDER BY ISNULL([Sale].ChangedToInvoice, Sale.Updated) DESC ";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(Region),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(SaleInvoiceDocument),
            typeof(Organization),
            typeof(VatRate),
            typeof(RetailClient),
            typeof(Workplace),
            typeof(CustomersOwnTtn)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            User SaleUpdateUser = (User)objects[5];
            ClientAgreement clientAgreement = (ClientAgreement)objects[6];
            Agreement agreement = (Agreement)objects[7];
            Pricing pricing = (Pricing)objects[8];
            Currency currency = (Currency)objects[9];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[10];
            ExchangeRate exchangeRate = (ExchangeRate)objects[11];
            Client client = (Client)objects[12];
            ClientInRole clientInRole = (ClientInRole)objects[13];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[14];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[15];
            Region region = (Region)objects[16];
            RegionCode regionCode = (RegionCode)objects[17];
            ClientSubClient clientSubClient = (ClientSubClient)objects[18];
            Client subClient = (Client)objects[19];
            RegionCode subClientRegionCode = (RegionCode)objects[20];
            Order order = (Order)objects[21];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[22];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[23];
            SaleMerged saleMerged = (SaleMerged)objects[24];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[25];
            Organization organization = (Organization)objects[26];
            VatRate vatRate = (VatRate)objects[27];
            RetailClient retailClient = (RetailClient)objects[28];
            Workplace workplace = (Workplace)objects[29];
            CustomersOwnTtn customersOwnTtn = (CustomersOwnTtn)objects[30];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                sale = sales.First(s => s.Id.Equals(sale.Id));
            } else {
                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                    organization.VatRate = vatRate;
                    agreement.Organization = organization;
                }

                if (pricing != null) agreement.Pricing = pricing;

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                if (clientInRole != null)
                    if (clientTypeRole != null) {
                        clientTypeRole.ClientTypeRoleTranslations.Add(clientTypeRoleTranslation);
                        clientInRole.ClientTypeRole = clientTypeRole;
                    }

                if (customersOwnTtn != null)
                    sale.CustomersOwnTtn = customersOwnTtn;

                client.Region = region;
                client.RegionCode = regionCode;
                client.ClientInRole = clientInRole;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
                organization.VatRate = vatRate;
                agreement.Organization = organization;

                sale.Order = order;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.SaleInvoiceDocument = saleInvoiceDocument;
                sale.User = saleUser;
                sale.UpdateUser = SaleUpdateUser;
                sale.Workplace = workplace;

                if (retailClient != null) {
                    retailClient.ShoppingCartJson = string.Empty;
                    sale.RetailClient = retailClient;
                }

                sales.Add(sale);
            }

            if (saleMerged != null && !sale.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) sale.InputSaleMerges.Add(saleMerged);

            if (clientSubClient == null || sale.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id)) || subClient == null) return sale;

            subClient.RegionCode = subClientRegionCode;

            clientSubClient.SubClient = subClient;

            sale.ClientAgreement.Client.SubClients.Add(clientSubClient);

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, OrderStatus = OrderStatus.Closed };

        _connection.Query(sqlExpression, types, mapper, props);

        if (!sales.Any()) return sales;

        sales.First().TotalRowsQty = saleIds.First().TotalRowsQty;

        string sqlQuerySale =
            "SELECT " +
            "[Sale].* " +
            ",[SaleBaseShiftStatus].* " +
            ",[Transporter].* " +
            ",[TransporterType].* " +
            ",[TransporterTypeTranslation].* " +
            ",[RootClient].* " +
            ",[RootRegionCode].* " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].[ID] = [Sale].[BaseLifeCycleStatusID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Transporter] " +
            "ON [Sale].TransporterID = [Transporter].ID " +
            "LEFT JOIN [TransporterType] " +
            "ON [Transporter].TransporterTypeID = [TransporterType].ID " +
            "LEFT JOIN [TransporterTypeTranslation] " +
            "ON [TransporterTypeTranslation].TransporterTypeID = [TransporterType].ID " +
            "AND [TransporterTypeTranslation].Deleted = 0 " +
            "AND [TransporterTypeTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ClientSubClient] AS [RootClientSubClient] " +
            "ON [RootClientSubClient].SubClientID = [ClientAgreement].[ClientID] " +
            "AND [RootClientSubClient].Deleted = 0 " +
            "LEFT JOIN [Client] AS [RootClient] " +
            "ON [RootClient].ID = [RootClientSubClient].RootClientID " +
            "LEFT JOIN [RegionCode] AS [RootRegionCode] " +
            "ON [RootRegionCode].ID = [RootClient].RegionCodeID " +
            "WHERE Sale.ID IN @Ids ";

        Type[] saleTypes = {
            typeof(Sale),
            typeof(SaleBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], Sale> saleMappers = objects => {
            Sale sale = (Sale)objects[0];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[1];
            Transporter transporter = (Transporter)objects[2];
            TransporterType transporterType = (TransporterType)objects[3];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[4];
            Client rootClient = (Client)objects[5];
            RegionCode rootRegionCode = (RegionCode)objects[6];

            Sale existSale = sales.First(x => x.Id.Equals(sale.Id));

            if (rootClient != null) {
                rootClient.RegionCode = rootRegionCode;

                existSale.ClientAgreement.Client.RootClient = rootClient;
            }

            if (transporterType != null) {
                if (transporterTypeTranslation != null) transporterType.Name = transporterTypeTranslation.Name;

                transporter.TransporterType = transporterType;
            }

            existSale.Transporter = transporter;
            existSale.ShiftStatus = saleBaseShiftStatus;

            return sale;
        };

        _connection.Query(sqlQuerySale, saleTypes, saleMappers,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = sales.Select(x => x.Id)
            });

        if (!sales.Any()) return sales;

        string sqlQueryOrderItems =
            "SELECT " +
            "[OrderItem].* " +
            ",[OrderItemUser].* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlQueryOrderItems += ",[Product].[NameUA] AS [Name] ";
            sqlQueryOrderItems += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlQueryOrderItems += ",[Product].[NameUA] AS [Name] ";
            sqlQueryOrderItems += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlQueryOrderItems +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
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
            ",[MeasureUnit].* " +
            ",[OrderItemBaseShiftStatus].* " +
            ",[DiscountUpdatedUser].* " +
            ",[ProductSpecification].* " +
            ",[Storage].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT  JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [User] AS [OrderItemUser] " +
            "ON [OrderItemUser].ID = [OrderItem].UserID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderItem].[AssignedSpecificationID] " +
            "LEFT JOIN ProductReservation " +
            "ON ProductReservation.OrderItemID = OrderItem.ID " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ID = ProductReservation.ProductAvailabilityID " +
            "LEFT JOIN Storage " +
            "ON Storage.ID = ProductAvailability.StorageID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "WHERE [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty > 0 " +
            "AND [Sale].[ID] IN @Ids";

        Type[] orderItemsTypes = {
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(OrderItemBaseShiftStatus),
            typeof(User),
            typeof(ProductSpecification),
            typeof(Storage),
            typeof(ProductProductGroup),
            typeof(ProductGroup)
        };

        Func<object[], OrderItem> orderItemMappers = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            User orderItemUser = (User)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[4];
            User discountUpdatedBy = (User)objects[5];
            ProductSpecification assignedSpecification = (ProductSpecification)objects[6];
            Storage storage = (Storage)objects[7];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[8];
            ProductGroup productGroup = (ProductGroup)objects[9];

            Sale sale = sales.First(x => x.OrderId.Equals(orderItem.OrderId));

            if (sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                Product productInList = sale.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).Product;
                if (productProductGroup != null && !productInList.ProductProductGroups.Any(o => o.Id.Equals(productProductGroup.Id)))
                    productInList.ProductProductGroups.Add(productProductGroup);

                orderItem = sale.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id));
            } else {
                if (productProductGroup != null && productGroup != null) {
                    productProductGroup.ProductGroup = productGroup;
                    product.ProductProductGroups.Add(productProductGroup);
                }

                orderItem.User = orderItemUser;
                orderItem.DiscountUpdatedBy = discountUpdatedBy;
                product.MeasureUnit = measureUnit;
                orderItem.Product = product;
                orderItem.Storage = storage;

                orderItem.AssignedSpecification = assignedSpecification;

                sale.Order.OrderItems.Add(orderItem);
            }

            if (orderItemBaseShiftStatus == null || orderItem.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) return orderItem;

            orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

            return orderItem;
        };

        _connection.Query(
            sqlQueryOrderItems,
            orderItemsTypes,
            orderItemMappers,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = sales.Select(x => x.Id)
            });

        types = new[] {
            typeof(Sale),
            typeof(Order),
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderPackage orderPackage = (OrderPackage)objects[2];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[3];
            User user = (User)objects[4];
            UserRole userRole = (UserRole)objects[5];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];

            if (orderPackage == null) return null;

            Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

            if (!saleFromList.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleFromList.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleFromList.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        string packagesExpression =
            "SELECT [Sale].ID " +
            ",[Order].ID " +
            ",[OrderPackage].* " +
            ",[OrderPackageUser].* " +
            ",[User].* " +
            ",[UserRole].* " +
            ",[OrderPackageItem].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        packagesExpression +=
            ", [Product].HasAnalogue " +
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
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderPackage] " +
            "ON [OrderPackage].OrderID = [Order].ID " +
            "AND [OrderPackage].Deleted = 0 " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Sale].ID IN " +
            inExpression;

        _connection.Query(
            packagesExpression,
            types,
            packagesMapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        Type[] typesUpdateDataCarrier = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapperUpdateDataCarrier = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];
            Sale saleFromList = sales.First(c => c.Id.Equals(updateDataCarrier.SaleId));

            if (user != null) updateDataCarrier.User = user;

            if (transporter != null) updateDataCarrier.Transporter = transporter;

            if (!updateDataCarrier.IsDevelopment)
                saleFromList.IsDevelopment = false;
            else
                saleFromList.IsDevelopment = true;

            saleFromList.UpdateDataCarrier.Add(updateDataCarrier);
            return updateDataCarrier;
        };

        _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "WHERE [UpdateDataCarrier].SaleId IN @Ids " +
            "AND [UpdateDataCarrier].IsEditTransporter = 0",
            typesUpdateDataCarrier,
            mapperUpdateDataCarrier,
            new {
                Ids = sales.Select(x => x.Id)
            });


        Type[] typesShipmentListItem = {
            typeof(ShipmentListItem),
            typeof(ShipmentList)
        };

        Func<object[], ShipmentListItem> mapperShipmentListItem = objects => {
            ShipmentListItem shipmentListItem = (ShipmentListItem)objects[0];
            ShipmentList shipmentList = (ShipmentList)objects[1];
            Sale saleFromList = sales.First(c => c.Id.Equals(shipmentListItem.SaleId));

            saleFromList.IsSent = shipmentList.IsSent;

            return shipmentListItem;
        };

        _connection.Query(
            "SELECT  * " +
            "FROM [ShipmentListItem] " +
            "LEFT JOIN [ShipmentList] " +
            "ON [ShipmentList].ID = ShipmentListItem.ShipmentListID " +
            "WHERE [ShipmentListItem].SaleID IN @Ids ",
            typesShipmentListItem,
            mapperShipmentListItem,
            new {
                Ids = sales.Select(x => x.Id)
            });

        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];

            Sale saleFromList = sales.First(c => c.Id.Equals(historyInvoice.SaleId));

            if (!saleFromList.HistoryInvoiceEdit.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                saleFromList.HistoryInvoiceEdit.Add(historyInvoice);
                if (!historyInvoice.IsDevelopment)
                    saleFromList.IsDevelopment = false;
                else
                    saleFromList.IsDevelopment = true;
            } else {
                HistoryInvoiceEdit historyinvoiceEditFromList = saleFromList.HistoryInvoiceEdit.First(s => s.Id.Equals(historyInvoice.Id));

                historyinvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                if (!historyinvoiceEditFromList.IsDevelopment)
                    saleFromList.IsDevelopment = false;
                else
                    saleFromList.IsDevelopment = true;
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditID = HistoryInvoiceEdit.ID " +
            "WHERE HistoryInvoiceEdit.SaleID IN @Ids " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new { Ids = sales.Select(x => x.Id) }
        );

        return sales;
    }

    public Sale GetGroupedOrderItemByProduct(Guid orderItemNetId) {
        Sale saleToReturn = null;

        long productId = _connection.QueryFirst<long>(
            "SELECT [ProductID] FROM OrderItem WHERE NetUID = @NetId "
            , new { NetId = orderItemNetId });

        long saleId = _connection.QueryFirst<long>(
            "SELECT [Sale].ID FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "WHERE OrderItem.NetUID = @NetId "
            , new { NetId = orderItemNetId });

        string sqlExpression =
            "SELECT Sale.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression += ",[Product].HasAnalogue " +
                         ",[Product].HasComponent " +
                         ",[Product].HasImage " +
                         ",[Product].[Image] " +
                         ",[Product].IsForSale " +
                         ",[Product].IsForWeb " +
                         ",[Product].IsForZeroSale " +
                         ",[Product].MainOriginalNumber " +
                         ",[Product].MeasureUnitID " +
                         ",[Product].NetUID " +
                         ",[Product].OrderStandard " +
                         ",[Product].PackingStandard " +
                         ",[Product].Size " +
                         ",[Product].[Top] " +
                         ",[Product].UCGFEA " +
                         ",[Product].Updated " +
                         ",[Product].VendorCode " +
                         ",[Product].Volume " +
                         ",[Product].[Weight] " +
                         ",[ProductReservation].* " +
                         ",[ProductAvailability].* " +
                         ",[Storage].* " +
                         ",[StorageOrganization].* " +
                         ",[ReSaleAvailability].* " +
                         "FROM Sale " +
                         "LEFT JOIN [Order] " +
                         "ON Sale.OrderID = [Order].ID " +
                         "LEFT JOIN OrderItem " +
                         "ON OrderItem.OrderID = [Order].ID " +
                         "AND OrderItem.Deleted = 0 " +
                         "AND OrderItem.Qty > 0 " +
                         "AND OrderItem.ProductID = @ProductId " +
                         "LEFT JOIN Product " +
                         "ON Product.ID = OrderItem.ProductID " +
                         "LEFT JOIN [ProductReservation] " +
                         "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
                         "AND [ProductReservation].Deleted = 0 " +
                         "LEFT JOIN [ProductAvailability] " +
                         "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
                         "LEFT JOIN [Storage] " +
                         "ON [Storage].ID = [ProductAvailability].StorageID " +
                         "LEFT JOIN [views].[OrganizationView] AS [StorageOrganization] " +
                         "ON [StorageOrganization].ID = [Storage].OrganizationID " +
                         "AND [StorageOrganization].CultureCode = @Culture " +
                         "LEFT JOIN [ReSaleAvailability] " +
                         "ON [ReSaleAvailability].ProductReservationID = [ProductReservation].ID " +
                         "AND [ReSaleAvailability].Deleted = 0 " +
                         "WHERE [Sale].ID = @SaleId " +
                         "ORDER BY Sale.Created DESC, Storage.ForVatProducts DESC; ";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductReservation),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Organization),
            typeof(ReSaleAvailability)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ProductReservation reservation = (ProductReservation)objects[4];
            ProductAvailability reservationAvailability = (ProductAvailability)objects[5];
            Storage reservationAvailabilityStorage = (Storage)objects[6];
            Organization reservationAvailabilityStorageOrganization = (Organization)objects[7];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[8];

            if (orderItem == null) return sale;

            if (saleToReturn == null) {
                orderItem.Product = product;
                orderItem.Qty -= orderItem.ReturnedQty;
                order.OrderItems.Add(orderItem);
                sale.Order = order;

                saleToReturn = sale;

                if (reservation == null) return sale;

                reservationAvailabilityStorage.Organization = reservationAvailabilityStorageOrganization;

                reservationAvailability.Storage = reservationAvailabilityStorage;

                reservation.ProductAvailability = reservationAvailability;
                reservation.ReSaleAvailabilities.Add(reSaleAvailability);

                orderItem.ProductReservations.Add(reservation);
            } else {
                if (saleToReturn.Order.OrderItems.Any(e => e.Id.Equals(orderItem.Id))) {
                    orderItem = saleToReturn.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id));
                } else {
                    if (!saleToReturn.Order.OrderItems.Any(x => x.ProductId.Equals(product.Id))) {
                        orderItem.Qty -= orderItem.ReturnedQty;
                        saleToReturn.Order.OrderItems.Add(orderItem);
                    } else {
                        OrderItem orderItemGroupByProduct = saleToReturn.Order.OrderItems.First(x => x.ProductId.Equals(product.Id));

                        if (orderItemGroupByProduct.OrderItemsGroupByProduct == null)
                            orderItemGroupByProduct.OrderItemsGroupByProduct = new List<OrderItem>();

                        orderItemGroupByProduct.Qty += orderItem.Qty;
                        orderItemGroupByProduct.Qty -= orderItem.ReturnedQty;
                        orderItemGroupByProduct.ReturnedQty += orderItem.ReturnedQty;

                        orderItemGroupByProduct.OrderItemsGroupByProduct.Add(orderItem);

                        orderItem = orderItemGroupByProduct;
                    }
                }

                if (reservation == null || orderItem.ProductReservations.Any(r => r.Id.Equals(reservation.Id))) return sale;

                reservationAvailabilityStorage.Organization = reservationAvailabilityStorageOrganization;

                reservationAvailability.Storage = reservationAvailabilityStorage;

                reservation.ProductAvailability = reservationAvailability;

                if (!reservation.ReSaleAvailabilities.Any(r => r.Id.Equals(reSaleAvailability.Id))) reservation.ReSaleAvailabilities.Add(reSaleAvailability);

                orderItem.ProductReservations.Add(reservation);
            }

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, SaleId = saleId, ProductId = productId };

        _connection.Query(sqlExpression, types, mapper, props, commandTimeout: 3600);

        return saleToReturn;
    }

    public List<Sale> GetAllSalesForReturnsFromSearch(
        DateTime from,
        DateTime to,
        string value,
        Guid netId,
        Guid? organizationNetId) {
        List<Sale> sales = new();

        string idsSqlExpression =
            "SELECT [Sale].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Order] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty > 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [Sale].Updated >= @From " +
            "AND [Sale].Updated <= @To " +
            "AND [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [Sale].ChangedToInvoice IS NOT NULL " +
            "AND [Organization].Culture = @Culture " +
            "AND PATINDEX(@Value, [Product].SearchVendorCode) > 0 ";

        if (!netId.Equals(Guid.Empty)) idsSqlExpression += "AND [Client].NetUID = @NetId ";

        idsSqlExpression +=
            "GROUP BY [Sale].ID, [Sale].Created " +
            "ORDER BY [Sale].Created DESC";

        IEnumerable<long> saleIds = _connection.Query<long>(
            idsSqlExpression,
            new {
                From = from,
                To = to,
                Value = $"%{value}%",
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                NetId = netId
            }
        );

        if (!saleIds.Any()) return sales;

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",ClientSubClient.* " +
            ",SubClient.* " +
            ",SubClientRegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
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
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",Transporter.* " +
            ",TransporterType.* " +
            ",TransporterTypeTranslation.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",SaleMerged.* " +
            ",[DiscountUpdatedUser].* " +
            ",[SaleInvoiceDocument].* " +
            ",[RootClient].* " +
            ",[RootRegionCode].* " +
            ",[ProductReservation].*  " +
            ",[ProductAvailability].*  " +
            ",[Storage].* " +
            ",[StorageOrganization].* " +
            ",[Organization].* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID " +
            "AND ClientSubClient.Deleted = 0 " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "LEFT JOIN RegionCode AS SubClientRegionCode " +
            "ON SubClient.RegionCodeId = SubClientRegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN Transporter " +
            "ON Sale.TransporterID = Transporter.ID " +
            "LEFT JOIN TransporterType " +
            "ON Transporter.TransporterTypeID = TransporterType.ID " +
            "LEFT JOIN TransporterTypeTranslation " +
            "ON TransporterTypeTranslation.TransporterTypeID = TransporterType.ID " +
            "AND TransporterTypeTranslation.Deleted = 0 " +
            "AND TransporterTypeTranslation.CultureCode = @Culture " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "LEFT JOIN [ClientSubClient] AS [RootClientSubClient] " +
            "ON [RootClientSubClient].SubClientID = [Client].ID " +
            "AND [RootClientSubClient].Deleted = 0 " +
            "LEFT JOIN [Client] AS [RootClient] " +
            "ON [RootClient].ID = [RootClientSubClient].RootClientID " +
            "LEFT JOIN [RegionCode] AS [RootRegionCode] " +
            "ON [RootRegionCode].ID = [RootClient].RegionCodeID " +
            "LEFT JOIN [ProductReservation] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "AND [ProductReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [views].[OrganizationView] AS [StorageOrganization] " +
            "ON [StorageOrganization].ID = [Storage].OrganizationID " +
            "AND [StorageOrganization].CultureCode = @Culture " +
            "WHERE Sale.ID IN @SaleIds " +
            (organizationNetId.HasValue ? "AND [Organization].[NetUid] = @OrganizationNetId " : string.Empty) +
            "ORDER BY Sale.Created DESC, Storage.ForVatProducts DESC";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(User),
            typeof(SaleInvoiceDocument),
            typeof(Client),
            typeof(RegionCode),
            typeof(ProductReservation),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Organization),
            typeof(Organization)
        };
        List<(long, long)> values = new();
        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            ClientSubClient clientSubClient = (ClientSubClient)objects[13];
            Client subClient = (Client)objects[14];
            RegionCode subClientRegionCode = (RegionCode)objects[15];
            Order order = (Order)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            User orderItemUser = (User)objects[18];
            Product product = (Product)objects[19];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[20];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[21];
            Transporter transporter = (Transporter)objects[22];
            TransporterType transporterType = (TransporterType)objects[23];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[24];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[25];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[26];
            SaleMerged saleMerged = (SaleMerged)objects[27];
            User discountUpdatedBy = (User)objects[28];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[29];
            Client rootClient = (Client)objects[30];
            RegionCode rootRegionCode = (RegionCode)objects[31];
            ProductReservation reservation = (ProductReservation)objects[32];
            ProductAvailability reservationAvailability = (ProductAvailability)objects[33];
            Storage reservationAvailabilityStorage = (Storage)objects[34];
            Organization reservationAvailabilityStorageOrganization = (Organization)objects[35];
            Organization organization = (Organization)objects[36];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (saleMerged != null && !saleFromList.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) saleFromList.InputSaleMerges.Add(saleMerged);

                if (clientSubClient != null && !saleFromList.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) {
                    if (subClient != null) {
                        if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                        clientSubClient.SubClient = subClient;
                    }

                    saleFromList.ClientAgreement.Client.SubClients.Add(clientSubClient);
                }

                if (orderItem == null) return sale;

                if (!values.Any(y => y.Item2 == orderItem.Id)) {
                    if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                        values.Add((sale.Id, orderItem.Id));
                        orderItem.User = orderItemUser;
                        orderItem.DiscountUpdatedBy = discountUpdatedBy;
                        orderItem.Product = product;

                        if (!saleFromList.Order.OrderItems.Any(x => x.ProductId.Equals(product.Id))) {
                            orderItem.Qty -= orderItem.ReturnedQty;
                            saleFromList.Order.OrderItems.Add(orderItem);
                        } else {
                            OrderItem orderItemGroupByProduct = saleFromList.Order.OrderItems.First(x => x.ProductId.Equals(product.Id));

                            if (orderItemGroupByProduct.OrderItemsGroupByProduct == null)
                                orderItemGroupByProduct.OrderItemsGroupByProduct = new List<OrderItem>();

                            orderItemGroupByProduct.Qty += orderItem.Qty;
                            orderItemGroupByProduct.Qty -= orderItem.ReturnedQty;
                            orderItemGroupByProduct.ReturnedQty += orderItem.ReturnedQty;

                            orderItemGroupByProduct.OrderItemsGroupByProduct.Add(orderItem);
                        }

                        if (reservation == null) return sale;

                        reservationAvailabilityStorage.Organization = reservationAvailabilityStorageOrganization;

                        reservationAvailability.Storage = reservationAvailabilityStorage;

                        reservation.ProductAvailability = reservationAvailability;

                        orderItem.ProductReservations.Add(reservation);
                    } else {
                        orderItem = saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id));

                        if (orderItemBaseShiftStatus != null && !orderItem.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id)))
                            saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);

                        if (reservation == null || orderItem.ProductReservations.Any(r => r.Id.Equals(reservation.Id))) return sale;

                        reservationAvailabilityStorage.Organization = reservationAvailabilityStorageOrganization;

                        reservationAvailability.Storage = reservationAvailabilityStorage;

                        reservation.ProductAvailability = reservationAvailability;

                        orderItem.ProductReservations.Add(reservation);
                    }
                }
            } else {
                if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                if (clientSubClient != null && subClient != null) {
                    if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                    clientSubClient.SubClient = subClient;

                    client.SubClients.Add(clientSubClient);
                }

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;
                agreement.Organization = organization;

                client.RegionCode = regionCode;

                if (rootClient != null) {
                    rootClient.RegionCode = rootRegionCode;

                    client.RootClient = rootClient;
                }

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    orderItem.Qty -= orderItem.ReturnedQty;

                    orderItem.OrderItemsGroupByProduct = new List<OrderItem>();

                    order.OrderItems.Add(orderItem);
                }

                if (transporterType != null) {
                    transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;
                }

                sale.Transporter = transporter;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.SaleInvoiceDocument = saleInvoiceDocument;
                sale.User = saleUser;

                sales.Add(sale);

                if (reservation == null) return sale;

                reservationAvailabilityStorage.Organization = reservationAvailabilityStorageOrganization;

                reservationAvailability.Storage = reservationAvailabilityStorage;

                reservation.ProductAvailability = reservationAvailability;

                orderItem.ProductReservations.Add(reservation);
            }

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, SaleIds = saleIds, OrganizationNetId = organizationNetId };

        _connection.Query(sqlExpression, types, mapper, props, commandTimeout: 3600);

        return sales;
    }

    public List<Sale> GetAllPlSalesFiltered(DateTime from, DateTime to) {
        List<Sale> sales = new();

        string idsSqlExpression =
            "SELECT [Sale].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "WHERE [Sale].ChangedToInvoice >= @From " +
            "AND [Sale].ChangedToInvoice <= @To " +
            "AND [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [SaleNumber].[Value] LIKE N'P%' " +
            "ORDER BY [Sale].ChangedToInvoice";

        IEnumerable<long> saleIds = _connection.Query<long>(
            idsSqlExpression,
            new {
                From = from,
                To = to
            }
        );

        if (!saleIds.Any()) return sales;

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in saleIds) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",ClientSubClient.* " +
            ",SubClient.* " +
            ",SubClientRegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, Agreement.WithVATAccounting, [OrderItem].[ID]) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, Agreement.WithVATAccounting, [OrderItem].[ID]) AS CurrentLocalPrice " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",Transporter.* " +
            ",TransporterType.* " +
            ",TransporterTypeTranslation.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",SaleMerged.* " +
            ",[DiscountUpdatedUser].* " +
            ",[SaleInvoiceDocument].* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = Agreement.OrganizationID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "LEFT JOIN RegionCode AS SubClientRegionCode " +
            "ON SubClient.RegionCodeId = SubClientRegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN Transporter " +
            "ON Sale.TransporterID = Transporter.ID " +
            "LEFT JOIN TransporterType " +
            "ON Transporter.TransporterTypeID = TransporterType.ID " +
            "LEFT JOIN TransporterTypeTranslation " +
            "ON TransporterTypeTranslation.TransporterTypeID = TransporterType.ID " +
            "AND TransporterTypeTranslation.Deleted = 0 " +
            "AND TransporterTypeTranslation.CultureCode = @Culture " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [Sale].SaleInvoiceDocumentID = [SaleInvoiceDocument].ID " +
            "WHERE Sale.ID IN " +
            inExpression +
            "ORDER BY Sale.ChangedToInvoice";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(User),
            typeof(SaleInvoiceDocument)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            ClientSubClient clientSubClient = (ClientSubClient)objects[13];
            Client subClient = (Client)objects[14];
            RegionCode subClientRegionCode = (RegionCode)objects[15];
            Order order = (Order)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            User orderItemUser = (User)objects[18];
            Product product = (Product)objects[19];
            ProductPricing productPricing = (ProductPricing)objects[20];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[21];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[22];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[23];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[24];
            Transporter transporter = (Transporter)objects[25];
            TransporterType transporterType = (TransporterType)objects[26];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[27];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[28];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[29];
            SaleMerged saleMerged = (SaleMerged)objects[30];
            User discountUpdatedBy = (User)objects[31];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[32];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (saleMerged != null && !saleFromList.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) saleFromList.InputSaleMerges.Add(saleMerged);

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (clientSubClient != null && !saleFromList.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) {
                    if (subClient != null) {
                        if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                        clientSubClient.SubClient = subClient;
                    }

                    saleFromList.ClientAgreement.Client.SubClients.Add(clientSubClient);
                }

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                if (clientSubClient != null && subClient != null) {
                    if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                    clientSubClient.SubClient = subClient;

                    client.SubClients.Add(clientSubClient);
                }

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);
                }

                if (transporterType != null) {
                    transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;
                }

                sale.Transporter = transporter;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;
                sale.SaleInvoiceDocument = saleInvoiceDocument;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (!sales.Any()) return sales;

        types = new[] {
            typeof(Sale),
            typeof(Order),
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderPackage orderPackage = (OrderPackage)objects[2];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[3];
            User user = (User)objects[4];
            UserRole userRole = (UserRole)objects[5];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[6];
            OrderItem orderItem = (OrderItem)objects[7];
            Product product = (Product)objects[8];
            MeasureUnit measureUnit = (MeasureUnit)objects[9];

            if (orderPackage == null) return null;

            Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

            if (!saleFromList.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleFromList.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleFromList.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        string packagesExpression =
            "SELECT [Sale].ID " +
            ",[Order].ID " +
            ",[OrderPackage].* " +
            ",[OrderPackageUser].* " +
            ",[User].* " +
            ",[UserRole].* " +
            ",[OrderPackageItem].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        packagesExpression +=
            ", [Product].HasAnalogue " +
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
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderPackage] " +
            "ON [OrderPackage].OrderID = [Order].ID " +
            "AND [OrderPackage].Deleted = 0 " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Sale].ID IN " +
            inExpression;

        _connection.Query(
            packagesExpression,
            types,
            packagesMapper,
            new { Ids = sales.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return sales;
    }

    public IEnumerable<Sale> GetAllSalesByTaxFreePackListIdExceptProvided(long packListId, IEnumerable<long> ids) {
        IEnumerable<Sale> sales =
            _connection.Query<Sale, Order, Sale>(
                "SELECT * " +
                "FROM [Sale] " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "WHERE [Sale].TaxFreePackListID = @PackListId " +
                "AND [Sale].ID NOT IN @Ids",
                (sale, order) => {
                    sale.Order = order;

                    return sale;
                },
                new { PackListId = packListId, Ids = ids }
            );

        if (sales.Any())
            _connection.Query<OrderItem, Product, OrderItem>(
                "SELECT * " +
                "FROM [OrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [OrderItem].ProductID " +
                "WHERE [OrderItem].OrderID IN @Ids",
                (orderItem, product) => {
                    orderItem.Product = product;

                    sales.First(s => s.OrderId.Equals(orderItem.OrderId)).Order.OrderItems.Add(orderItem);

                    return orderItem;
                },
                new { Ids = sales.Select(s => s.Order.Id) }
            );

        return sales;
    }

    public IEnumerable<Sale> GetAllSalesBySadIdExceptProvided(long sadId, IEnumerable<long> ids) {
        IEnumerable<Sale> sales =
            _connection.Query<Sale, Order, Sale>(
                "SELECT * " +
                "FROM [Sale] " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "WHERE [Sale].SadID = @SadId " +
                "AND [Sale].ID NOT IN @Ids",
                (sale, order) => {
                    sale.Order = order;

                    return sale;
                },
                new { SadId = sadId, Ids = ids }
            );

        if (!sales.Any()) return sales;

        _connection.Query<OrderItem, Product, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [OrderItem].OrderID IN @Ids",
            (orderItem, product) => {
                orderItem.Product = product;

                sales.First(s => s.OrderId.Equals(orderItem.OrderId)).Order.OrderItems.Add(orderItem);

                return orderItem;
            },
            new { Ids = sales.Select(s => s.Order.Id) }
        );

        return sales;
    }

    public List<Sale> GetAllUkPlClientsSalesFiltered(DateTime from, DateTime to, string value) {
        List<Sale> sales = new();

        IEnumerable<long> saleIds =
            _connection.Query<long>(
                "SELECT [Sale].ID " +
                "FROM [Sale] " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [ClientAgreement].ClientID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [ClientInRole] " +
                "ON [ClientInRole].ClientID = [Client].ID " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                "LEFT JOIN [SaleBaseShiftStatus] " +
                "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
                "WHERE [Sale].IsMerged = 0 " +
                "AND [BaseLifeCycleStatus].SaleLifeCycleType <> 0 " +
                "AND [ClientInRole].ClientTypeRoleID = 3 " +
                "AND ( " +
                "[Sale].ShiftStatusID IS NULL " +
                "OR " +
                "[SaleBaseShiftStatus].ShiftStatus <> 0 " +
                ") " +
                "AND (" +
                "[Client].FullName like N'%' + @Value + N'%' " +
                "OR " +
                "[RegionCode].[Value] like N'%' + @Value + N'%' " +
                ") " +
                "AND [SaleNumber].[Value] like N'P%' " +
                "AND [Sale].ChangedToInvoice IS NOT NULL " +
                //"AND [Sale].ChangedToInvoice >= @From " +
                //"AND [Sale].ChangedToInvoice <= @To " +
                "AND [Sale].TaxFreePackListID IS NULL " +
                "AND [Sale].SadID IS NULL " +
                "AND [Sale].Deleted = 0",
                new { Value = value, From = from, To = to }
            );

        if (!saleIds.Any()) return sales;

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in saleIds) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",ClientSubClient.* " +
            ",SubClient.* " +
            ",SubClientRegionCode.* " +
            ",[Order].* " +
            ",[OrderItem].* " +
            ", (" +
            "SELECT SUM(" +
            "ISNULL([ProductReservation].Qty, 0.00) * ISNULL([ProductResidue].Weight, 0.00)" +
            ") " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [ProductResidue] " +
            "ON [ProductResidue].ID = [ProductReservation].ProductResidueID " +
            "WHERE [ProductReservation].Deleted = 0 " +
            "AND [ProductReservation].OrderItemID = [OrderItem].ID " +
            ") [TotalWeight] " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, Agreement.WithVATAccounting, [OrderItem].[ID]) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, Agreement.WithVATAccounting, [OrderItem].[ID]) AS CurrentLocalPrice " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",Transporter.* " +
            ",TransporterType.* " +
            ",TransporterTypeTranslation.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",SaleMerged.* " +
            ",[DiscountUpdatedUser].* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = Agreement.OrganizationID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "LEFT JOIN RegionCode AS SubClientRegionCode " +
            "ON SubClient.RegionCodeId = SubClientRegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN Transporter " +
            "ON Sale.TransporterID = Transporter.ID " +
            "LEFT JOIN TransporterType " +
            "ON Transporter.TransporterTypeID = TransporterType.ID " +
            "LEFT JOIN TransporterTypeTranslation " +
            "ON TransporterTypeTranslation.TransporterTypeID = TransporterType.ID " +
            "AND TransporterTypeTranslation.Deleted = 0 " +
            "AND TransporterTypeTranslation.CultureCode = @Culture " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "WHERE Sale.ID IN " +
            inExpression +
            "ORDER BY Sale.Created DESC";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(User)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            ClientSubClient clientSubClient = (ClientSubClient)objects[13];
            Client subClient = (Client)objects[14];
            RegionCode subClientRegionCode = (RegionCode)objects[15];
            Order order = (Order)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            User orderItemUser = (User)objects[18];
            Product product = (Product)objects[19];
            ProductPricing productPricing = (ProductPricing)objects[20];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[21];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[22];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[23];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[24];
            Transporter transporter = (Transporter)objects[25];
            TransporterType transporterType = (TransporterType)objects[26];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[27];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[28];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[29];
            SaleMerged saleMerged = (SaleMerged)objects[30];
            User discountUpdatedBy = (User)objects[31];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (saleMerged != null && !saleFromList.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) saleFromList.InputSaleMerges.Add(saleMerged);

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (clientSubClient != null && !saleFromList.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) {
                    if (subClient != null) {
                        if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                        clientSubClient.SubClient = subClient;
                    }

                    saleFromList.ClientAgreement.Client.SubClients.Add(clientSubClient);
                }

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);

                    sale.TotalWeight += orderItem.TotalWeight;
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                if (clientSubClient != null && subClient != null) {
                    if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                    clientSubClient.SubClient = subClient;

                    client.SubClients.Add(clientSubClient);
                }

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);

                    sale.TotalWeight += orderItem.TotalWeight;
                }

                if (transporterType != null) {
                    transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;
                }

                sale.Transporter = transporter;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (!sales.Any()) return sales;

        types = new[] {
            typeof(Sale),
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderPackage orderPackage = (OrderPackage)objects[1];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[2];
            User user = (User)objects[3];
            UserRole userRole = (UserRole)objects[4];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[5];
            OrderItem orderItem = (OrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];

            if (orderPackage == null) return null;

            Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

            if (!saleFromList.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleFromList.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleFromList.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        string packagesExpression =
            "SELECT [Sale].ID " +
            ",[OrderPackage].* " +
            ",[OrderPackageUser].* " +
            ",[User].* " +
            ",[UserRole].* " +
            ",[OrderPackageItem].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        packagesExpression +=
            ", [Product].HasAnalogue " +
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
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderPackage] " +
            "ON [OrderPackage].OrderID = [Order].ID " +
            "AND [OrderPackage].Deleted = 0 " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Sale].ID IN " +
            inExpression;

        _connection.Query(
            packagesExpression,
            types,
            packagesMapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return sales;
    }

    public List<Sale> GetAllFilteredByTransporterAndType(DateTime from, DateTime to, Guid netId, bool onlyPrinted = false) {
        List<Sale> sales = new();

        string idsSqlExpression =
            "SELECT [Sale].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [Sale].TransporterID " +
            "LEFT JOIN [ShipmentListItem] " +
            "ON [ShipmentListItem].SaleID = [Sale].ID " +
            "WHERE [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [Sale].ChangedToInvoice >= @From " +
            "AND [Sale].ChangedToInvoice <= @To " +
            "AND [Organization].Culture = @Culture " +
            "AND [Transporter].NetUID = @NetId " +
            "AND [BaseLifeCycleStatus].SaleLifeCycleType = @Type " +
            "AND [ShipmentListItem].ID IS NULL " +
            "AND (" +
            "[Sale].IsVatSale = 0 " +
            "OR " +
            "(" +
            "[Sale].IsVatSale = 1 " +
            "AND " +
            "[BaseSalePaymentStatus].SalePaymentStatusType > 0 " +
            "AND " +
            "[BaseSalePaymentStatus].SalePaymentStatusType <= 3" +
            ")" +
            "OR ([Sale].[IsAcceptedToPacking] = 1) " +
            ")";

        if (onlyPrinted) idsSqlExpression += " AND [Sale].IsPrinted = 1";

        IEnumerable<long> saleIds =
            _connection.Query<long>(
                idsSqlExpression,
                new {
                    From = from,
                    To = to,
                    NetId = netId,
                    Type = SaleLifeCycleType.Packaging,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });

        if (!saleIds.Any()) return sales;

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in saleIds) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",ClientSubClient.* " +
            ",SubClient.* " +
            ",SubClientRegionCode.* " +
            ",[Order].* " +
            ",SaleBaseShiftStatus.* " +
            ",Transporter.* " +
            ",TransporterType.* " +
            ",TransporterTypeTranslation.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",SaleMerged.* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = Agreement.OrganizationID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "LEFT JOIN RegionCode AS SubClientRegionCode " +
            "ON SubClient.RegionCodeId = SubClientRegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN Transporter " +
            "ON Sale.TransporterID = Transporter.ID " +
            "LEFT JOIN TransporterType " +
            "ON Transporter.TransporterTypeID = TransporterType.ID " +
            "LEFT JOIN TransporterTypeTranslation " +
            "ON TransporterTypeTranslation.TransporterTypeID = TransporterType.ID " +
            "AND TransporterTypeTranslation.Deleted = 0 " +
            "AND TransporterTypeTranslation.CultureCode = @Culture " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "WHERE Sale.ID IN " +
            inExpression +
            "ORDER BY Sale.Created DESC";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(SaleBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            ClientSubClient clientSubClient = (ClientSubClient)objects[13];
            Client subClient = (Client)objects[14];
            RegionCode subClientRegionCode = (RegionCode)objects[15];
            Order order = (Order)objects[16];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[17];
            Transporter transporter = (Transporter)objects[18];
            TransporterType transporterType = (TransporterType)objects[19];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[20];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[21];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[22];
            SaleMerged saleMerged = (SaleMerged)objects[23];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (saleMerged != null && !saleFromList.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) saleFromList.InputSaleMerges.Add(saleMerged);

                if (clientSubClient == null || saleFromList.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) return sale;

                if (subClient != null) {
                    if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                    clientSubClient.SubClient = subClient;
                }

                saleFromList.ClientAgreement.Client.SubClients.Add(clientSubClient);
            } else {
                if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                if (clientSubClient != null && subClient != null) {
                    if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                    clientSubClient.SubClient = subClient;

                    client.SubClients.Add(clientSubClient);
                }

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (transporterType != null) {
                    transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;
                }

                sale.Transporter = transporter;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (!sales.Any()) return sales;

        string sqlQueryOrderItems =
            "SELECT " +
            "OrderItem.* " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].[NameUA] AS [Name] " +
            ",[Product].[DescriptionUA] AS [Description] " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentPrice] " +
            ", (CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) AS [CurrentLocalPrice] ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlQueryOrderItems += ",[Product].[NameUA] AS [Name] ";
            sqlQueryOrderItems += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlQueryOrderItems += ",[Product].[NameUA] AS [Name] ";
            sqlQueryOrderItems += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlQueryOrderItems +=
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",[DiscountUpdatedUser].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = [ClientAgreement].ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "WHERE " +
            "[Sale].[ID] IN @Ids " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 ";

        Type[] orderItemTypes = {
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(OrderItemBaseShiftStatus),
            typeof(User)
        };

        Func<object[], OrderItem> orderItemsMapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            User orderItemUser = (User)objects[1];
            Product product = (Product)objects[2];
            ProductPricing productPricing = (ProductPricing)objects[3];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[4];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[5];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[6];
            User discountUpdatedBy = (User)objects[7];

            Sale existSale = sales.First(x => x.OrderId.Equals(orderItem.OrderId));

            if (!existSale.Order.OrderItems.Any(x => x.Id.Equals(orderItem.Id)))
                existSale.Order.OrderItems.Add(orderItem);

            if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

            if (productPricing != null) product.ProductPricings.Add(productPricing);

            if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

            if (productGroupDiscount != null) existSale.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

            orderItem.User = orderItemUser;
            orderItem.DiscountUpdatedBy = discountUpdatedBy;
            orderItem.Product = product;

            return orderItem;
        };

        _connection.Query(
            sqlQueryOrderItems,
            orderItemTypes,
            orderItemsMapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = sales.Select(x => x.Id)
            });

        types = new[] {
            typeof(Sale),
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderPackage orderPackage = (OrderPackage)objects[1];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[2];
            User user = (User)objects[3];
            UserRole userRole = (UserRole)objects[4];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[5];
            OrderItem orderItem = (OrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];

            if (orderPackage == null) return null;

            Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

            if (!saleFromList.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleFromList.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleFromList.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        string packagesExpression =
            "SELECT [Sale].ID " +
            ",[OrderPackage].* " +
            ",[OrderPackageUser].* " +
            ",[User].* " +
            ",[UserRole].* " +
            ",[OrderPackageItem].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        packagesExpression +=
            ", [Product].HasAnalogue " +
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
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderPackage] " +
            "ON [OrderPackage].OrderID = [Order].ID " +
            "AND [OrderPackage].Deleted = 0 " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Sale].ID IN " +
            inExpression;

        _connection.Query(
            packagesExpression,
            types,
            packagesMapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return sales;
    }

    public List<Sale> GetAllSalesFromECommerceFromPlUkClients() {
        List<Sale> sales = new();

        string idsSqlExpression =
            "SELECT [Sale].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [Client].ID = [ClientInRole].ClientID " +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "WHERE [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [ClientTypeRole].[Name] = N'Pl->Ukr' " +
            "AND [Order].OrderSource <> 1 " +
            "GROUP BY [Sale].ID, [Sale].Created " +
            "ORDER BY [Sale].Created DESC";

        IEnumerable<long> saleIds = _connection.Query<long>(
            idsSqlExpression
        );

        if (!saleIds.Any()) return sales;

        StringBuilder builder = new();

        builder.Append("(0");

        foreach (long saleId in saleIds) builder.Append($",{saleId}");

        builder.Append(")");

        string inExpression = builder.ToString();

        string sqlExpression =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",ClientSubClient.* " +
            ",SubClient.* " +
            ",SubClientRegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, Agreement.WithVATAccounting, [OrderItem].[ID]) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, Agreement.WithVATAccounting, [OrderItem].[ID]) AS CurrentLocalPrice " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            ",Transporter.* " +
            ",TransporterType.* " +
            ",TransporterTypeTranslation.* " +
            ",DeliveryRecipient.* " +
            ",DeliveryRecipientAddress.* " +
            ",SaleMerged.* " +
            ",[DiscountUpdatedUser].* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = Agreement.OrganizationID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "LEFT JOIN RegionCode AS SubClientRegionCode " +
            "ON SubClient.RegionCodeId = SubClientRegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN Transporter " +
            "ON Sale.TransporterID = Transporter.ID " +
            "LEFT JOIN TransporterType " +
            "ON Transporter.TransporterTypeID = TransporterType.ID " +
            "LEFT JOIN TransporterTypeTranslation " +
            "ON TransporterTypeTranslation.TransporterTypeID = TransporterType.ID " +
            "AND TransporterTypeTranslation.Deleted = 0 " +
            "AND TransporterTypeTranslation.CultureCode = @Culture " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "WHERE Sale.ID IN " +
            inExpression +
            "ORDER BY Sale.Created DESC";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(User)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            ClientSubClient clientSubClient = (ClientSubClient)objects[13];
            Client subClient = (Client)objects[14];
            RegionCode subClientRegionCode = (RegionCode)objects[15];
            Order order = (Order)objects[16];
            OrderItem orderItem = (OrderItem)objects[17];
            User orderItemUser = (User)objects[18];
            Product product = (Product)objects[19];
            ProductPricing productPricing = (ProductPricing)objects[20];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[21];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[22];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[23];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[24];
            Transporter transporter = (Transporter)objects[25];
            TransporterType transporterType = (TransporterType)objects[26];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[27];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[28];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[29];
            SaleMerged saleMerged = (SaleMerged)objects[30];
            User discountUpdatedBy = (User)objects[31];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (saleMerged != null && !saleFromList.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) saleFromList.InputSaleMerges.Add(saleMerged);

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (clientSubClient != null && !saleFromList.ClientAgreement.Client.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) {
                    if (subClient != null) {
                        if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                        clientSubClient.SubClient = subClient;
                    }

                    saleFromList.ClientAgreement.Client.SubClients.Add(clientSubClient);
                }

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                if (clientSubClient != null && subClient != null) {
                    if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                    clientSubClient.SubClient = subClient;

                    client.SubClients.Add(clientSubClient);
                }

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.DiscountUpdatedBy = discountUpdatedBy;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);
                }

                if (transporterType != null) {
                    transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;
                }

                sale.Transporter = transporter;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (!sales.Any()) return sales;

        types = new[] {
            typeof(Sale),
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            Sale sale = (Sale)objects[0];
            OrderPackage orderPackage = (OrderPackage)objects[1];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[2];
            User user = (User)objects[3];
            UserRole userRole = (UserRole)objects[4];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[5];
            OrderItem orderItem = (OrderItem)objects[6];
            Product product = (Product)objects[7];
            MeasureUnit measureUnit = (MeasureUnit)objects[8];

            if (orderPackage == null) return null;

            Sale saleFromList = sales.First(s => s.Id.Equals(sale.Id));

            if (!saleFromList.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleFromList.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleFromList.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        string packagesExpression =
            "SELECT [Sale].ID " +
            ",[OrderPackage].* " +
            ",[OrderPackageUser].* " +
            ",[User].* " +
            ",[UserRole].* " +
            ",[OrderPackageItem].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            packagesExpression += ",[Product].[NameUA] AS [Name] ";
            packagesExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        packagesExpression +=
            ", [Product].HasAnalogue " +
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
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderPackage] " +
            "ON [OrderPackage].OrderID = [Order].ID " +
            "AND [OrderPackage].Deleted = 0 " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Sale].ID IN " +
            inExpression;

        _connection.Query(
            packagesExpression,
            types,
            packagesMapper,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return sales;
    }

    public Sale GetByIdWithoutIncludes(long id) {
        return _connection.Query<Sale>(
            "SELECT * FROM Sale " +
            "WHERE Sale.ID = @Id",
            new {
                Id = id
            }
        ).SingleOrDefault();
    }

    public Sale GetByIdWithAgreement(long id) {
        return _connection.Query<Sale, Order, ClientAgreement, Agreement, BaseLifeCycleStatus, Sale>(
            "SELECT * FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "WHERE Sale.ID = @Id " +
            "AND Sale.Deleted = 0 " +
            "ORDER BY Sale.Created DESC",
            (sale, order, clientAgreement, agreement, baseLifeCycleStatus) => {
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                clientAgreement.Agreement = agreement;
                sale.ClientAgreement = clientAgreement;
                sale.Order = order;
                return sale;
            },
            new {
                Id = id
            }
        ).SingleOrDefault();
    }

    public Sale GetByIdWithSaleMerged(long id) {
        Sale saleToReturn = null;

        _connection.Query<Sale, SaleMerged, Sale>(
            "SELECT * FROM Sale " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "WHERE Sale.ID = @Id",
            (sale, saleMerged) => {
                if (saleToReturn != null) {
                    if (saleMerged != null && !saleToReturn.InputSaleMerges.Any(m => m.Id.Equals(saleMerged.Id))) saleToReturn.InputSaleMerges.Add(saleMerged);
                } else {
                    if (saleMerged != null) sale.InputSaleMerges.Add(saleMerged);

                    saleToReturn = sale;
                }

                return sale;
            },
            new {
                Id = id
            }
        );

        return saleToReturn;
    }

    public Sale GetById(long id, bool withDeleted = false) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN (" +
            "SELECT [Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            "FROM [Product] " +
            ") AS [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [User] " +
            "ON [Sale].UserID = [User].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Currency] " +
            "ON [Agreement].CurrencyID = [Currency].ID " +
            "AND [Currency].Deleted = 0 " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "AND [RegionCode].Deleted = 0 " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "AND [BaseLifeCycleStatus].Deleted = 0 " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "AND [BaseSalePaymentStatus].Deleted = 0 " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "AND [SaleNumber].Deleted = 0 " +
            "WHERE [Sale].ID = @Id " +
            (
                withDeleted
                    ? string.Empty
                    : "AND [Sale].Deleted = 0"
            );

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Client client = (Client)objects[8];
            RegionCode regionCode = (RegionCode)objects[9];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];
            SaleNumber saleNumber = (SaleNumber)objects[12];

            if (saleToReturn == null) {
                agreement.Currency = currency;

                client.RegionCode = regionCode;

                clientAgreement.Agreement = agreement;
                clientAgreement.Client = client;

                sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                sale.User = user;
                sale.SaleNumber = saleNumber;
                sale.ClientAgreement = clientAgreement;
                sale.Order = order;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            orderItem.Product = product;

            saleToReturn.Order.OrderItems.Add(orderItem);

            return sale;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        if (saleToReturn != null)
            saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        return saleToReturn;
    }

    public Sale GetByIdForConsignment(long id) {
        Sale toReturn = null;

        Type[] types = {
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Order),
            typeof(OrderItem),
            typeof(ProductReservation),
            typeof(ProductAvailability),
            typeof(Product)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Organization organization = (Organization)objects[3];
            Order order = (Order)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            ProductReservation reservation = (ProductReservation)objects[6];
            ProductAvailability availability = (ProductAvailability)objects[7];
            Product product = (Product)objects[8];

            if (toReturn == null) {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.Order = order;

                toReturn = sale;
            }

            if (orderItem == null) return sale;

            if (toReturn.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                orderItem = toReturn.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id));
            } else {
                orderItem.Product = product;

                toReturn.Order.OrderItems.Add(orderItem);
            }

            if (reservation == null) return sale;

            reservation.ProductAvailability = availability;

            orderItem.ProductReservations.Add(reservation);

            return sale;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty <> 0 " +
            "LEFT JOIN [ProductReservation] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "AND [ProductReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [Sale].ID = @Id",
            types,
            mapper,
            new {
                Id = id
            }
        );

        return toReturn;
    }

    public Sale GetByIdForConsignment(long id, long orderItemId) {
        Sale toReturn = null;

        Type[] types = {
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Order),
            typeof(OrderItem),
            typeof(ProductReservation),
            typeof(ProductAvailability),
            typeof(Product)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Organization organization = (Organization)objects[3];
            Order order = (Order)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            ProductReservation reservation = (ProductReservation)objects[6];
            ProductAvailability availability = (ProductAvailability)objects[7];
            Product product = (Product)objects[8];

            if (toReturn == null) {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;
                sale.Order = order;

                toReturn = sale;
            }

            if (orderItem == null) return sale;

            if (toReturn.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                orderItem = toReturn.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id));
            } else {
                orderItem.Product = product;

                toReturn.Order.OrderItems.Add(orderItem);
            }

            if (reservation == null) return sale;

            reservation.ProductAvailability = availability;

            orderItem.ProductReservations.Add(reservation);

            return sale;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty <> 0 " +
            "LEFT JOIN [ProductReservation] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "AND [ProductReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [Sale].ID = @Id " +
            "AND [OrderItem].ID = @OrderItemId",
            types,
            mapper,
            new {
                Id = id,
                OrderItemId = orderItemId
            }
        );

        return toReturn;
    }

    public Sale GetByIdWithCalculatedDynamicPrices(long id) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT [Sale].* " +
            ",[ClientAgreement].* " +
            ",[Order].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
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
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            ",[ProductProductGroup].* " +
            ",[User].* " +
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[Client].* " +
            ",[RegionCode].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[BaseSalePaymentStatus].* " +
            ",[SaleNumber].* " +
            ",[OrderItemUser].* " +
            ",[CurrencyTranslation].* " +
            ",[SaleBaseShiftStatus].* " +
            ",[OrderItemBaseShiftStatus].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[Transporter].* " +
            ",[DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            ",[InputSaleMerged].* " +
            ",[OutputSaleMerged].* " +
            ",[SaleInvoiceDocument].* " +
            ",[ClientSubClient].* " +
            ",[SaleInvoiceNumber].* " +
            ",[MeasureUnit].* " +
            "FROM [Sale] " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN  [Product] " +
            "ON [Product].ID = OrderItem.ProductID " +
            "AND Product.Deleted = 0 " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN Pricing " +
            "ON ProductPricing.PricingID = Pricing.ID " +
            "AND Pricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID " +
            "AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = Sale.UserID AND [User].Deleted = 0 " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusID = BaseLifeCycleStatus.ID " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberID = SaleNumber.ID And SaleNumber.Deleted = 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ProductID = Product.ID " +
            "AND ProductAvailability.Deleted = 0 " +
            "LEFT JOIN Storage " +
            "ON Storage.ID = ProductAvailability.StorageID " +
            "LEFT JOIN Transporter " +
            "ON Transporter.ID = Sale.TransporterID " +
            "AND Transporter.Deleted = 0 " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged AS InputSaleMerged " +
            "ON InputSaleMerged.OutputSaleID = Sale.ID " +
            "LEFT JOIN SaleMerged AS OutputSaleMerged " +
            "ON OutputSaleMerged.InputSaleID = Sale.ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = Sale.SaleInvoiceDocumentID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID " +
            "LEFT JOIN [SaleInvoiceNumber] " +
            "ON [SaleInvoiceNumber].ID = [Sale].SaleInvoiceNumberID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE Sale.ID = @Id";

        Type[] types = {
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(User),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(CurrencyTranslation),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(SaleMerged),
            typeof(SaleInvoiceDocument),
            typeof(ClientSubClient),
            typeof(SaleInvoiceNumber),
            typeof(MeasureUnit)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Order order = (Order)objects[2];
            OrderItem orderItem = (OrderItem)objects[3];
            Product product = (Product)objects[4];
            ProductPricing productPricing = (ProductPricing)objects[5];
            Pricing pricing = (Pricing)objects[6];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[7];
            User user = (User)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Currency currency = (Currency)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[13];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[14];
            SaleNumber saleNumber = (SaleNumber)objects[15];
            User orderItemUser = (User)objects[16];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[17];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[18];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[19];
            ProductAvailability productAvailability = (ProductAvailability)objects[20];
            Storage productAvailabilityStorage = (Storage)objects[21];
            Transporter transporter = (Transporter)objects[22];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[23];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[24];
            SaleMerged inputSaleMerged = (SaleMerged)objects[25];
            SaleMerged outputSaleMerged = (SaleMerged)objects[26];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[27];
            ClientSubClient clientSubClient = (ClientSubClient)objects[28];
            SaleInvoiceNumber saleInvoiceNumber = (SaleInvoiceNumber)objects[29];
            MeasureUnit measureUnit = (MeasureUnit)objects[30];

            if (saleToReturn == null) {
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.SaleInvoiceDocument = saleInvoiceDocument;

                if (inputSaleMerged != null) sale.InputSaleMerges.Add(inputSaleMerged);

                if (outputSaleMerged != null) sale.OutputSaleMerges.Add(outputSaleMerged);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (transporter != null) sale.Transporter = transporter;

                if (clientAgreement != null) {
                    if (agreement != null) {
                        if (currency != null) {
                            currency.Name = currencyTranslation?.Name;

                            agreement.Currency = currency;
                        }

                        clientAgreement.Agreement = agreement;
                    }

                    if (client != null) {
                        if (regionCode != null) client.RegionCode = regionCode;

                        if (clientSubClient != null) client.SubClients.Add(clientSubClient);

                        clientAgreement.Client = client;
                    }

                    sale.ClientAgreement = clientAgreement;
                }

                if (user != null) {
                    sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                    sale.User = user;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                sale.SaleInvoiceNumber = saleInvoiceNumber;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (productPricing != null) {
                if (pricing != null) productPricing.Pricing = pricing;

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.TotalAmount = productPricing.Price * Convert.ToDecimal(orderItem.Qty);

                orderItem.User = orderItemUser;

                product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

            product.MeasureUnit = measureUnit;

            orderItem.Product = product;

            if (saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                OrderItem orderItemFormArray = saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (productPricing != null && !orderItemFormArray.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemFormArray.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemFormArray.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemFormArray.Product.ProductProductGroups.Add(productProductGroup);

                if (orderItemBaseShiftStatus != null && !orderItemFormArray.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id)))
                    orderItemFormArray.ShiftStatuses.Add(orderItemBaseShiftStatus);
            } else {
                saleToReturn.Order.OrderItems.Add(orderItem);
            }

            if (inputSaleMerged != null && !saleToReturn.InputSaleMerges.Any(s => s.Id.Equals(inputSaleMerged.Id))) saleToReturn.InputSaleMerges.Add(inputSaleMerged);

            if (outputSaleMerged != null && !saleToReturn.OutputSaleMerges.Any(s => s.Id.Equals(outputSaleMerged.Id))) saleToReturn.OutputSaleMerges.Add(outputSaleMerged);

            return sale;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (saleToReturn?.Order == null) return saleToReturn;

        types = new[] {
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            OrderPackage orderPackage = (OrderPackage)objects[0];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[1];
            User user = (User)objects[2];
            UserRole userRole = (UserRole)objects[3];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];

            if (!saleToReturn.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleToReturn.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleToReturn.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [OrderPackage] " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [OrderPackage].Deleted = 0 " +
            "AND [OrderPackage].OrderID = @OrderId",
            types,
            packagesMapper,
            new {
                OrderId = saleToReturn.Order.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        return saleToReturn;
    }

    public Sale GetByIdWithOrderItemMerged(long id) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT * FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderId = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON [Order].Id = OrderItem.OrderId " +
            "LEFT JOIN Product " +
            "ON OrderItem.ProductId = Product.Id " +
            "LEFT JOIN [User] " +
            "ON Sale.UserId = [User].Id " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementId = ClientAgreement.Id " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementId = Agreement.Id " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientId = Client.Id AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusId = BaseLifeCycleStatus.Id " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.Id = Sale.BaseSalePaymentStatusId AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberId = SaleNumber.Id And SaleNumber.Deleted = 0 " +
            "LEFT JOIN OrderItemMerged " +
            "ON OrderItemMerged.OldOrderItemID = OrderItem.ID " +
            "WHERE Sale.ID = @Id " +
            "AND Sale.Deleted = 0";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(OrderItemMerged)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Client client = (Client)objects[8];
            RegionCode regionCode = (RegionCode)objects[9];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];
            SaleNumber saleNumber = (SaleNumber)objects[12];
            OrderItemMerged orderItemMerged = (OrderItemMerged)objects[13];

            if (saleNumber != null) sale.SaleNumber = saleNumber;

            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currency != null) agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                sale.ClientAgreement = clientAgreement;
            }

            if (user != null) {
                sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                sale.User = user;
            }

            if (orderItem != null && product != null) {
                if (orderItemMerged != null) orderItem.OrderItemMerges.Add(orderItemMerged);

                orderItem.Product = product;
                order.OrderItems.Add(orderItem);
            }

            if (order != null) sale.Order = order;

            if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

            if (saleToReturn != null && orderItem != null)
                saleToReturn.Order.OrderItems.Add(orderItem);
            else
                saleToReturn = sale;

            return sale;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        return saleToReturn;
    }

    public Sale GetByOrderItemNetId(Guid orderItemNetId) {
        return _connection.Query<Sale, SaleBaseShiftStatus, Order, ClientAgreement, Agreement, SaleMerged, Sale>(
            "SELECT Sale.* " +
            ",SaleBaseShiftStatus.* " +
            ",[Order].* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",SaleMerged.* " +
            "FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON SaleBaseShiftStatus.ID = Sale.ShiftStatusID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.InputSaleID = Sale.ID " +
            "AND SaleMerged.Deleted = 0 " +
            "WHERE OrderItem.NetUID = @OrderItemNetId " +
            "ORDER BY Sale.ID",
            (sale, shiftStatus, order, clientAgreement, agreement, saleMerged) => {
                if (saleMerged != null) sale.OutputSaleMerges.Add(saleMerged);

                clientAgreement.Agreement = agreement;
                sale.ClientAgreement = clientAgreement;
                sale.Order = order;
                sale.ShiftStatus = shiftStatus;

                return sale;
            },
            new {
                OrderItemNetId = orderItemNetId.ToString()
            }
        ).FirstOrDefault();
    }

    public Sale GetChildSaleIdIfExist(Guid parentNetId) {
        Sale saleToReturn = null;

        _connection.Query<Sale, Order, OrderItem, Sale>(
            "SELECT Sale.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON [Order].ID = OrderItem.OrderID " +
            "AND OrderItem.Deleted = 0 " +
            "WHERE Sale.ParentNetId = @ParentNetId " +
            "AND BaseLifeCycleStatus.SaleLifeCycleType = 0 " +
            "AND Sale.IsMerged = 0",
            (sale, order, orderItem) => {
                if (saleToReturn == null) {
                    sale.Order = order;

                    saleToReturn = sale;
                }

                if (orderItem == null) return sale;

                if (!saleToReturn.Order.OrderItems.Any(e => e.Id.Equals(orderItem.Id))) saleToReturn.Order.OrderItems.Add(orderItem);

                return sale;
            },
            new {
                ParentNetId = parentNetId.ToString()
            }
        );

        return saleToReturn;
    }

    public Sale GetByIdWithAdditionalIncludes(long id) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT [Sale].* " +
            ",[ClientAgreement].* " +
            ",[Order].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].NetUID " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
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
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[User].* " +
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[Client].* " +
            ",[RegionCode].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[BaseSalePaymentStatus].* " +
            ",[SaleNumber].* " +
            ",[OrderItemUser].* " +
            ",[CurrencyTranslation].* " +
            ",[SaleBaseShiftStatus].* " +
            ",[OrderItemBaseShiftStatus].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[Transporter].* " +
            ",[DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            ",[InputSaleMerged].* " +
            ",[OutputSaleMerged].* " +
            ",[SaleInvoiceDocument].* " +
            ",[ClientSubClient].* " +
            ",[SaleInvoiceNumber].* " +
            ",[MeasureUnit].* " +
            ",[DiscountUpdatedUser].* " +
            "FROM [Sale] " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN  [Product] " +
            "ON [Product].ID = OrderItem.ProductID " +
            "AND Product.Deleted = 0 " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN Pricing " +
            "ON ProductPricing.PricingID = Pricing.ID " +
            "AND Pricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID " +
            "AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = Sale.UserID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusID = BaseLifeCycleStatus.ID " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberID = SaleNumber.ID And SaleNumber.Deleted = 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ProductID = Product.ID " +
            "AND ProductAvailability.Deleted = 0 " +
            "LEFT JOIN Storage " +
            "ON Storage.ID = ProductAvailability.StorageID " +
            "LEFT JOIN Transporter " +
            "ON Transporter.ID = Sale.TransporterID " +
            "AND Transporter.Deleted = 0 " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged AS InputSaleMerged " +
            "ON InputSaleMerged.OutputSaleID = Sale.ID " +
            "LEFT JOIN SaleMerged AS OutputSaleMerged " +
            "ON OutputSaleMerged.InputSaleID = Sale.ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = Sale.SaleInvoiceDocumentID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID " +
            "LEFT JOIN [SaleInvoiceNumber] " +
            "ON [SaleInvoiceNumber].ID = [Sale].SaleInvoiceNumberID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "WHERE Sale.ID = @Id";

        Type[] types = {
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(User),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(CurrencyTranslation),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(SaleMerged),
            typeof(SaleInvoiceDocument),
            typeof(ClientSubClient),
            typeof(SaleInvoiceNumber),
            typeof(MeasureUnit),
            typeof(User)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Order order = (Order)objects[2];
            OrderItem orderItem = (OrderItem)objects[3];
            Product product = (Product)objects[4];
            ProductPricing productPricing = (ProductPricing)objects[5];
            Pricing pricing = (Pricing)objects[6];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[7];
            User user = (User)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];
            Client client = (Client)objects[12];
            RegionCode regionCode = (RegionCode)objects[13];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[14];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[15];
            SaleNumber saleNumber = (SaleNumber)objects[16];
            User orderItemUser = (User)objects[17];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[18];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[19];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[20];
            ProductAvailability productAvailability = (ProductAvailability)objects[21];
            Storage productAvailabilityStorage = (Storage)objects[22];
            Transporter transporter = (Transporter)objects[23];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[24];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[25];
            SaleMerged inputSaleMerged = (SaleMerged)objects[26];
            SaleMerged outputSaleMerged = (SaleMerged)objects[27];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[28];
            ClientSubClient clientSubClient = (ClientSubClient)objects[29];
            SaleInvoiceNumber saleInvoiceNumber = (SaleInvoiceNumber)objects[30];
            MeasureUnit measureUnit = (MeasureUnit)objects[31];
            User discountUpdatedBy = (User)objects[32];

            if (saleToReturn == null) {
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.SaleInvoiceDocument = saleInvoiceDocument;

                if (inputSaleMerged != null) sale.InputSaleMerges.Add(inputSaleMerged);

                if (outputSaleMerged != null) sale.OutputSaleMerges.Add(outputSaleMerged);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (transporter != null) sale.Transporter = transporter;

                if (clientAgreement != null) {
                    if (agreement != null) {
                        if (currency != null) {
                            currency.Name = currencyTranslation?.Name;

                            agreement.Currency = currency;
                        }

                        clientAgreement.Agreement = agreement;
                    }

                    if (client != null) {
                        if (regionCode != null) client.RegionCode = regionCode;

                        if (clientSubClient != null) client.SubClients.Add(clientSubClient);

                        clientAgreement.Client = client;
                    }

                    sale.ClientAgreement = clientAgreement;
                }

                if (user != null) {
                    sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                    sale.User = user;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                sale.SaleInvoiceNumber = saleInvoiceNumber;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (productPricing != null) {
                if (pricing != null) productPricing.Pricing = pricing;

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.TotalAmount = product.CurrentPrice * Convert.ToDecimal(orderItem.Qty);

                product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

            product.MeasureUnit = measureUnit;

            orderItem.Product = product;
            orderItem.User = orderItemUser;
            orderItem.DiscountUpdatedBy = discountUpdatedBy;

            if (saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                OrderItem orderItemFormArray = saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (productPricing != null && !orderItemFormArray.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemFormArray.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemFormArray.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemFormArray.Product.ProductProductGroups.Add(productProductGroup);

                if (orderItemBaseShiftStatus != null && !orderItemFormArray.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id)))
                    orderItemFormArray.ShiftStatuses.Add(orderItemBaseShiftStatus);
            } else {
                saleToReturn.Order.OrderItems.Add(orderItem);
            }

            if (inputSaleMerged != null && !saleToReturn.InputSaleMerges.Any(s => s.Id.Equals(inputSaleMerged.Id))) saleToReturn.InputSaleMerges.Add(inputSaleMerged);

            if (outputSaleMerged != null && !saleToReturn.OutputSaleMerges.Any(s => s.Id.Equals(outputSaleMerged.Id))) saleToReturn.OutputSaleMerges.Add(outputSaleMerged);

            return sale;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (saleToReturn?.Order == null) return saleToReturn;

        types = new[] {
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            OrderPackage orderPackage = (OrderPackage)objects[0];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[1];
            User user = (User)objects[2];
            UserRole userRole = (UserRole)objects[3];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];

            if (!saleToReturn.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleToReturn.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleToReturn.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [OrderPackage] " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [OrderPackage].Deleted = 0 " +
            "AND [OrderPackage].OrderID = @OrderId",
            types,
            packagesMapper,
            new {
                OrderId = saleToReturn.Order.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        return saleToReturn;
    }

    public Sale GetByNetIdWithAgreementOnly(Guid netId) {
        return _connection.Query<Sale, ClientAgreement, Agreement, Sale>(
            "SELECT * " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [Sale].NetUID = @NetId",
            (sale, clientAgreement, agreement) => {
                clientAgreement.Agreement = agreement;
                sale.ClientAgreement = clientAgreement;
                return sale;
            },
            new {
                NetId = netId
            }
        ).FirstOrDefault();
    }

    public void SetIsAcceptedToPackingFalse(long id) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET [Updated] = getutcdate() " +
            ", [IsAcceptedToPacking] = 0 " +
            "WHERE [Sale].[ID] = @Id; ",
            new { Id = id });
    }

    public void SetIsPrintedFalse(long id) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET [Updated] = getutcdate() " +
            ", [IsPrinted] = 0 " +
            "WHERE [Sale].[ID] = @Id; ",
            new { Id = id });
    }

    public void SetIsPrintedActProtocolEditFalse(long id) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET [Updated] = getutcdate() " +
            ", [IsPrintedActProtocolEdit] = 0 " +
            "WHERE [Sale].[ID] = @Id; ",
            new { Id = id });
    }

    public Sale GetByNetId(Guid netId) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT [Sale].* " +
            ",[ClientAgreement].* " +
            ",[Order].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].NetUID " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
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
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[Client].* " +
            ",[ClientInRole].* " +
            ",[ClientTypeRole].* " +
            ",[RegionCode].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[BaseSalePaymentStatus].* " +
            ",[SaleNumber].* " +
            ",[OrderItemUser].* " +
            ",[CurrencyTranslation].* " +
            ",[SaleBaseShiftStatus].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ClientSubClient].* " +
            ",[Organization].* " +
            ",[VatRate].* " +
            ",[PaymentRegister].* " +
            ",[MeasureUnit].* " +
            ",[RetailClient].* " +
            ",[Workplace].* " +
            "FROM [Sale] " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN  [Product] " +
            "ON [Product].ID = OrderItem.ProductID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN ClientInRole " +
            "ON ClientInRole.ClientID = Client.ID " +
            "LEFT JOIN ClientTypeRole " +
            "ON ClientTypeRole.ID = ClientInRole.ClientTypeRoleID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusID = BaseLifeCycleStatus.ID " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberID = SaleNumber.ID And SaleNumber.Deleted = 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ProductID = Product.ID " +
            "AND ProductAvailability.Deleted = 0 " +
            "LEFT JOIN Storage " +
            "ON Storage.ID = ProductAvailability.StorageID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
            "AND [PaymentRegister].[Deleted] = 0 " +
            "AND [PaymentRegister].[IsMain] = 1 " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].[ID] = [Sale].[RetailClientId] " +
            "LEFT JOIN [Workplace] " +
            "ON [Workplace].ID = [Sale].WorkplaceID " +
            "WHERE Sale.NetUID = @NetId";

        Type[] types = {
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientTypeRole),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(CurrencyTranslation),
            typeof(SaleBaseShiftStatus),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ClientSubClient),
            typeof(Organization),
            typeof(VatRate),
            typeof(PaymentRegister),
            typeof(MeasureUnit),
            typeof(RetailClient),
            typeof(Workplace)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Order order = (Order)objects[2];
            OrderItem orderItem = (OrderItem)objects[3];
            Product product = (Product)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Currency currency = (Currency)objects[6];
            Client client = (Client)objects[7];
            ClientInRole clientInRole = (ClientInRole)objects[8];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[11];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[12];
            SaleNumber saleNumber = (SaleNumber)objects[13];
            User orderItemUser = (User)objects[14];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[15];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[16];
            ProductAvailability productAvailability = (ProductAvailability)objects[17];
            Storage productAvailabilityStorage = (Storage)objects[18];
            ClientSubClient clientSubClient = (ClientSubClient)objects[19];
            Organization organization = (Organization)objects[20];
            VatRate vatRate = (VatRate)objects[21];
            PaymentRegister paymentRegister = (PaymentRegister)objects[22];
            MeasureUnit measureUnit = (MeasureUnit)objects[23];
            RetailClient retailClient = (RetailClient)objects[24];
            Workplace workplace = (Workplace)objects[25];

            if (saleToReturn == null) {
                sale.ShiftStatus = saleBaseShiftStatus;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (clientAgreement != null) {
                    if (agreement != null) {
                        if (currency != null) {
                            currency.Name = currencyTranslation?.Name;

                            agreement.Currency = currency;
                        }

                        organization.MainPaymentRegister = paymentRegister;
                        organization.VatRate = vatRate;
                        agreement.Organization = organization;

                        clientAgreement.Agreement = agreement;
                    }

                    if (client != null) {
                        if (regionCode != null) client.RegionCode = regionCode;

                        if (clientSubClient != null) client.SubClients.Add(clientSubClient);

                        if (clientInRole != null) {
                            clientInRole.ClientTypeRole = clientTypeRole;
                            client.ClientInRole = clientInRole;
                        }

                        clientAgreement.Client = client;
                    }

                    sale.ClientAgreement = clientAgreement;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                if (retailClient != null) {
                    retailClient.ShoppingCartJson = string.Empty;
                    sale.RetailClient = retailClient;
                }

                sale.Workplace = workplace;
                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            product.MeasureUnit = measureUnit;

            orderItem.TotalAmount = product.CurrentPrice * Convert.ToDecimal(orderItem.Qty);
            orderItem.TotalAmountLocal = product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);
            orderItem.Product = product;
            orderItem.User = orderItemUser;

            if (!saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                saleToReturn.TotalWeight += product.Weight * orderItem.Qty;

                saleToReturn.Order.OrderItems.Add(orderItem);
            }

            return sale;
        };

        string twoLetterCultureName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl") ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName : "uk";

        var props = new { NetId = netId, Culture = twoLetterCultureName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (saleToReturn?.Order == null) return saleToReturn;

        Type[] orderItems = {
            typeof(OrderItem),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(User),
            typeof(ProductSpecification),
            typeof(ProductGroupDiscount),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], OrderItem> orderItemMapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            Pricing pricing = (Pricing)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            User discountUpdatedBy = (User)objects[6];
            ProductSpecification assignedSpecification = (ProductSpecification)objects[7];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[8];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[9];

            OrderItem existOrderItem = saleToReturn.Order.OrderItems.FirstOrDefault(x => x.Id.Equals(orderItem.Id));

            if (existOrderItem == null)
                return orderItem;

            if (productPricing != null) {
                if (pricing != null)
                    productPricing.Pricing = pricing;

                if (!existOrderItem.Product.ProductPricings.Any(x => x.Id.Equals(productPricing.Id)))
                    existOrderItem.Product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null && !existOrderItem.Product.ProductProductGroups
                    .Any(x => x.Id.Equals(productProductGroup.Id)))
                existOrderItem.Product.ProductProductGroups.Add(productProductGroup);

            if (orderItemBaseShiftStatus != null && !existOrderItem.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id)))
                existOrderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

            existOrderItem.Product.MeasureUnit = measureUnit;

            existOrderItem.DiscountUpdatedBy = discountUpdatedBy;

            existOrderItem.AssignedSpecification = assignedSpecification;

            if (productGroupDiscount != null)
                existOrderItem.Discount = Convert.ToDecimal(productGroupDiscount.DiscountRate);

            return orderItem;
        };

        _connection.Query(
            "SELECT " +
            "[OrderItem].* " +
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[MeasureUnit].* " +
            ",[DiscountUpdatedUser].* " +
            ",[ProductSpecification].* " +
            ",[ProductGroupDiscount].* " +
            ",[OrderItemBaseShiftStatus].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN Pricing " +
            "ON ProductPricing.PricingID = Pricing.ID " +
            "AND Pricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID " +
            "AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderItem].[AssignedSpecificationID] " +
            "LEFT JOIN [ProductGroupDiscount] " +
            "ON [ProductGroupDiscount].[ProductGroupID] = [ProductGroup].[ID] " +
            "AND [ProductGroupDiscount].[ClientAgreementID] = [ClientAgreement].[ID] " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].[OrderItemID] = [OrderItem].[ID] " +
            "WHERE Sale.NetUID = @NetId ",
            orderItems,
            orderItemMapper,
            new {
                NetId = saleToReturn.NetUid,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });

        Type[] saleInfoTypes = {
            typeof(Sale),
            typeof(User),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(SaleMerged),
            typeof(SaleInvoiceDocument),
            typeof(SaleInvoiceNumber),
            typeof(CustomersOwnTtn)
        };

        Func<object[], Sale> saleInfoMapper = objects => {
            Sale sale = (Sale)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[3];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[4];
            SaleMerged inputSaleMerged = (SaleMerged)objects[5];
            SaleMerged outputSaleMerged = (SaleMerged)objects[6];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[7];
            SaleInvoiceNumber saleInvoiceNumber = (SaleInvoiceNumber)objects[8];
            CustomersOwnTtn customersOwnTtn = (CustomersOwnTtn)objects[9];

            saleToReturn.SaleInvoiceDocument = saleInvoiceDocument;

            if (inputSaleMerged != null && !saleToReturn.InputSaleMerges.Any(x => x.Id.Equals(inputSaleMerged.Id)))
                saleToReturn.InputSaleMerges.Add(inputSaleMerged);

            if (outputSaleMerged != null && !saleToReturn.OutputSaleMerges.Any(x => x.Id.Equals(outputSaleMerged.Id)))
                saleToReturn.OutputSaleMerges.Add(outputSaleMerged);

            if (deliveryRecipient != null)
                saleToReturn.DeliveryRecipient = deliveryRecipient;

            if (deliveryRecipientAddress != null)
                saleToReturn.DeliveryRecipientAddress = deliveryRecipientAddress;

            if (transporter != null)
                saleToReturn.Transporter = transporter;

            if (customersOwnTtn != null)
                saleToReturn.CustomersOwnTtn = customersOwnTtn;

            saleToReturn.SaleInvoiceNumber = saleInvoiceNumber;

            if (user == null) return sale;

            saleToReturn.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
            saleToReturn.User = user;

            return sale;
        };

        _connection.Query(
            "SELECT " +
            "[Sale].* " +
            ",[User].* " +
            ",[Transporter].* " +
            ",[DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            ",[InputSaleMerged].* " +
            ",[OutputSaleMerged].* " +
            ",[SaleInvoiceDocument].* " +
            ",[SaleInvoiceNumber].* " +
            ",[CustomersOwnTtn].* " +
            "FROM [Sale] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = Sale.UserID " +
            "LEFT JOIN Transporter " +
            "ON Transporter.ID = Sale.TransporterID " +
            //"AND Transporter.Deleted = 0 " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged AS InputSaleMerged " +
            "ON InputSaleMerged.OutputSaleID = Sale.ID " +
            "LEFT JOIN SaleMerged AS OutputSaleMerged " +
            "ON OutputSaleMerged.InputSaleID = Sale.ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = Sale.SaleInvoiceDocumentID " +
            "LEFT JOIN [SaleInvoiceNumber] " +
            "ON [SaleInvoiceNumber].ID = [Sale].SaleInvoiceNumberID " +
            "LEFT JOIN [CustomersOwnTtn] " +
            "ON [CustomersOwnTtn].ID = [Sale].CustomersOwnTtnID " +
            "WHERE Sale.NetUID = @NetId ",
            saleInfoTypes,
            saleInfoMapper,
            new { NetId = saleToReturn.NetUid }
        );

        types = new[] {
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            OrderPackage orderPackage = (OrderPackage)objects[0];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[1];
            User user = (User)objects[2];
            UserRole userRole = (UserRole)objects[3];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];

            if (!saleToReturn.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleToReturn.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleToReturn.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [OrderPackage] " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [OrderPackage].Deleted = 0 " +
            "AND [OrderPackage].OrderID = @OrderId",
            types,
            packagesMapper,
            new {
                OrderId = saleToReturn.Order.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);
        Type[] typesUpdateDataCarrier = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapperUpdateDataCarrier = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) updateDataCarrier.User = user;

            if (transporter != null) updateDataCarrier.Transporter = transporter;

            saleToReturn.UpdateDataCarrier.Add(updateDataCarrier);
            return updateDataCarrier;
        };

        _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "WHERE [UpdateDataCarrier].SaleId = @Id " +
            "AND [UpdateDataCarrier].IsEditTransporter = 0",
            typesUpdateDataCarrier,
            mapperUpdateDataCarrier,
            new {
                saleToReturn.Id
            });

        Type[] typesWarehousesShipment = {
            typeof(WarehousesShipment),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], WarehousesShipment> mapperWarehousesShipment = objects => {
            WarehousesShipment WarehousesShipment = (WarehousesShipment)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) WarehousesShipment.User = user;

            if (transporter != null) WarehousesShipment.Transporter = transporter;

            saleToReturn.WarehousesShipment = WarehousesShipment;
            return WarehousesShipment;
        };

        _connection.Query(
            "SELECT " +
            "[WarehousesShipment].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [WarehousesShipment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [WarehousesShipment].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
            "WHERE [WarehousesShipment].SaleId = @Id ",
            typesWarehousesShipment,
            mapperWarehousesShipment,
            new {
                saleToReturn.Id
            });

        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];


            if (!saleToReturn.HistoryInvoiceEdit.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                saleToReturn.HistoryInvoiceEdit.Add(historyInvoice);
            } else {
                HistoryInvoiceEdit historyinvoiceEditFromList = saleToReturn.HistoryInvoiceEdit.First(s => s.Id.Equals(historyInvoice.Id));

                historyinvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditID = HistoryInvoiceEdit.ID " +
            "WHERE HistoryInvoiceEdit.SaleID = @Id " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new {
                saleToReturn.Id
            }
        );

        return saleToReturn;
    }

    public Sale GetByNetIdWithProductLocations(Guid netId) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT [Sale].* " +
            ",[ClientAgreement].* " +
            ",[Order].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].NetUID " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
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
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[Client].* " +
            ",[ClientInRole].* " +
            ",[ClientTypeRole].* " +
            ",[RegionCode].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[BaseSalePaymentStatus].* " +
            ",[SaleNumber].* " +
            ",[OrderItemUser].* " +
            ",[CurrencyTranslation].* " +
            ",[SaleBaseShiftStatus].* " +
            ",[OrderItemBaseShiftStatus].* " +
            ",[OrderItemBaseShiftStatusUser].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ClientSubClient].* " +
            ",[Organization].* " +
            ",[VatRate].* " +
            ",[PaymentRegister].* " +
            ",[RetailClient].* " +
            ",[CustomersOwnTtn].* " +
            "FROM [Sale] " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            //"AND OrderItem.Qty > 0 " +
            "LEFT JOIN  [Product] " +
            "ON [Product].ID = OrderItem.ProductID " +
            "AND Product.Deleted = 0 " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN ClientInRole " +
            "ON ClientInRole.ClientID = Client.ID " +
            "LEFT JOIN ClientTypeRole " +
            "ON ClientTypeRole.ID = ClientInRole.ClientTypeRoleID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusID = BaseLifeCycleStatus.ID " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberID = SaleNumber.ID And SaleNumber.Deleted = 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN [User] AS OrderItemBaseShiftStatusUser " +
            "ON OrderItemBaseShiftStatusUser.ID = OrderItemBaseShiftStatus.UserId " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ProductID = Product.ID " +
            "AND ProductAvailability.Deleted = 0 " +
            "LEFT JOIN Storage " +
            "ON Storage.ID = ProductAvailability.StorageID " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
            "AND [PaymentRegister].[Deleted] = 0 " +
            "AND [PaymentRegister].[IsMain] = 1 " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [Sale].RetailClientId " +
            "LEFT JOIN [CustomersOwnTtn] " +
            "ON CustomersOwnTtn.ID = Sale.CustomersOwnTtnID " +
            "WHERE Sale.NetUID = @NetId";

        Type[] types = {
            typeof(Sale),
            typeof(ClientAgreement),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientTypeRole),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(CurrencyTranslation),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(User),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ClientSubClient),
            typeof(Organization),
            typeof(VatRate),
            typeof(PaymentRegister),
            typeof(RetailClient),
            typeof(CustomersOwnTtn)
        };

        List<long> orderItemIds = new();

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Order order = (Order)objects[2];
            OrderItem orderItem = (OrderItem)objects[3];
            Product product = (Product)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Currency currency = (Currency)objects[6];
            Client client = (Client)objects[7];
            ClientInRole clientInRole = (ClientInRole)objects[8];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[11];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[12];
            SaleNumber saleNumber = (SaleNumber)objects[13];
            User orderItemUser = (User)objects[14];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[15];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[16];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[17];
            User orderItemBaseShiftStatusUser = (User)objects[18];
            ProductAvailability productAvailability = (ProductAvailability)objects[19];
            Storage productAvailabilityStorage = (Storage)objects[20];
            ClientSubClient clientSubClient = (ClientSubClient)objects[21];
            Organization organization = (Organization)objects[22];
            VatRate vatRate = (VatRate)objects[23];
            PaymentRegister paymentRegister = (PaymentRegister)objects[24];
            RetailClient retailClient = (RetailClient)objects[25];
            CustomersOwnTtn customersOwnTtn = (CustomersOwnTtn)objects[26];

            if (saleToReturn == null) {
                sale.RetailClient = retailClient;
                sale.ShiftStatus = saleBaseShiftStatus;

                if (customersOwnTtn != null) sale.CustomersOwnTtn = customersOwnTtn;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (clientAgreement != null) {
                    if (agreement != null) {
                        if (currency != null) {
                            currency.Name = currencyTranslation?.Name;

                            agreement.Currency = currency;
                        }

                        organization.VatRate = vatRate;
                        organization.MainPaymentRegister = paymentRegister;
                        agreement.Organization = organization;

                        clientAgreement.Agreement = agreement;
                    }

                    if (client != null) {
                        if (regionCode != null) client.RegionCode = regionCode;

                        if (clientSubClient != null) client.SubClients.Add(clientSubClient);

                        if (clientInRole != null) {
                            clientInRole.ClientTypeRole = clientTypeRole;
                            client.ClientInRole = clientInRole;
                        }

                        clientAgreement.Client = client;
                    }

                    sale.ClientAgreement = clientAgreement;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            orderItem.TotalAmount = product.CurrentPrice * Convert.ToDecimal(orderItem.Qty);

            orderItem.Product = product;
            orderItem.User = orderItemUser;

            if (saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                OrderItem orderItemFormArray = saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (orderItemBaseShiftStatus != null && !orderItemFormArray.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    orderItemBaseShiftStatus.User = orderItemBaseShiftStatusUser;
                    orderItemFormArray.ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                orderItemIds.Add(orderItem.Id);

                saleToReturn.Order.OrderItems.Add(orderItem);
            }

            return sale;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props, commandTimeout: 3600);

        if (saleToReturn?.Order == null) return saleToReturn;

        Type[] orderItemTypes = {
            typeof(OrderItem),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(User),
            typeof(ProductSpecification)
        };

        Func<object[], OrderItem> orderItemMapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            Pricing pricing = (Pricing)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup ProductGroup = (ProductGroup)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            User discountUpdatedBy = (User)objects[6];
            ProductSpecification assignedSpecification = (ProductSpecification)objects[7];

            OrderItem existOrderItem = saleToReturn.Order.OrderItems.First(x => x.Id.Equals(orderItem.Id));

            if (productPricing != null) {
                if (pricing != null) productPricing.Pricing = pricing;

                existOrderItem.Product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null) existOrderItem.Product.ProductProductGroups.Add(productProductGroup);

            existOrderItem.Product.MeasureUnit = measureUnit;

            existOrderItem.DiscountUpdatedBy = discountUpdatedBy;

            existOrderItem.AssignedSpecification = assignedSpecification;

            return orderItem;
        };

        _connection.Query(
            "SELECT " +
            "[OrderItem].* " +
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[MeasureUnit].* " +
            ",[DiscountUpdatedUser].* " +
            ",[ProductSpecification].* " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            //"AND OrderItem.Qty > 0 " +
            "LEFT JOIN  [Product] " +
            "ON [Product].ID = OrderItem.ProductID " +
            "AND Product.Deleted = 0 " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN Pricing " +
            "ON ProductPricing.PricingID = Pricing.ID " +
            "AND Pricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID " +
            "AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [DiscountUpdatedUser] " +
            "ON [DiscountUpdatedUser].ID = [OrderItem].DiscountUpdatedByID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderItem].[AssignedSpecificationID] " +
            "WHERE Sale.NetUID = @NetId ",
            orderItemTypes,
            orderItemMapper,
            new {
                NetId = saleToReturn.NetUid,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });

        Type[] saleInfoTypes = {
            typeof(Sale),
            typeof(User),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(SaleMerged),
            typeof(SaleMerged),
            typeof(SaleInvoiceDocument),
            typeof(SaleInvoiceNumber)
        };

        Func<object[], Sale> saleInfoMapper = objects => {
            Sale sale = (Sale)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[3];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[4];
            SaleMerged inputSaleMerged = (SaleMerged)objects[5];
            SaleMerged outputSaleMerged = (SaleMerged)objects[6];
            SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[7];
            SaleInvoiceNumber saleInvoiceNumber = (SaleInvoiceNumber)objects[8];

            if (inputSaleMerged != null && !saleToReturn.InputSaleMerges.Any(x => x.Id.Equals(inputSaleMerged.Id)))
                saleToReturn.InputSaleMerges.Add(inputSaleMerged);

            if (outputSaleMerged != null && !saleToReturn.OutputSaleMerges.Any(x => x.Id.Equals(outputSaleMerged.Id)))
                saleToReturn.OutputSaleMerges.Add(outputSaleMerged);

            saleToReturn.DeliveryRecipient = deliveryRecipient;

            saleToReturn.DeliveryRecipientAddress = deliveryRecipientAddress;

            saleToReturn.Transporter = transporter;

            saleToReturn.SaleInvoiceNumber = saleInvoiceNumber;

            saleToReturn.SaleInvoiceDocument = saleInvoiceDocument;

            if (user == null) return sale;

            saleToReturn.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
            saleToReturn.User = user;

            return sale;
        };

        _connection.Query(
            "SELECT " +
            "[Sale].* " +
            ",[User].* " +
            ",[Transporter].* " +
            ",[DeliveryRecipient].* " +
            ",[DeliveryRecipientAddress].* " +
            ",[InputSaleMerged].* " +
            ",[OutputSaleMerged].* " +
            ",[SaleInvoiceDocument].* " +
            ",[SaleInvoiceNumber].* " +
            "FROM [Sale] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = Sale.UserID " +
            "LEFT JOIN Transporter " +
            "ON Transporter.ID = Sale.TransporterID " +
            //"AND Transporter.Deleted = 0 " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "LEFT JOIN SaleMerged AS InputSaleMerged " +
            "ON InputSaleMerged.OutputSaleID = Sale.ID " +
            "LEFT JOIN SaleMerged AS OutputSaleMerged " +
            "ON OutputSaleMerged.InputSaleID = Sale.ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = Sale.SaleInvoiceDocumentID " +
            "LEFT JOIN [SaleInvoiceNumber] " +
            "ON [SaleInvoiceNumber].ID = [Sale].SaleInvoiceNumberID " +
            "WHERE Sale.NetUID = @NetId ",
            saleInfoTypes,
            saleInfoMapper,
            new { NetId = saleToReturn.NetUid }
        );

        types = new[] {
            typeof(OrderPackage),
            typeof(OrderPackageUser),
            typeof(User),
            typeof(UserRole),
            typeof(OrderPackageItem),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], OrderPackage> packagesMapper = objects => {
            OrderPackage orderPackage = (OrderPackage)objects[0];
            OrderPackageUser orderPackageUser = (OrderPackageUser)objects[1];
            User user = (User)objects[2];
            UserRole userRole = (UserRole)objects[3];
            OrderPackageItem orderPackageItem = (OrderPackageItem)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];

            if (!saleToReturn.Order.OrderPackages.Any(p => p.Id.Equals(orderPackage.Id))) {
                if (orderPackageUser != null) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    orderPackage.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem != null) {
                    product.MeasureUnit = measureUnit;

                    orderItem.Product = product;

                    orderPackageItem.OrderItem = orderItem;

                    orderPackage.OrderPackageItems.Add(orderPackageItem);
                }

                saleToReturn.Order.OrderPackages.Add(orderPackage);
            } else {
                OrderPackage fromList = saleToReturn.Order.OrderPackages.First(p => p.Id.Equals(orderPackage.Id));

                if (orderPackageUser != null && !fromList.OrderPackageUsers.Any(u => u.Id.Equals(orderPackageUser.Id))) {
                    user.UserRole = userRole;

                    orderPackageUser.User = user;

                    fromList.OrderPackageUsers.Add(orderPackageUser);
                }

                if (orderPackageItem == null || fromList.OrderPackageItems.Any(i => i.Id.Equals(orderPackageItem.Id))) return orderPackage;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                orderPackageItem.OrderItem = orderItem;

                fromList.OrderPackageItems.Add(orderPackageItem);
            }

            return orderPackage;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [OrderPackage] " +
            "LEFT JOIN [OrderPackageUser] " +
            "ON [OrderPackageUser].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageUser].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderPackageUser].UserID " +
            "LEFT JOIN ( " +
            "SELECT [UserRole].Created " +
            ",[UserRole].Dashboard " +
            ",[UserRole].Deleted " +
            ",[UserRole].ID " +
            ",[UserRole].NetUID " +
            ",[UserRole].Updated " +
            ",[UserRole].UserRoleType " +
            ",[UserRoleTranslation].Name " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            ") AS [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT JOIN [OrderPackageItem] " +
            "ON [OrderPackageItem].OrderPackageID = [OrderPackage].ID " +
            "AND [OrderPackageItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderPackageItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [OrderPackage].Deleted = 0 " +
            "AND [OrderPackage].OrderID = @OrderId",
            types,
            packagesMapper,
            new {
                OrderId = saleToReturn.Order.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        _connection.Query<ProductLocation, ProductPlacement, ProductLocation>(
            "SELECT * " +
            "FROM [ProductLocation] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
            "WHERE [ProductLocation].Deleted = 0 " +
            "AND [ProductLocation].OrderItemID IN @Ids",
            (location, placement) => {
                location.ProductPlacement = placement;

                saleToReturn.Order.OrderItems.First(i => location.OrderItemId != null && i.Id.Equals(location.OrderItemId.Value)).ProductLocations.Add(location);

                return location;
            },
            new {
                Ids = orderItemIds
            }
        );

        _connection.Query<ProductLocationHistory, ProductPlacement, ProductLocationHistory>(
            "SELECT * " +
            "FROM [ProductLocationHistory] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocationHistory].ProductPlacementID " +
            "WHERE [ProductLocationHistory].Deleted = 0 " +
            "AND [ProductLocationHistory].OrderItemID IN @Ids",
            (location, placement) => {
                location.ProductPlacement = placement;
                //ProductLocationHistory productLocationHistory = saleToReturn.Order.OrderItems.First(i => location.OrderItemId != null && i.Id.Equals(location.OrderItemId.Value)).ProductLocationsHistory.FirstOrDefault(x => x.ProductPlacementId == location.ProductPlacementId && x.TypeOfMovement == location.TypeOfMovement);
                //if (productLocationHistory == null) {
                saleToReturn.Order.OrderItems.First(i => location.OrderItemId != null && i.Id.Equals(location.OrderItemId.Value)).ProductLocationsHistory.Add(location);
                //} else {
                //    productLocationHistory.Qty += location.Qty;
                //}

                return location;
            },
            new {
                Ids = orderItemIds
            }
        );

        if (saleToReturn.ClientAgreement.Agreement.Organization == null) return saleToReturn;

        IEnumerable<PaymentRegister> registers =
            _connection.Query<PaymentRegister>(
                "SELECT * FROM PaymentRegister " +
                "WHERE OrganizationID = @Id " +
                "UNION ALL " +
                "SELECT * FROM PaymentRegister " +
                "WHERE OrganizationID = @Id " +
                "AND PaymentRegister.IsMain = 1 ",
                new { ID = saleToReturn.ClientAgreement.Agreement.Organization.Id }
            );

        if (registers.Any())
            saleToReturn.ClientAgreement.Agreement.Organization.MainPaymentRegister =
                registers.Any(x => x.IsMain)
                    ? registers.First(x => x.IsMain)
                    : registers.First(x => !string.IsNullOrEmpty(x.AccountNumber));

        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus),
            typeof(ProductLocationHistory),
            typeof(ProductPlacement)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];
            ProductLocationHistory productLocationHistory = (ProductLocationHistory)objects[2];
            ProductPlacement productPlacement = (ProductPlacement)objects[3];


            if (!saleToReturn.HistoryInvoiceEdit.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                if (productPlacement != null) productLocationHistory.ProductPlacement = productPlacement;

                historyInvoice.ProductLocationHistory.Add(productLocationHistory);
                saleToReturn.HistoryInvoiceEdit.Add(historyInvoice);
            } else {
                HistoryInvoiceEdit historyinvoiceEditFromList = saleToReturn.HistoryInvoiceEdit.First(s => s.Id.Equals(historyInvoice.Id));
                if (!historyinvoiceEditFromList.OrderItemBaseShiftStatuses.Any(x => x.Id.Equals(orderItemBaseShiftStatus.Id)))
                    historyinvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                if (productLocationHistory != null)
                    if (!historyinvoiceEditFromList.ProductLocationHistory.Any(x => x.Id.Equals(productLocationHistory.Id))) {
                        if (productPlacement != null) productLocationHistory.ProductPlacement = productPlacement;

                        historyinvoiceEditFromList.ProductLocationHistory.Add(productLocationHistory);
                    }
            }


            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditID = HistoryInvoiceEdit.ID " +
            "LEFT JOIN [ProductLocationHistory] " +
            "ON [ProductLocationHistory].HistoryInvoiceEditID = HistoryInvoiceEdit.ID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocationHistory].ProductPlacementID " +
            "WHERE HistoryInvoiceEdit.SaleID = @Id " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new {
                saleToReturn.Id
            }
        );

        return saleToReturn;
    }

    public Sale GetByNetIdWithDeletedOrderItems(Guid netId) {
        Sale saleToReturn = null;

        string sqlExpression =
            ";WITH ProductPricing_CTE (ID, PricingID, ProductID, Price) " +
            "AS " +
            "( " +
            "SELECT " +
            "ROW_NUMBER() OVER (ORDER BY Pricing.ID) AS ID, " +
            "[Pricing].ID, " +
            "[Product].ID, " +
            "( " +
            "CASE " +
            "WHEN ISNULL([OrderItem].PricePerItem, 0) > 0 " +
            "THEN [OrderItem].PricePerItem " +
            "ELSE " +
            "( " +
            "[ProductPricing].Price " +
            "+ " +
            "([ProductPricing].Price * ISNULL(dbo.GetPricingExtraCharge([Pricing].NetUID), 0) / 100) " +
            ") " +
            "- " +
            "( " +
            "( " +
            "[ProductPricing].Price " +
            "+ " +
            "([ProductPricing].Price * ISNULL(dbo.GetPricingExtraCharge([Pricing].NetUID), 0) / 100) " +
            ") " +
            "* " +
            "ISNULL([ProductGroupDiscount].DiscountRate, 0) " +
            ") / 100 " +
            "END " +
            ") " +
            "AS [Price] " +
            "FROM [Sale] " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "AND Product.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ID = ( " +
            "SELECT TOP(1) ID FROM ProductGroupDiscount " +
            "WHERE ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupId = ProductProductGroup.ProductGroupId " +
            "AND IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            ") " +
            "LEFT JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ID = (SELECT TOP(1) ID FROM ProductPricing WHERE ProductID = Product.ID AND ProductPricing.Deleted = 0) " +
            "WHERE Sale.NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
            "LEFT JOIN ProductPricing_CTE " +
            "ON ProductPricing_CTE.ProductID = Product.ID " +
            "LEFT JOIN Pricing " +
            "ON ProductPricing_CTE.PricingID = Pricing.ID AND Pricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = Sale.UserID AND [User].Deleted = 0 " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusID = BaseLifeCycleStatus.ID " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN SaleNumber " +
            "ON Sale.SaleNumberID = SaleNumber.ID And SaleNumber.Deleted = 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ProductID = Product.ID " +
            "LEFT JOIN Storage " +
            "ON Storage.ID = ProductAvailability.StorageID " +
            "WHERE Sale.NetUID = @NetId ";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(CurrencyTranslation),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(ProductAvailability),
            typeof(Storage)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ProductPricing productPricing = (ProductPricing)objects[4];
            Pricing pricing = (Pricing)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            User user = (User)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];
            Client client = (Client)objects[12];
            RegionCode regionCode = (RegionCode)objects[13];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[14];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[15];
            SaleNumber saleNumber = (SaleNumber)objects[16];
            User orderItemUser = (User)objects[17];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[18];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[19];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[20];
            ProductAvailability productAvailability = (ProductAvailability)objects[21];
            Storage productAvailabilityStorage = (Storage)objects[22];

            if (saleToReturn == null) {
                sale.ShiftStatus = saleBaseShiftStatus;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (clientAgreement != null) {
                    if (agreement != null) {
                        if (currency != null) {
                            currency.Name = currencyTranslation?.Name;

                            agreement.Currency = currency;
                        }

                        clientAgreement.Agreement = agreement;
                    }

                    if (client != null) {
                        if (regionCode != null) client.RegionCode = regionCode;

                        clientAgreement.Client = client;
                    }

                    sale.ClientAgreement = clientAgreement;
                }

                if (user != null) {
                    sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                    sale.User = user;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (productPricing != null) {
                if (pricing != null) productPricing.Pricing = pricing;

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.TotalAmount = productPricing.Price * Convert.ToDecimal(orderItem.Qty);

                product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

            orderItem.User = orderItemUser;
            orderItem.Product = product;

            if (saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                OrderItem orderItemFormArray = saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (productPricing != null && !orderItemFormArray.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemFormArray.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemFormArray.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemFormArray.Product.ProductProductGroups.Add(productProductGroup);

                if (orderItemBaseShiftStatus != null && !orderItemFormArray.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id)))
                    orderItemFormArray.ShiftStatuses.Add(orderItemBaseShiftStatus);
            } else {
                saleToReturn.Order.OrderItems.Add(orderItem);
            }

            return sale;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (saleToReturn?.Order != null) saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        return saleToReturn;
    }

    public Sale GetByNetIdWithShiftedItems(Guid netId) {
        Sale saleToReturn = null;

        string sqlExpression =
            ";WITH ProductPricing_CTE (ID, PricingID, ProductID, Price) " +
            "AS " +
            "( " +
            "SELECT " +
            "ROW_NUMBER() OVER (ORDER BY Pricing.ID) AS ID, " +
            "[Pricing].ID, " +
            "[Product].ID, " +
            "( " +
            "CASE " +
            "WHEN ISNULL([OrderItem].PricePerItem, 0) > 0 " +
            "THEN [OrderItem].PricePerItem " +
            "ELSE " +
            "( " +
            "[ProductPricing].Price " +
            "+ " +
            "([ProductPricing].Price * ISNULL(dbo.GetPricingExtraCharge([Pricing].NetUID), 0) / 100) " +
            ") " +
            "- " +
            "( " +
            "( " +
            "[ProductPricing].Price " +
            "+ " +
            "([ProductPricing].Price * ISNULL(dbo.GetPricingExtraCharge([Pricing].NetUID), 0) / 100) " +
            ") " +
            "* " +
            "ISNULL([ProductGroupDiscount].DiscountRate, 0) " +
            ") / 100 " +
            "END " +
            ") " +
            "AS [Price] " +
            "FROM [Sale] " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "AND [Order].Deleted = 0 " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "AND OrderItem.Qty > 0 " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "AND Product.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ID = ( " +
            "SELECT TOP(1) ID FROM ProductGroupDiscount " +
            "WHERE ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupId = ProductProductGroup.ProductGroupId " +
            "AND IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            ") " +
            "LEFT JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ID = (SELECT TOP(1) ID FROM ProductPricing WHERE ProductID = Product.ID AND ProductPricing.Deleted = 0) " +
            "WHERE Sale.NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
            "LEFT OUTER JOIN OrderItem " +
            "ON OrderItem.OrderId = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
            "LEFT OUTER JOIN ProductPricing_CTE " +
            "ON ProductPricing_CTE.ProductID = Product.ID " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing_CTE.PricingID = Pricing.ID AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN [User] " +
            "ON [User].ID = Sale.UserID AND [User].Deleted = 0 " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT OUTER JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT OUTER JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID AND ClientAgreement.Deleted = 0 " +
            "LEFT OUTER JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID AND RegionCode.Deleted = 0 " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusID = BaseLifeCycleStatus.ID " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT OUTER JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT OUTER JOIN SaleNumber " +
            "ON Sale.SaleNumberID = SaleNumber.ID And SaleNumber.Deleted = 0 " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN [User] AS OrderItemBaseShiftStatusUser " +
            "ON OrderItemBaseShiftStatusUser.ID = OrderItemBaseShiftStatus.UserID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID]" +
            "WHERE Sale.NetUID = @NetId ";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(CurrencyTranslation),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(User),
            typeof(Organization)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ProductPricing productPricing = (ProductPricing)objects[4];
            Pricing pricing = (Pricing)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            User user = (User)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];
            Client client = (Client)objects[12];
            RegionCode regionCode = (RegionCode)objects[13];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[14];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[15];
            SaleNumber saleNumber = (SaleNumber)objects[16];
            User orderItemUser = (User)objects[17];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[18];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[19];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[20];
            User userOrderItemBaseShiftStatus = (User)objects[21];
            Organization organization = (Organization)objects[22];

            if (saleToReturn == null) {
                sale.ShiftStatus = saleBaseShiftStatus;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (clientAgreement != null) {
                    if (agreement != null) {
                        if (currency != null) {
                            currency.Name = currencyTranslation?.Name;

                            agreement.Currency = currency;
                        }

                        agreement.Organization = organization;
                        clientAgreement.Agreement = agreement;
                    }

                    if (client != null) {
                        if (regionCode != null) client.RegionCode = regionCode;

                        clientAgreement.Client = client;
                    }

                    sale.ClientAgreement = clientAgreement;
                }

                if (user != null) {
                    sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                    sale.User = user;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productPricing != null) {
                if (pricing != null) productPricing.Pricing = pricing;

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.TotalAmount = productPricing.Price * Convert.ToDecimal(orderItem.Qty);

                product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

            orderItem.Product = product;
            orderItem.User = orderItemUser;

            if (saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                OrderItem orderItemFormArray = saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (productPricing != null && !orderItemFormArray.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemFormArray.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemFormArray.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemFormArray.Product.ProductProductGroups.Add(productProductGroup);

                if (orderItemBaseShiftStatus != null && orderItemBaseShiftStatus.Qty != 0 &&
                    !orderItemFormArray.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    orderItemBaseShiftStatus.User = userOrderItemBaseShiftStatus;
                    orderItemFormArray.ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (orderItemBaseShiftStatus != null && orderItemBaseShiftStatus.Qty != 0) {
                    orderItemBaseShiftStatus.User = userOrderItemBaseShiftStatus;
                    orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);
                }

                saleToReturn.Order.OrderItems.Add(orderItem);
            }


            return sale;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (saleToReturn?.Order != null) saleToReturn.TotalCount = saleToReturn.Order.OrderItems.Sum(i => i.Qty);

        if (saleToReturn?.Order == null || !saleToReturn.Order.OrderItems.Any()) return saleToReturn;

        Type[] orderItemInfoTypes = {
            typeof(ConsignmentItemMovement),
            typeof(ConsignmentItem),
            typeof(ProductIncomeItem),
            typeof(ProductIncome),
            typeof(Storage)
        };

        Func<object[], ConsignmentItemMovement> orderItemInfoMapper = objects => {
            ConsignmentItemMovement consignmentItemMovement = (ConsignmentItemMovement)objects[0];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[1];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[2];
            ProductIncome productIncome = (ProductIncome)objects[3];
            Storage storage = (Storage)objects[4];

            if (consignmentItemMovement == null ||
                !saleToReturn.Order.OrderItems
                    .Any(x => x.Id.Equals(consignmentItemMovement.OrderItemId)))
                return consignmentItemMovement;

            productIncome.Storage = storage;
            productIncomeItem.ProductIncome = productIncome;
            consignmentItem.ProductIncomeItem = productIncomeItem;
            consignmentItemMovement.ConsignmentItem = consignmentItem;

            saleToReturn.Order.OrderItems
                .First(x => x.Id.Equals(consignmentItemMovement.OrderItemId)).ConsignmentItemMovements
                .Add(consignmentItemMovement);

            return consignmentItemMovement;
        };

        _connection.Query(
            "SELECT * FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ConsignmentItemMovement].[ConsignmentItemID] " +
            "AND [ConsignmentItem].[Deleted] = 0 " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID] " +
            "AND [ProductIncomeItem].[Deleted] = 0 " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductIncome].[StorageID] " +
            "WHERE [ConsignmentItemMovement].[OrderItemID] IN @Ids; ",
            orderItemInfoTypes, orderItemInfoMapper,
            new {
                Ids = saleToReturn.Order.OrderItems.Select(x => x.Id)
            });

        Type[] typesHistoryInvoiceEdit = {
            typeof(HistoryInvoiceEdit),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], HistoryInvoiceEdit> mapperHistoryInvoiceEdit = objects => {
            HistoryInvoiceEdit historyInvoice = (HistoryInvoiceEdit)objects[0];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[1];


            if (!saleToReturn.HistoryInvoiceEdit.Any(p => p.Id.Equals(historyInvoice.Id))) {
                historyInvoice.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
                saleToReturn.HistoryInvoiceEdit.Add(historyInvoice);
            } else {
                HistoryInvoiceEdit historyInvoiceEditFromList = saleToReturn.HistoryInvoiceEdit.First(s => s.Id.Equals(historyInvoice.Id));

                historyInvoiceEditFromList.OrderItemBaseShiftStatuses.Add(orderItemBaseShiftStatus);
            }

            return historyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [HistoryInvoiceEdit] " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].HistoryInvoiceEditId = HistoryInvoiceEdit.ID " +
            "WHERE HistoryInvoiceEdit.SaleID = @Id " +
            "AND HistoryInvoiceEdit.Deleted = 0 ",
            typesHistoryInvoiceEdit,
            mapperHistoryInvoiceEdit,
            new {
                saleToReturn.Id
            }
        );
        return saleToReturn;
    }

    public Sale GetByNetIdWithShiftedItemsWithoutAdditionalIncludes(Guid netId) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [Pricing] " +
            "ON [ProductPricing].PricingID = [Pricing].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Product].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [Sale].UserID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [Sale].BaseSalePaymentStatusID = [BaseSalePaymentStatus].ID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [User] AS [OrderItemUser] " +
            "ON [OrderItemUser].ID = [OrderItem].UserID " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "AND [SaleBaseShiftStatus].Deleted = 0 " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].OrderItemID = [OrderItem].ID " +
            "AND [OrderItemBaseShiftStatus].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [Sale].NetUID = @NetId";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(User),
            typeof(ClientAgreement),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(Agreement)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ProductPricing productPricing = (ProductPricing)objects[4];
            Pricing pricing = (Pricing)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            User user = (User)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[9];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[10];
            SaleNumber saleNumber = (SaleNumber)objects[11];
            User orderItemUser = (User)objects[12];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[13];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[14];
            Agreement agreement = (Agreement)objects[15];

            if (saleToReturn == null) {
                sale.ShiftStatus = saleBaseShiftStatus;

                if (saleNumber != null) sale.SaleNumber = saleNumber;

                if (clientAgreement != null) {
                    clientAgreement.Agreement = agreement;

                    sale.ClientAgreement = clientAgreement;
                }

                if (user != null) {
                    sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                    sale.User = user;
                }

                if (order != null) sale.Order = order;

                if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                saleToReturn = sale;
            }

            if (orderItem == null) return sale;

            if (productPricing != null) {
                if (pricing != null) productPricing.Pricing = pricing;

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.TotalAmount = productPricing.Price * Convert.ToDecimal(orderItem.Qty);

                product.ProductPricings.Add(productPricing);
            }

            if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

            orderItem.Product = product;
            orderItem.User = orderItemUser;

            if (saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                OrderItem orderItemFormArray = saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (productPricing != null && !orderItemFormArray.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemFormArray.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemFormArray.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemFormArray.Product.ProductProductGroups.Add(productProductGroup);

                if (orderItemBaseShiftStatus != null && !orderItemFormArray.ShiftStatuses.Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id)))
                    orderItemFormArray.ShiftStatuses.Add(orderItemBaseShiftStatus);
            } else {
                orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                saleToReturn.Order.OrderItems.Add(orderItem);
            }

            return sale;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return saleToReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Sale SET Deleted = 1 WHERE NetUid = @NetId",
            new {
                NetId = netId
            }
        );
    }

    public void Update(Sale sale) {
        _connection.Execute(
            "UPDATE Sale " +
            "SET ClientAgreementId = @ClientAgreementId, IsPrintedActProtocolEdit = @IsPrintedActProtocolEdit, WarehousesShipmentId = @WarehousesShipmentId, OrderId = @OrderId, UserId = @UserId, BaseLifeCycleStatusId = @BaseLifeCycleStatusId, " +
            "BaseSalePaymentStatusId = @BaseSalePaymentStatusId, Comment = @Comment, DeliveryRecipientId = @DeliveryRecipientId, " +
            "DeliveryRecipientAddressId = @DeliveryRecipientAddressId, TransporterId = @TransporterId, SaleNumberId = @SaleNumberId, ShiftStatusId = @ShiftStatusId, " +
            "IsMerged = @IsMerged, IsDevelopment = @IsDevelopment, SaleInvoiceDocumentId = @SaleInvoiceDocumentId, ShipmentDate = @ShipmentDate, IsPrinted = @IsPrinted, TTN = @TTN," +
            "ShippingAmount = @ShippingAmount, CashOnDeliveryAmount = @CashOnDeliveryAmount, Updated = getutcdate(), [IsPrintedPaymentInvoice] = @IsPrintedPaymentInvoice, " +
            "RetailClientId = @RetailClientId, CustomersOwnTtnID = @CustomersOwnTtnId, IsFullPayment = @IsFullPayment, " +
            "MisplacedSaleId = @MisplacedSaleId, WorkplaceID = @WorkplaceId, IsCashOnDelivery = @IsCashOnDelivery, HasDocuments = @HasDocuments " +
            "WHERE NetUid = @NetUid",
            sale
        );
    }

    public void UpdateUser(Sale sale) {
        _connection.Execute(
            "UPDATE Sale " +
            "SET UpdateUserId = @UpdateUserId " +
            "WHERE NetUid = @NetUid",
            sale
        );
    }

    public void UpdateShipmentInfo(Sale sale) {
        _connection.Execute(
            "UPDATE Sale " +
            "SET TTN = @TTN, ShippingAmount = @ShippingAmount, Updated = GETUTCDATE() " +
            "WHERE NetUid = @NetUid",
            sale
        );
    }

    public void UpdateTaxFreePackListReference(Sale sale) {
        _connection.Execute(
            "UPDATE Sale " +
            "SET TaxFreePackListId = @TaxFreePackListId, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetUid",
            sale
        );
    }

    public void UpdateSadReference(Sale sale) {
        _connection.Execute(
            "UPDATE Sale " +
            "SET SadId = @SadId, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetUid",
            sale
        );
    }

    public void UpdateClientAgreementByIds(long saleId, long clientAgreementId) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET ClientAgreementId = @ClientAgreementId, Updated = GETUTCDATE() " +
            "WHERE ID = @SaleId",
            new {
                SaleId = saleId,
                ClientAgreementId = clientAgreementId
            }
        );
    }

    public void UpdateSaleCommentByNetId(Guid netId, string comment) {
        _connection.Execute(
            "UPDATE [Sale] SET [Comment] = @Comment, [Updated] = GETUTCDATE() " +
            "WHERE [Sale].NetUID = @NetId ",
            new { NetId = netId, Comment = comment });
    }

    public void UpdateWarehousesShipmentCommentByNetId(Guid netId, string comment) {
        _connection.Execute(
            "UPDATE [WarehousesShipment] SET [Comment] = @Comment, [Updated] = GETUTCDATE() " +
            "WHERE [WarehousesShipment].NetUID = @NetId ",
            new { NetId = netId, Comment = comment });
    }


    public void SetNewUpdatedDate(Sale sale) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            sale
        );
    }

    public void SetNewUpdatedDate(Guid netId) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET IsDevelopment = 1 " +
            "WHERE NetUid = @NetId",
            new {
                NetId = netId
            }
        );
    }

    public void Update(List<Sale> sales) {
        _connection.Execute(
            "UPDATE Sale " +
            "SET ClientAgreementId = @ClientAgreementId, OrderId = @OrderId, UserId = @UserId, BaseLifeCycleStatusId = @BaseLifeCycleStatusId, " +
            "BaseSalePaymentStatusId = @BaseSalePaymentStatusId, Comment = @Comment, DeliveryRecipientId = @DeliveryRecipientId, " +
            "DeliveryRecipientAddressId = @DeliveryRecipientAddressId, TransporterId = @TransporterId, SaleNumberId = @SaleNumberId, ShiftStatusId = @ShiftStatusId, " +
            "SaleInvoiceDocumentId = @SaleInvoiceDocumentId, IsMerged = @IsMerged, Updated = getutcdate(), [IsPrintedPaymentInvoice] = @IsPrintedPaymentInvoice " +
            "WHERE NetUid = @NetUid",
            sales
        );
    }

    public void UpdateDiscountComment(Sale sale) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET OneTimeDiscountComment = @OneTimeDiscountComment " +
            "WHERE ID = @Id",
            sale
        );
    }

    public void UpdateLockInfo(Sale sale) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET IsLocked = @IsLocked, IsPaymentBillDownloaded = @IsPaymentBillDownloaded, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            sale
        );
    }

    public void SetBillDownloadDateByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET BillDownloadDate = GETUTCDATE() " +
            "WHERE [Sale].NetUID = @NetID ",
            new { NetID = netId }
        );
    }

    public void UnlockSaleById(long id) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET IsLocked = 0 " +
            "WHERE ID = @Id",
            new {
                Id = id
            }
        );
    }

    public void SetChangedToInvoiceDateByNetId(Guid netId, long? updatedById) {
        if (updatedById.HasValue)
            _connection.Execute(
                "UPDATE [Sale] " +
                "SET ChangedToInvoice = GETUTCDATE(), ChangedToInvoiceByID = @UpdatedById " +
                "WHERE [Sale].NetUID = @NetId",
                new { NetId = netId, UpdatedById = updatedById.Value }
            );
        else
            _connection.Execute(
                "UPDATE [Sale] " +
                "SET ChangedToInvoice = GETUTCDATE() " +
                "WHERE [Sale].NetUID = @NetId",
                new { NetId = netId }
            );
    }

    public List<Sale> GetAll(string orderBy, long offset, long limit) {
        List<Sale> sales = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Sale].ID DESC) AS RowNumber " +
            ", [Sale].ID " +
            "FROM [Sale] " +
            "WHERE [Sale].Deleted = 0 " +
            ") " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY RowNumber ";

        IEnumerable<long> saleIds = _connection.Query<long>(
            sqlExpression,
            new { Limit = limit, Offset = offset }
        );

        string sqlFullExpression =
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON Sale.OrderId = [Order].ID " +
            "LEFT OUTER JOIN OrderItem " +
            "ON [Order].Id = OrderItem.OrderId " +
            "LEFT OUTER JOIN Product " +
            "ON OrderItem.ProductId = Product.Id " +
            "LEFT OUTER JOIN [User] " +
            "ON Sale.UserId = [User].Id " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON Sale.ClientAgreementId = ClientAgreement.Id " +
            "LEFT OUTER JOIN Agreement " +
            "ON ClientAgreement.AgreementId = Agreement.Id " +
            "LEFT OUTER JOIN Currency " +
            "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN Client " +
            "ON ClientAgreement.ClientId = Client.Id AND ClientAgreement.Deleted = 0 " +
            "LEFT OUTER JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusId = BaseLifeCycleStatus.Id " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT OUTER JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.Id = Sale.BaseSalePaymentStatusId AND BaseSalePaymentStatus.Deleted = 0 " +
            "WHERE Sale.Id IN @SaleIds " +
            orderBy;

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Client client = (Client)objects[8];
            RegionCode regionCode = (RegionCode)objects[9];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];


            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currency != null) agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                sale.ClientAgreement = clientAgreement;
            }

            if (user != null) {
                sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                sale.User = user;
            }

            if (orderItem != null && product != null) {
                orderItem.Product = product;
                order.OrderItems.Add(orderItem);
            }

            if (order != null) sale.Order = order;

            if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

            if (sales.Any(c => c.Id.Equals(sale.Id))) {
                Sale saleFromDb = sales.First(c => c.Id.Equals(sale.Id));

                if (orderItem != null && !saleFromDb.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) saleFromDb.Order.OrderItems.Add(orderItem);
            } else {
                sales.Add(sale);
            }

            return sale;
        };

        var props = new { SaleIds = saleIds };

        _connection.Query(sqlFullExpression, types, mapper, props);

        sales.ForEach(s => s.TotalCount = s.Order.OrderItems.Sum(i => i.Qty));
        sales.ForEach(s => s.TotalAmount = s.Order.OrderItems.Sum(i => i.Product.ProductPricings.First().Price));

        return sales;
    }

    public List<Sale> GetAll(string sql, string orderBy, GetQuery query) {
        List<Sale> sales = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Sale].ID DESC) AS RowNumber " +
            ", [Sale].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderId = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON [Order].Id = OrderItem.OrderId " +
            "LEFT JOIN Product " +
            "ON OrderItem.ProductId = Product.Id " +
            "LEFT JOIN [User] " +
            "ON Sale.UserId = [User].Id " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementId = ClientAgreement.Id " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementId = Agreement.Id " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientId = Client.Id AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusId = BaseLifeCycleStatus.Id " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.Id = Sale.BaseSalePaymentStatusId AND BaseSalePaymentStatus.Deleted = 0 " +
            sql +
            ") " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY RowNumber ";

        IEnumerable<long> saleIds = _connection.Query<long>(
            sqlExpression,
            new { query.Limit, query.Offset }
        );

        string sqlFullExpression =
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON Sale.OrderId = [Order].ID " +
            "LEFT OUTER JOIN OrderItem " +
            "ON [Order].Id = OrderItem.OrderId " +
            "LEFT OUTER JOIN Product " +
            "ON OrderItem.ProductId = Product.Id " +
            "LEFT OUTER JOIN [User] " +
            "ON Sale.UserId = [User].Id " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON Sale.ClientAgreementId = ClientAgreement.Id " +
            "LEFT OUTER JOIN Agreement " +
            "ON ClientAgreement.AgreementId = Agreement.Id " +
            "LEFT OUTER JOIN Currency " +
            "ON Agreement.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN Client " +
            "ON ClientAgreement.ClientId = Client.Id AND ClientAgreement.Deleted = 0 " +
            "LEFT OUTER JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.Id AND RegionCode.Deleted = 0 " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusId = BaseLifeCycleStatus.Id " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT OUTER JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.Id = Sale.BaseSalePaymentStatusId AND BaseSalePaymentStatus.Deleted = 0 " +
            "WHERE Sale.Id IN @SaleIds " +
            orderBy;

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Client),
            typeof(RegionCode),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            Client client = (Client)objects[8];
            RegionCode regionCode = (RegionCode)objects[9];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];

            if (clientAgreement != null) {
                if (agreement != null) {
                    if (currency != null) agreement.Currency = currency;

                    clientAgreement.Agreement = agreement;
                }

                if (client != null) {
                    if (regionCode != null) client.RegionCode = regionCode;

                    clientAgreement.Client = client;
                }

                sale.ClientAgreement = clientAgreement;
            }

            if (user != null) {
                sale.UserFullName = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                sale.User = user;
            }

            if (orderItem != null) {
                orderItem.Product = product;
                order.OrderItems.Add(orderItem);
            }

            if (order != null) sale.Order = order;

            if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            if (baseSalePaymentStatus != null) sale.BaseSalePaymentStatus = baseSalePaymentStatus;

            if (sales.Any(c => c.Id.Equals(sale.Id))) {
                Sale saleFromDb = sales.First(c => c.Id.Equals(sale.Id));

                if (orderItem != null && !saleFromDb.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) saleFromDb.Order.OrderItems.Add(orderItem);
            } else {
                sales.Add(sale);
            }

            return sale;
        };

        var props = new { SaleIds = saleIds };

        _connection.Query(sqlFullExpression, types, mapper, props);

        sales.ForEach(s => s.TotalCount = s.Order.OrderItems.Sum(i => i.Qty));
        sales.ForEach(s => s.TotalAmount = s.Order.OrderItems.Sum(i => i.Product.ProductPricings.First().Price));

        return sales;
    }

    public long GetAllTotalAmount(SaleLifeCycleType? saleLifeCycleType, DateTime from, DateTime to) {
        string sqlExpression =
            "SELECT COUNT(*) FROM Sale " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON Sale.BaseLifeCycleStatusId = BaseLifeCycleStatus.Id " +
            "AND BaseLifeCycleStatus.Deleted = 0 " +
            "WHERE Sale.Updated >= @From " +
            "AND Sale.Updated <= @To " +
            "AND Sale.Deleted = 0 ";

        if (saleLifeCycleType != null && Enum.IsDefined(typeof(SaleLifeCycleType), saleLifeCycleType))
            sqlExpression += "AND BaseLifeCycleStatus.SaleLifeCycleType = @SaleLifeCycleType ";

        return _connection.Query<long>(
            sqlExpression,
            new {
                SaleLifeCycleType = saleLifeCycleType,
                From = from,
                To = to
            }
        ).SingleOrDefault();
    }

    public List<dynamic> GetTotalForSalesByYear(Guid clientNetId) {
        return _connection.Query<dynamic>(
            "WITH TotalSalesByMonth_CTE " +
            "AS " +
            "( " +
            "SELECT COUNT(*) AS TotalCount, MONTH(Sale.Created) AS [Month] " +
            "FROM Sale " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "WHERE Sale.Deleted = 0 " +
            "AND YEAR(Sale.Created) = YEAR(GETUTCDATE()) " +
            "AND Client.NetUID = @ClientNetId " +
            "GROUP BY MONTH(Sale.Created) " +
            ") " +
            "SELECT " +
            "ChartMonthTranslation.Name, " +
            "CASE " +
            "WHEN TotalSalesByMonth_CTE.TotalCount IS NULL " +
            "THEN 0 " +
            "WHEN TotalSalesByMonth_CTE.TotalCount IS NOT NULL " +
            "THEN TotalSalesByMonth_CTE.TotalCount " +
            "END AS TotalCount " +
            "FROM ChartMonth " +
            "LEFT JOIN ChartMonthTranslation " +
            "ON ChartMonth.ID = ChartMonthTranslation.ChartMonthID " +
            "AND ChartMonthTranslation.CultureCode = @Culture " +
            "LEFT JOIN TotalSalesByMonth_CTE " +
            "ON TotalSalesByMonth_CTE.Month = ChartMonth.Number ",
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                ClientNetId = clientNetId.ToString()
            }
        ).ToList();
    }

    public List<dynamic> GetSaleLifeCycleLine(Guid saleNetId) {
        return _connection.Query<dynamic>(
            "SELECT AuditEntityProperty.Name, Value, AuditEntityProperty.Updated " +
            "FROM AuditEntity " +
            "LEFT OUTER JOIN AuditEntityProperty " +
            "ON AuditEntityProperty.AuditEntityID = AuditEntity.ID " +
            "AND AuditEntityProperty.Name = 'SaleLifeCycleType' " +
            "AND AuditEntityProperty.[Type] = 1 " +
            "WHERE AuditEntity.BaseEntityNetUID = @SaleNetId " +
            "ORDER BY AuditEntityProperty.Updated",
            new {
                SaleNetId = saleNetId
            }
        ).ToList();
    }

    public List<Sale> FindClientSalesBySaleNumber(Guid clientNetId, string searchValue) {
        List<Sale> sales = new();

        string sql =
            "SELECT Sale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",[Order].* " +
            ",OrderItem.* " +
            ",OrderItemUser.* " +
            ",Product.* " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            ",SaleBaseShiftStatus.* " +
            ",OrderItemBaseShiftStatus.* " +
            "FROM Sale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = Sale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "LEFT JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserID " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "WHERE Client.NetUID = @ClientNetId " +
            "AND SaleNumber.Value LIKE '%' + @Value + '%' " +
            "AND Sale.Deleted = 0 ";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Order order = (Order)objects[13];
            OrderItem orderItem = (OrderItem)objects[14];
            User orderItemUser = (User)objects[15];
            Product product = (Product)objects[16];
            ProductPricing productPricing = (ProductPricing)objects[17];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[18];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[19];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[20];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[21];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);
                }

                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new {
            ClientNetId = clientNetId,
            Value = searchValue,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(sql, types, mapper, props);

        return sales;
    }

    public Sale GetLastNewSaleByClientAgreementNetId(Guid clientAgreementNetId) {
        long? saleId = _connection.Query<long?>(
            "SELECT Sale.ID FROM Sale " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "WHERE ClientAgreement.NetUID = @NetId " +
            "AND BaseLifeCycleStatus.SaleLifeCycleType = @NewStatus " +
            "AND Sale.Deleted = 0 " +
            "AND Sale.IsMerged = 0 " +
            "AND Sale.Updated >= @Today " +
            "ORDER BY Sale.Created DESC",
            new {
                NetId = clientAgreementNetId,
                NewStatus = SaleLifeCycleType.New,
                Today = DateTime.ParseExact(DateTime.Now.ToString("yyyyMMdd"), "yyyyMMdd", CultureInfo.InvariantCulture)
            }
        ).FirstOrDefault();

        if (!saleId.HasValue) return null;

        Sale saleToReturn = null;

        _connection.Query<Sale, Order, ClientAgreement, Agreement, BaseLifeCycleStatus, OrderItem, Sale>(
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "WHERE Sale.ID = @Id ",
            (sale, order, clientAgreement, agreement, baseLifeCycleStatus, orderItem) => {
                if (saleToReturn == null) {
                    if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                    if (clientAgreement != null) {
                        clientAgreement.Agreement = agreement;
                        sale.ClientAgreement = clientAgreement;
                    }

                    if (order != null) {
                        if (orderItem != null) order.OrderItems.Add(orderItem);

                        sale.Order = order;
                    }

                    saleToReturn = sale;
                }

                if (orderItem != null && !saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) saleToReturn.Order.OrderItems.Add(orderItem);

                return sale;
            },
            new {
                Id = saleId.Value
            }
        );

        return saleToReturn;
    }

    public Sale GetLastNotMergedNewSaleByClientAgreementNetId(Guid clientAgreementNetId) {
        long? saleId =
            _connection.Query<long?>(
                "SELECT Sale.ID FROM Sale " +
                "LEFT OUTER JOIN ClientAgreement " +
                "ON ClientAgreement.ID = Sale.ClientAgreementID " +
                "LEFT OUTER JOIN BaseLifeCycleStatus " +
                "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
                "LEFT JOIN SaleMerged " +
                "ON SaleMerged.ID = (" +
                "SELECT TOP(1) SaleMerged.ID " +
                "FROM SaleMerged " +
                "WHERE SaleMerged.OutputSaleID = Sale.ID " +
                ") " +
                "WHERE ClientAgreement.NetUID = @NetId " +
                "AND BaseLifeCycleStatus.SaleLifeCycleType = @NewStatus " +
                "AND Sale.Deleted = 0 " +
                "AND Sale.Updated >= @Today " +
                "AND Sale.IsMerged = 0 " +
                "AND SaleMerged.ID IS NULL " +
                "ORDER BY Sale.Created DESC",
                new {
                    NetId = clientAgreementNetId,
                    NewStatus = SaleLifeCycleType.New,
                    Today = DateTime.ParseExact(DateTime.Now.ToString("yyyyMMdd"), "yyyyMMdd", CultureInfo.InvariantCulture)
                }
            ).FirstOrDefault();

        if (!saleId.HasValue) return null;

        Sale saleToReturn = null;

        _connection.Query<Sale, Order, ClientAgreement, Agreement, BaseLifeCycleStatus, OrderItem, Sale>(
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "WHERE Sale.ID = @Id ",
            (sale, order, clientAgreement, agreement, baseLifeCycleStatus, orderItem) => {
                if (saleToReturn == null) {
                    if (baseLifeCycleStatus != null) sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                    if (clientAgreement != null) {
                        clientAgreement.Agreement = agreement;
                        sale.ClientAgreement = clientAgreement;
                    }

                    if (order != null) {
                        if (orderItem != null) order.OrderItems.Add(orderItem);

                        sale.Order = order;
                    }

                    saleToReturn = sale;
                }

                if (orderItem != null && !saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) saleToReturn.Order.OrderItems.Add(orderItem);

                return sale;
            },
            new {
                Id = saleId.Value
            }
        );

        return saleToReturn;
    }

    public Sale GetByNetIdWithSaleMergedAndOrderItemsMerged(Guid netId) {
        Sale saleToReturn = null;

        string sqlExpression =
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.OutputSaleID = Sale.ID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Organization " +
            "ON Organization.ID = Agreement.OrganizationID " +
            "LEFT JOIN OrderItemMerged " +
            "ON OrderItemMerged.OrderItemID = OrderItem.ID " +
            "WHERE Sale.NetUID = @NetId ";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(SaleMerged),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(OrderItemMerged)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            SaleMerged saleMerged = (SaleMerged)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Organization organization = (Organization)objects[6];
            OrderItemMerged orderItemMerged = (OrderItemMerged)objects[7];

            if (saleToReturn == null) {
                if (orderItem != null) {
                    if (orderItemMerged != null) orderItem.OrderItemMerges.Add(orderItemMerged);

                    order.OrderItems.Add(orderItem);
                }

                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                if (saleMerged != null)
                    sale.InputSaleMerges.Add(saleMerged);

                sale.ClientAgreement = clientAgreement;
                sale.Order = order;

                saleToReturn = sale;
            }

            if (saleMerged != null && !saleToReturn.InputSaleMerges.Any(s => s.Id.Equals(saleMerged.Id))) saleToReturn.InputSaleMerges.Add(saleMerged);

            if (orderItem == null) return sale;

            if (!saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) {
                if (orderItemMerged != null) orderItem.OrderItemMerges.Add(orderItemMerged);

                saleToReturn.Order.OrderItems.Add(orderItem);
            } else {
                if (orderItemMerged != null &&
                    !saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id)).OrderItemMerges.Any(m => m.Id.Equals(orderItemMerged.Id)))
                    saleToReturn.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id)).OrderItemMerges.Add(orderItemMerged);
            }

            return sale;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return saleToReturn;
    }

    public Sale GetByNetIdWithSaleMerged(Guid netId) {
        Sale saleToReturn = null;

        _connection.Query<Sale, Order, OrderItem, SaleMerged, ClientAgreement, Agreement, Organization, Sale>(
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "LEFT JOIN SaleMerged " +
            "ON SaleMerged.InputSaleID = Sale.ID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Organization " +
            "ON Organization.ID = Agreement.OrganizationID " +
            "WHERE Sale.NetUID = @NetId ",
            (sale, order, orderItem, saleMerged, clientAgreement, agreement, organization) => {
                if (saleToReturn == null) {
                    if (orderItem != null) order.OrderItems.Add(orderItem);

                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    sale.OutputSaleMerges.Add(saleMerged);
                    sale.ClientAgreement = clientAgreement;
                    sale.Order = order;

                    saleToReturn = sale;
                }

                if (orderItem != null && !saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) saleToReturn.Order.OrderItems.Add(orderItem);

                return sale;
            },
            new {
                NetId = netId
            }
        );

        return saleToReturn;
    }

    public Sale GetSaleBySaleNumber(string value) {
        return _connection.Query<Sale>(
            "SELECT * FROM [Sale] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "WHERE [SaleNumber].Value = @Value ",
            new { Value = value }).FirstOrDefault();
    }


    public Sale GetByNetIdWithAgreement(Guid netId) {
        Sale saleToReturn = null;

        _connection.Query<Sale, Order, ClientAgreement, Agreement, Organization, BaseLifeCycleStatus, OrderItem, Sale>(
            "SELECT * FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Organization " +
            "ON Organization.ID = Agreement.OrganizationID " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "AND OrderItem.Deleted = 0 " +
            "WHERE Sale.NetUID = @NetId ",
            (sale, order, clientAgreement, agreement, organization, baseLifeCycleStatus, orderItem) => {
                if (saleToReturn == null) {
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    if (orderItem != null) order.OrderItems.Add(orderItem);

                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.ClientAgreement = clientAgreement;
                    sale.Order = order;

                    saleToReturn = sale;
                }

                if (orderItem != null && !saleToReturn.Order.OrderItems.Any(o => o.Id.Equals(orderItem.Id))) saleToReturn.Order.OrderItems.Add(orderItem);

                return sale;
            },
            new {
                NetId = netId
            }
        );

        return saleToReturn;
    }

    public Sale GetByOrderId(long orderId) {
        return _connection.Query<Sale, Order, BaseLifeCycleStatus, BaseSalePaymentStatus, ClientAgreement, Agreement, Organization, Sale>(
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID AND [Order].Deleted = 0 " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT OUTER JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [Order].ID = @OrderId",
            (sale, order, lifeCycleStatus, salePaymentStatus, clientAgreement, agreement, organization) => {
                if (lifeCycleStatus != null) sale.BaseLifeCycleStatus = lifeCycleStatus;

                if (salePaymentStatus != null) sale.BaseSalePaymentStatus = salePaymentStatus;

                agreement.Organization = organization;
                clientAgreement.Agreement = agreement;
                sale.ClientAgreement = clientAgreement;

                return sale;
            },
            new {
                OrderId = orderId
            }
        ).SingleOrDefault();
    }

    public List<Sale> GetAllSubClientsSalesByClientNetId(Guid clientNetId) {
        List<Sale> sales = new();

        string sqlExpression =
            "WITH ClientAndSubClientsNewSales_CTE (SaleID) " +
            "AS ( " +
            "SELECT Sale.ID FROM Sale " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID AND ClientAgreement.Deleted = 0 " +
            "WHERE BaseLifeCycleStatus.SaleLifeCycleType = @SaleLifeCycleType " +
            "AND ClientAgreement.ClientID IN ( " +
            "SELECT ID FROM Client WHERE Client.NetUID = @ClientNetID " +
            "UNION ALL " +
            "SELECT ClientSubClient.SubClientID AS ID FROM Client " +
            "LEFT OUTER JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "WHERE Client.NetUID = @ClientNetID " +
            ")) " +
            "SELECT * FROM Sale " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID AND BaseLifeCycleStatus.Deleted = 0 " +
            "LEFT OUTER JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = Sale.BaseSalePaymentStatusID AND BaseSalePaymentStatus.Deleted = 0 " +
            "LEFT OUTER JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID AND SaleNumber.Deleted = 0 " +
            "LEFT OUTER JOIN [User] " +
            "ON [User].ID = Sale.UserID AND [User].Deleted = 0 " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID AND ClientAgreement.Deleted = 0 " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID AND Agreement.Deleted = 0 " +
            "LEFT OUTER JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID AND Pricing.Deleted = 0 " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID AND Currency.Deleted = 0 " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT OUTER JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID AND OrderItem.Deleted = 0 " +
            "LEFT OUTER JOIN [User] AS OrderItemUser " +
            "ON OrderItemUser.ID = OrderItem.UserId " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT JOIN SaleBaseShiftStatus " +
            "ON Sale.ShiftStatusID = SaleBaseShiftStatus.ID " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItemBaseShiftStatus.OrderItemID = OrderItem.ID " +
            "LEFT JOIN Transporter " +
            "ON Sale.TransporterID = Transporter.ID " +
            "LEFT JOIN TransporterType " +
            "ON Transporter.TransporterTypeID = TransporterType.ID " +
            "LEFT JOIN TransporterTypeTranslation " +
            "ON TransporterTypeTranslation.TransporterTypeID = TransporterType.ID " +
            "AND TransporterTypeTranslation.Deleted = 0 " +
            "AND TransporterTypeTranslation.CultureCode = @Culture " +
            "LEFT JOIN DeliveryRecipient " +
            "ON DeliveryRecipient.ID = Sale.DeliveryRecipientID " +
            "LEFT JOIN DeliveryRecipientAddress " +
            "ON DeliveryRecipientAddress.ID = Sale.DeliveryRecipientAddressID " +
            "WHERE Sale.ID IN (SELECT SaleID FROM ClientAndSubClientsNewSales_CTE)";

        Type[] types = {
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(Order),
            typeof(OrderItem),
            typeof(User),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount),
            typeof(SaleBaseShiftStatus),
            typeof(OrderItemBaseShiftStatus),
            typeof(Transporter),
            typeof(TransporterType),
            typeof(TransporterTypeTranslation),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            Client client = (Client)objects[11];
            RegionCode regionCode = (RegionCode)objects[12];
            Order order = (Order)objects[13];
            OrderItem orderItem = (OrderItem)objects[14];
            User orderItemUser = (User)objects[15];
            Product product = (Product)objects[16];
            ProductPricing productPricing = (ProductPricing)objects[17];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[18];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[19];
            SaleBaseShiftStatus saleBaseShiftStatus = (SaleBaseShiftStatus)objects[20];
            OrderItemBaseShiftStatus orderItemBaseShiftStatus = (OrderItemBaseShiftStatus)objects[21];
            Transporter transporter = (Transporter)objects[22];
            TransporterType transporterType = (TransporterType)objects[23];
            TransporterTypeTranslation transporterTypeTranslation = (TransporterTypeTranslation)objects[24];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[25];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[26];

            if (sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sales.First(c => c.Id.Equals(sale.Id));

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (orderItem == null) return sale;

                if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    saleFromList.Order.OrderItems.Add(orderItem);
                } else if (orderItemBaseShiftStatus != null && !saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses
                               .Any(s => s.Id.Equals(orderItemBaseShiftStatus.Id))) {
                    saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ShiftStatuses.Add(orderItemBaseShiftStatus);
                }
            } else {
                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (deliveryRecipient != null) sale.DeliveryRecipient = deliveryRecipient;

                if (deliveryRecipientAddress != null) sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                agreement.Pricing = pricing;

                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                if (orderItem != null) {
                    if (orderItemBaseShiftStatus != null) orderItem.ShiftStatuses.Add(orderItemBaseShiftStatus);

                    orderItem.User = orderItemUser;
                    orderItem.Product = product;

                    order.OrderItems.Add(orderItem);
                }

                if (transporterType != null) {
                    transporterType.Name = transporterTypeTranslation.Name;

                    transporter.TransporterType = transporterType;
                }

                sale.Transporter = transporter;
                sale.Order = order;
                sale.ShiftStatus = saleBaseShiftStatus;
                sale.ClientAgreement = clientAgreement;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                sale.SaleNumber = saleNumber;
                sale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                sale.User = saleUser;

                sales.Add(sale);
            }

            return sale;
        };

        var props = new {
            ClientNetId = clientNetId,
            SaleLifeCycleType = SaleLifeCycleType.New,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(sqlExpression, types, mapper, props);

        return sales;
    }

    public SaleStatisticsByManager GetSaleStatisticsByManagerRanged(long managerId, DateTime from, DateTime to) {
        return _connection.Query<SaleStatisticsByManager>(
            "SELECT " +
            "( " +
            "SELECT COUNT(1) " +
            "FROM [Sale] " +
            "WHERE [Sale].ChangedToInvoice >= @From " +
            "AND [Sale].ChangedToInvoice <= @To " +
            "AND [Sale].ChangedToInvoiceByID = @UserId " +
            ") AS [PackagingSalesCount] " +
            ",( " +
            "SELECT COUNT(1) " +
            "FROM [Sale] " +
            "WHERE [Sale].Created >= @From " +
            "AND [Sale].Created <= @To " +
            "AND [Sale].UserID = @UserId " +
            ") AS [NewSalesCount] " +
            ",( " +
            "SELECT DISTINCT COUNT([OrderItemMovement].OrderItemID) " +
            "FROM [OrderItemMovement] " +
            "WHERE [OrderItemMovement].Deleted = 0 " +
            "AND [OrderItemMovement].Created >= @From " +
            "AND [OrderItemMovement].Created <= @To " +
            "AND [OrderItemMovement].UserID = @UserId " +
            ") AS [OrderItemsCount] " +
            ",( " +
            "SELECT SUM([OrderItemMovement].Qty) " +
            "FROM [OrderItemMovement] " +
            "WHERE [OrderItemMovement].Deleted = 0 " +
            "AND [OrderItemMovement].Created >= @From " +
            "AND [OrderItemMovement].Created <= @To " +
            "AND [OrderItemMovement].UserID = @UserId " +
            ") AS [OrderItemsTotalQty] " +
            ",( " +
            "SELECT " +
            "ROUND( " +
            "SUM( " +
            "ISNULL( " +
            "CASE " +
            "WHEN [OrderItem].PricePerItem <> 0 " +
            "THEN ( " +
            "[OrderItem].PricePerItem " +
            "* " +
            "[OrderItemMovement].Qty " +
            ") " +
            "ELSE ( " +
            "ROUND( " +
            "(CASE " +
            "WHEN [OrderItem].IsFromReSale = 1 " +
            "THEN dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, [ClientAgreement].NetUID, @Culture, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].[ID]) " +
            "END) " +
            "* " +
            "[OrderItemMovement].Qty " +
            ", 2) " +
            ") " +
            "END " +
            ", 0.00) " +
            ") " +
            ", 2) " +
            "FROM [OrderItemMovement] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderItemMovement].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Order].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "WHERE [OrderItemMovement].Deleted = 0 " +
            "AND [Sale].Updated >= @From " +
            "AND [Sale].Updated <= @To " +
            "AND [OrderItemMovement].UserID = @UserId " +
            ") AS [OrderItemsTotalAmount]",
            new {
                UserId = managerId,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        ).SingleOrDefault();
    }

    public dynamic GetSalesStatisticByDateRangeAndUserNetId(Guid userNetId, DateTime from, DateTime to) {
        return _connection.Query<dynamic>(
            "DECLARE @UserID bigint = (SELECT ID FROM [User] WHERE NetUID = @UserNetId); " +
            "WITH TotalNewSalesByDateRangeAndUserNetId_CTE(NetUID) " +
            "AS ( " +
            "SELECT Sale.NetUID " +
            "FROM Sale " +
            "LEFT OUTER JOIN AuditEntity " +
            "ON Sale.NetUID = AuditEntity.BaseEntityNetUID " +
            "LEFT OUTER JOIN AuditEntityProperty " +
            "ON AuditEntityProperty.AuditEntityID = AuditEntity.ID " +
            "WHERE AuditEntityProperty.[Name] = 'SaleLifeCycleType' " +
            "AND AuditEntityProperty.Value = 'New' " +
            "AND Sale.Updated >= @From " +
            "AND Sale.Updated <= @To " +
            "AND Sale.Deleted = 0 " +
            "AND Sale.IsMerged = 0 " +
            "AND AuditEntity.UpdatedByNetUID = @UserNetId " +
            "GROUP BY Sale.NetUID " +
            "), TotalPackagingSalesByDateRangeAndUserNetId_CTE(NetUID) " +
            "AS ( " +
            "SELECT Sale.NetUID " +
            "FROM Sale " +
            "LEFT OUTER JOIN AuditEntity " +
            "ON Sale.NetUID = AuditEntity.BaseEntityNetUID " +
            "LEFT OUTER JOIN AuditEntityProperty " +
            "ON AuditEntityProperty.AuditEntityID = AuditEntity.ID " +
            "WHERE AuditEntityProperty.[Name] = 'SaleLifeCycleType' " +
            "AND AuditEntityProperty.Value = 'Packaging' " +
            "AND Sale.Updated >= @From " +
            "AND Sale.Updated <= @To " +
            "AND Sale.Deleted = 0 " +
            "AND Sale.IsMerged = 0 " +
            "AND AuditEntity.UpdatedByNetUID = @UserNetId " +
            "GROUP BY Sale.NetUID " +
            "), TotalOrderItemsByDateRangeAndUserNetId_CTE([Count], TotalQty) " +
            "AS ( " +
            "SELECT COUNT(*) AS [Count], SUM(Qty) FROM OrderItem " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = OrderItem.OrderID " +
            "LEFT OUTER JOIN Sale " +
            "ON Sale.OrderID = [Order].ID " +
            "WHERE OrderItem.UserId = @UserID " +
            "AND OrderItem.Created >= @From " +
            "AND OrderItem.Created <= @To " +
            "AND OrderItem.Qty != 0 " +
            "AND OrderItem.Deleted = 0 " +
            "AND Sale.Deleted = 0 " +
            "), TotalAmountOrderItemsByDateRangeAndUserNetId_CTE(TotalAmount) " +
            "AS ( " +
            "SELECT " +
            "ROUND( " +
            "SUM( " +
            "((ProductPricing.Price + " +
            "(ProductPricing.Price * ISNULL(dbo.GetPricingExtraCharge(Pricing.NetUID), 0)/100)) - " +
            "((ProductPricing.Price + (ProductPricing.Price * ISNULL(dbo.GetPricingExtraCharge(Pricing.NetUID), 0)/100)) * ISNULL(ProductGroupDiscount.DiscountRate, 0))/100) " +
            "* OrderItem.Qty " +
            "), 2) " +
            "AS TotalAmount " +
            "FROM OrderItem " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = OrderItem.OrderID " +
            "LEFT OUTER JOIN Sale " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT OUTER JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = Sale.BaseLifeCycleStatusID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT OUTER JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ID = ( " +
            "SELECT TOP(1) ID FROM ProductGroupDiscount " +
            "WHERE ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupId = ProductProductGroup.ProductGroupId " +
            "AND IsActive = 1 " +
            ") " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ID = (SELECT TOP(1) ID FROM ProductPricing WHERE ProductID = Product.ID) " +
            "WHERE OrderItem.UserId = @UserID " +
            "AND BaseLifeCycleStatus.SaleLifeCycleType != 0 " +
            "AND Sale.Updated >= @From " +
            "AND Sale.Updated <= @To " +
            "AND Sale.Deleted = 0 " +
            "AND Sale.IsMerged = 0 " +
            "AND OrderItem.Deleted = 0 " +
            ") " +
            "SELECT " +
            "PackagingSalesCount = (SELECT COUNT(*) FROM TotalPackagingSalesByDateRangeAndUserNetId_CTE), " +
            "NewSalesCount = (SELECT COUNT(*) FROM TotalNewSalesByDateRangeAndUserNetId_CTE), " +
            "OrderItemsCount = (SELECT [Count] FROM TotalOrderItemsByDateRangeAndUserNetId_CTE), " +
            "OrderItemsTotalQty = (SELECT ISNULL(TotalQty, 0) FROM TotalOrderItemsByDateRangeAndUserNetId_CTE), " +
            "OrderItemsTotalAmount = (SELECT ISNULL(TotalAmount, 0) FROM TotalAmountOrderItemsByDateRangeAndUserNetId_CTE)",
            new {
                UserNetId = userNetId,
                From = from,
                To = to
            }
        ).SingleOrDefault();
    }

    public TotalDashboardItem GetTotalAmountByDayAndCurrentMonth() {
        TotalDashboardItem toReturn = new();

        _connection.Query<TotalItem, TotalItem, TotalItem>(
            "DECLARE @SALE_LIST_TABLE TABLE ( " +
            "[Qty] int, " +
            "[ReturnedQty] int, " +
            "[PricePerItem] money, " +
            "[Created] datetime, " +
            "[IsVatSale] bit " +
            ") " +
            ";WITH LIST_ORDER_ITEMS_CTE AS ( " +
            "SELECT [OrderItem].[Qty], " +
            "[OrderItem].[ReturnedQty], " +
            "[OrderItem].[PricePerItem] - ([OrderItem].[DiscountAmount] / 100 * [OrderItem].[PricePerItem]) AS [PricePerItem], " +
            "[Sale].[Created], " +
            "[Sale].[IsVatSale] " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Sale] ON [Sale].[OrderID] = [Order].[ID] " +
            "WHERE [Sale].[Deleted] = 0 " +
            "AND [Order].[Deleted] = 0 " +
            "AND [OrderItem].[Deleted] = 0 " +
            ") " +
            "INSERT INTO @SALE_LIST_TABLE([Qty], [ReturnedQty],[PricePerItem], [Created], [IsVatSale]) " +
            "SELECT [Qty] " +
            ",[ReturnedQty] " +
            ",[PricePerItem] " +
            ",[Created] " +
            ",[IsVatSale] " +
            "FROM LIST_ORDER_ITEMS_CTE " +
            "SELECT ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 1 " +
            ")       AS [ValueByDay] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 1) - " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 1) " +
            ")   AS [IncreaseByDay] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 1) > " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 1) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByDay] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatSale] = 1 " +
            ")       AS [ValueByMonth] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatSale] = 1) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatSale] = 1) " +
            ")   AS [IncreaseByMonth] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatSale] = 1) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatSale] = 1) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByMonth] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 0 " +
            ")       AS [ValueByDay] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 0) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 0) " +
            ")   AS [IncreaseByDay] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 0) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatSale] = 0) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByDay] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatSale] = 0 " +
            ")       AS [ValueByMonth] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatSale] = 0) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatSale] = 0) " +
            ")   AS [IncreaseByMonth] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatSale] = 0) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(([Qty] - [ReturnedQty]) * [PricePerItem])) " +
            "END " +
            "FROM @SALE_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatSale] = 0) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByMonth] "
            ,
            (vatTotalItem, notVatTotalItem) => {
                if (vatTotalItem != null) {
                    vatTotalItem.ValueByDay = decimal.Round(vatTotalItem.ValueByDay, 2, MidpointRounding.AwayFromZero);
                    vatTotalItem.IncreaseByDay = decimal.Round(vatTotalItem.IncreaseByDay, 2, MidpointRounding.AwayFromZero);
                    vatTotalItem.ValueByMonth = decimal.Round(vatTotalItem.ValueByMonth, 2, MidpointRounding.AwayFromZero);
                    vatTotalItem.IncreaseByMonth = decimal.Round(vatTotalItem.IncreaseByMonth, 2, MidpointRounding.AwayFromZero);
                }

                if (notVatTotalItem != null) {
                    notVatTotalItem.ValueByDay = decimal.Round(notVatTotalItem.ValueByDay, 2, MidpointRounding.AwayFromZero);
                    notVatTotalItem.IncreaseByDay = decimal.Round(notVatTotalItem.IncreaseByDay, 2, MidpointRounding.AwayFromZero);
                    notVatTotalItem.ValueByMonth = decimal.Round(notVatTotalItem.ValueByMonth, 2, MidpointRounding.AwayFromZero);
                    notVatTotalItem.IncreaseByMonth = decimal.Round(notVatTotalItem.IncreaseByMonth, 2, MidpointRounding.AwayFromZero);
                }

                toReturn.VatItem = vatTotalItem;
                toReturn.NotVatItem = notVatTotalItem;

                return vatTotalItem;
            },
            new {
                QtyHour = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl") ? 3 : 2
            },
            splitOn: "ValueByDay"
        );

        return toReturn;
    }

    public void UpdateIsPrintedPaymentInvoice(long id) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET [Updated] = getutcdate() " +
            ", [IsPrintedPaymentInvoice] = 1 " +
            "WHERE [Sale].[ID] = @Id; ",
            new { Id = id }
        );
    }

    public void UpdateIsAcceptedToPacking(long id, bool isAccepted) {
        _connection.Execute(
            "UPDATE [Sale] " +
            "SET [Updated] = getutcdate() " +
            ", [IsAcceptedToPacking] = @IsAccepted " +
            "WHERE [Sale].[ID] = @Id; ",
            new { Id = id, IsAccepted = isAccepted }
        );
    }

    public long AddCustomersOwnTtn(CustomersOwnTtn customersOwnTtn) {
        return _connection.Query<long>(
            "INSERT INTO [CustomersOwnTtn] (Number, TtnPDFPath, Updated) " +
            "VALUES (@Number, @TtnPDFPath, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            customersOwnTtn
        ).FirstOrDefault();
    }

    public void UpdateCustomersOwnTtn(CustomersOwnTtn customersOwnTtn) {
        _connection.Execute(
            "UPDATE [CustomersOwnTtn] SET [Number] = @Number, [TtnPDFPath] = @TtnPDFPath, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            customersOwnTtn);
    }

    public void RemoveCustomersOwnTtn(CustomersOwnTtn customersOwnTtn) {
        _connection.Execute(
            "UPDATE [CustomersOwnTtn] SET [Deleted] = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            customersOwnTtn);
    }

    public CustomersOwnTtn GetCustomersOwnTtnById(long id) {
        return _connection.Query<CustomersOwnTtn>(
                "SELECT * FROM [CustomersOwnTtn] " +
                "WHERE ID = @Id",
                new { Id = id })
            .FirstOrDefault();
    }

    public static string BuildGetAllRanged(
    ) {
        StringBuilder builder = new();
        builder.Append(";WITH [SALE_IDS_CTE] AS ( ");
        builder.Append("SELECT ROW_NUMBER() OVER (ORDER BY [Sale].[ID] DESC) AS [RowNumber] ");
        builder.Append(", [Sale].ID ");
        builder.Append(", COUNT(*) OVER() [TotalRowsQty] ");
        builder.Append("FROM [Sale] ");
        builder.Append("LEFT JOIN [SaleNumber] ON [SaleNumber].ID = [Sale].SaleNumberID ");
        builder.Append("LEFT JOIN [BaseLifeCycleStatus] ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID ");
        builder.Append("LEFT JOIN [User] AS [SaleUser] ON [SaleUser].ID = [Sale].UserID ");
        builder.Append("LEFT JOIN [ClientAgreement] ON [ClientAgreement].ID = [Sale].ClientAgreementID ");
        builder.Append("LEFT JOIN [Client] ON [Client].ID = [ClientAgreement].ClientID ");
        builder.Append("LEFT JOIN [Agreement] ON [ClientAgreement].AgreementID = [Agreement].ID ");
        builder.Append("LEFT JOIN [Organization] ON [Organization].ID = [Agreement].OrganizationID ");
        builder.Append("LEFT JOIN [Order] ON [Sale].OrderID = [Order].ID ");
        builder.Append("LEFT JOIN [OrderItem] ON [OrderItem].OrderID = [Order].ID AND [OrderItem].Deleted = 0 AND [OrderItem].Qty > 0 ");
        builder.Append("LEFT JOIN [Product] ON [Product].ID = [OrderItem].ProductID ");
        builder.Append("WHERE ISNULL([Sale].ChangedToInvoice, [Sale].Updated) >= @From ");
        builder.Append("AND ISNULL([Sale].ChangedToInvoice, [Sale].Updated) <= @To ");
        builder.Append("AND [Sale].Deleted = 0 ");
        builder.Append("AND [Sale].IsMerged = 0 ");
        builder.Append("AND [Sale].Comment <> N'Ввід боргів з 1С' ");
        builder.Append("GROUP BY [Sale].ID) ");
        builder.Append("SELECT [Sale].ID ");
        builder.Append(", (SELECT TOP 1 [TotalRowsQty] FROM [SALE_IDS_CTE]) AS [TotalRowsQty] ");
        builder.Append("FROM [Sale] ");
        builder.Append("WHERE [Sale].[ID] IN ( ");
        builder.Append("SELECT [SALE_IDS_CTE].[ID] ");
        builder.Append("FROM [SALE_IDS_CTE] ");
        builder.Append(") ");
        builder.Append("GROUP BY [Sale].ID, [Sale].Updated, [Sale].ChangedToInvoice ");
        builder.Append("ORDER BY ISNULL(Sale.ChangedToInvoice, Sale.Updated) DESC ");

        return builder.ToString();
    }

    public static string BuildGetAllRangedSalesIdsByLifeCycleTypeQuery(
        string value,
        SaleLifeCycleType? saleLifeCycleType,
        long[] organizationIds,
        bool fromShipments,
        bool forEcommerce,
        bool fastEcommerce,
        long? clientId,
        Guid? retailClientNetId,
        Guid? userNetId) {
        IEnumerable<long> saleLifeCycleTypesExceptNew = ((SaleLifeCycleType[])Enum.GetValues(typeof(SaleLifeCycleType)))
            .Where(e => e != SaleLifeCycleType.New)
            .Select(e => (long)e);

        StringBuilder builder = new();
        builder.Append(";WITH [SALE_IDS_CTE] AS ( ");
        builder.Append("SELECT ROW_NUMBER() OVER (ORDER BY [Sale].[ID] DESC) AS [RowNumber] ");
        builder.Append(", [Sale].ID ");
        builder.Append(", COUNT(*) OVER() [TotalRowsQty] ");
        builder.Append("FROM [Sale] ");
        builder.Append("LEFT JOIN [SaleNumber] ON [SaleNumber].ID = [Sale].SaleNumberID ");
        builder.Append("LEFT JOIN [BaseLifeCycleStatus] ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID ");
        builder.Append("LEFT JOIN [User] AS [SaleUser] ON [SaleUser].ID = [Sale].UserID ");
        builder.Append("LEFT JOIN [ClientAgreement] ON [ClientAgreement].ID = [Sale].ClientAgreementID ");
        builder.Append("LEFT JOIN [Client] ON [Client].ID = [ClientAgreement].ClientID ");
        builder.Append("LEFT JOIN [Agreement] ON [ClientAgreement].AgreementID = [Agreement].ID ");
        builder.Append("LEFT JOIN [Organization] ON [Organization].ID = [Agreement].OrganizationID ");
        builder.Append("LEFT JOIN [Order] ON [Sale].OrderID = [Order].ID ");
        builder.Append("LEFT JOIN [OrderItem] ON [OrderItem].OrderID = [Order].ID AND [OrderItem].Deleted = 0 AND [OrderItem].Qty > 0 ");
        builder.Append("LEFT JOIN [Product] ON [Product].ID = [OrderItem].ProductID ");
        builder.Append("LEFT JOIN [HistoryInvoiceEdit] ON [HistoryInvoiceEdit].SaleID = [Sale].ID ");
        builder.Append("LEFT JOIN [UpdateDataCarrier] ON [UpdateDataCarrier].SaleId = [Sale].ID ");

        if (retailClientNetId.HasValue)
            builder.Append("LEFT JOIN [RetailClient] ON [RetailClient].ID = [Sale].RetailClientID ");

        if (fromShipments) {
            builder.Append("LEFT JOIN [BaseSalePaymentStatus] ");
            builder.Append("ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID ");
            builder.Append("WHERE [Sale].ChangedToInvoice >= @From ");
            builder.Append("AND [Sale].ChangedToInvoice <= @To ");
            builder.Append("AND ( ");
            builder.Append("[Sale].IsVatSale = 0 ");
            builder.Append("OR ");
            builder.Append("( ");
            builder.Append("[Sale].IsVatSale = 1 ");
            builder.Append("AND ");
            builder.Append("[BaseSalePaymentStatus].SalePaymentStatusType > 0 ");
            builder.Append("AND ");
            builder.Append("[BaseSalePaymentStatus].SalePaymentStatusType <= 3 ");
            builder.Append(") ");
            builder.Append("OR ([Sale].[IsAcceptedToPacking] = 1) ");
            builder.Append(") ");
        } else {
            builder.Append("WHERE ISNULL([Sale].ChangedToInvoice, [Sale].Updated) >= @From ");
            builder.Append("AND ISNULL([Sale].ChangedToInvoice, [Sale].Updated) <= @To ");
        }

        builder.Append("AND [Sale].Deleted = 0 ");
        builder.Append("AND [Sale].IsMerged = 0 ");
        builder.Append("AND [Organization].Culture = @Culture ");

        builder.Append(forEcommerce ? "AND [Order].OrderSource = 0 " : "AND [Order].OrderSource != 0 ");

        if (!fastEcommerce)
            builder.Append("AND [Sale].[RetailClientId] IS NULL ");

        if (fastEcommerce) builder.Append("AND [Sale].[RetailClientId] IS NOT NULL ");

        if (clientId.HasValue && fastEcommerce)
            builder.Append("AND [Sale].[RetailClientId] = @ClientId ");
        else if (clientId.HasValue)
            builder.Append("AND [Client].[ID] = @ClientId ");

        if (retailClientNetId.HasValue)
            builder.Append("AND [RetailClient].NetUID = @RetailClientNetId ");

        if (organizationIds != null && organizationIds.Any())
            builder.Append("AND [Organization].ID IN @OrganizationIds ");

        // Ensures it gets only lifecycle status
        if (saleLifeCycleType != null && Enum.IsDefined(typeof(SaleLifeCycleType), saleLifeCycleType)
                                      && saleLifeCycleType != SaleLifeCycleType.InvoiceChanged
                                      && saleLifeCycleType != SaleLifeCycleType.TransporterChanged
                                      && saleLifeCycleType != SaleLifeCycleType.OrderClosed)
            builder.Append(
                saleLifeCycleType.Equals(SaleLifeCycleType.Packaging)
                    ? $"AND [BaseLifeCycleStatus].SaleLifeCycleType IN ({string.Join(", ", saleLifeCycleTypesExceptNew)}) "
                    : "AND [BaseLifeCycleStatus].SaleLifeCycleType = @SaleLifeCycleType ");

        if (saleLifeCycleType != null && Enum.IsDefined(typeof(SaleLifeCycleType), saleLifeCycleType) && saleLifeCycleType == SaleLifeCycleType.InvoiceChanged)
            builder.Append("AND [HistoryInvoiceEdit].ID IS NOT NULL ");

        if (saleLifeCycleType != null && Enum.IsDefined(typeof(SaleLifeCycleType), saleLifeCycleType) && saleLifeCycleType == SaleLifeCycleType.TransporterChanged)
            builder.Append("AND [UpdateDataCarrier].ID IS NOT NULL ");

        if (saleLifeCycleType != null && Enum.IsDefined(typeof(SaleLifeCycleType), saleLifeCycleType) && saleLifeCycleType == SaleLifeCycleType.OrderClosed)
            builder.Append("AND [Order].OrderStatus = @OrderClosedStatus ");

        if (userNetId.HasValue) builder.Append("AND [SaleUser].NetUID = @UserNetId ");

        if (!string.IsNullOrEmpty(value)) {
            builder.Append("AND ");
            builder.Append("( ");
            builder.Append("PATINDEX('%' + @Value + '%', [Product].VendorCode) > 0 ");
            builder.Append("OR PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) > 0 ");
            builder.Append("OR PATINDEX('%' + @Value + '%', [SaleNumber].Value) > 0 ");

            builder.Append(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                ? "OR PATINDEX('%' + @Value + '%', [Product].[NameUA]) > 0 "
                : "OR PATINDEX('%' + @Value + '%', [Product].[NameUA]) > 0 ");

            builder.Append(") ");
        }

        builder.Append("GROUP BY [Sale].ID) ");
        builder.Append("SELECT [Sale].ID ");
        builder.Append(", (SELECT TOP 1 [TotalRowsQty] FROM [SALE_IDS_CTE]) AS [TotalRowsQty] ");
        builder.Append("FROM [Sale] ");
        builder.Append("WHERE [Sale].[ID] IN ( ");
        builder.Append("SELECT [SALE_IDS_CTE].[ID] ");
        builder.Append("FROM [SALE_IDS_CTE] ");
        builder.Append("WHERE [SALE_IDS_CTE].[RowNumber] > @Offset ");
        builder.Append("AND [SALE_IDS_CTE].[RowNumber] <= @Limit + @Offset ");
        builder.Append(") ");
        builder.Append("GROUP BY [Sale].ID, [Sale].Updated, [Sale].ChangedToInvoice ");
        builder.Append("ORDER BY ISNULL(Sale.ChangedToInvoice, Sale.Updated) DESC ");

        return builder.ToString();
    }
}