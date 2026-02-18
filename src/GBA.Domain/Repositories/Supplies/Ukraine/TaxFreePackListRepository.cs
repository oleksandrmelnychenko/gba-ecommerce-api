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
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class TaxFreePackListRepository : ITaxFreePackListRepository {
    private readonly IDbConnection _connection;

    private readonly IDbConnection _exchangeRateConnection;

    public TaxFreePackListRepository(IDbConnection connection, IDbConnection exchangeRateConnection) {
        _connection = connection;

        _exchangeRateConnection = exchangeRateConnection;
    }

    public long Add(TaxFreePackList taxFreePackList) {
        return _connection.Query<long>(
            "INSERT INTO [TaxFreePackList] (Number, Comment, IsSent, FromDate, ResponsibleId, OrganizationId, IsFromSale, ClientId, MarginAmount, Updated, " +
            "WeightLimit, MaxPriceLimit, MinPriceLimit, MaxQtyInTaxFree, MaxPositionsInTaxFree, ClientAgreementId) " +
            "VALUES (@Number, @Comment, @IsSent, @FromDate, @ResponsibleId, @OrganizationId, @IsFromSale, @ClientId, 0.00, GETUTCDATE(), " +
            "@WeightLimit, @MaxPriceLimit, @MinPriceLimit, @MaxQtyInTaxFree, @MaxPositionsInTaxFree, @ClientAgreementId); " +
            "SELECT SCOPE_IDENTITY()",
            taxFreePackList
        ).Single();
    }

    public void Update(TaxFreePackList taxFreePackList) {
        _connection.Execute(
            "UPDATE [TaxFreePackList] " +
            "SET Comment = @Comment, IsSent = @IsSent, FromDate = @FromDate, ResponsibleId = @ResponsibleId, OrganizationId = @OrganizationId, " +
            "SupplyOrderUkraineId = @SupplyOrderUkraineId, ClientId = @ClientId, MarginAmount = @MarginAmount, Updated = GETUTCDATE(), " +
            "WeightLimit = @WeightLimit, MaxPriceLimit = @MaxPriceLimit, MinPriceLimit = @MinPriceLimit, MaxQtyInTaxFree = @MaxQtyInTaxFree, " +
            "MaxPositionsInTaxFree = @MaxPositionsInTaxFree, ClientAgreementId = @ClientAgreementId " +
            "WHERE ID = @Id",
            taxFreePackList
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [TaxFreePackList] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public TaxFreePackList GetLastRecord() {
        return _connection.Query<TaxFreePackList>(
            "SELECT TOP(1) * " +
            "FROM [TaxFreePackList] " +
            "WHERE [TaxFreePackList].Deleted = 0 " +
            "ORDER BY [TaxFreePackList].ID DESC"
        ).SingleOrDefault();
    }

    public TaxFreePackList GetById(long id) {
        TaxFreePackList toReturn =
            _connection.Query<TaxFreePackList, User, Organization, SupplyOrderUkraine, Client, ClientAgreement, Agreement, TaxFreePackList>(
                "SELECT * " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [TaxFreePackList].ID = @Id",
                (packList, responsible, organization, orderUkraine, client, clientAgreement, agreement) => {
                    if (clientAgreement != null) clientAgreement.Agreement = agreement;

                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.SupplyOrderUkraine = orderUkraine;
                    packList.Client = client;
                    packList.ClientAgreement = clientAgreement;

                    return packList;
                },
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).SingleOrDefault();

        if (toReturn == null) return null;

        if (toReturn.IsFromSale) {
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
                "WHEN [OrderItem].[IsFromReSale] = 0 " +
                "THEN dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                "END) AS [CurrentPrice] " +
                ", (CASE " +
                "WHEN [OrderItem].[IsFromReSale] = 0 " +
                "THEN dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                "END) AS [CurrentLocalPrice] " +
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
                "LEFT OUTER JOIN ClientSubClient " +
                "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
                "LEFT OUTER JOIN Client AS SubClient " +
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
                "WHERE Sale.TaxFreePackListID = @Id " +
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

                if (toReturn.Sales.Any(s => s.Id.Equals(sale.Id))) {
                    Sale saleFromList = toReturn.Sales.First(c => c.Id.Equals(sale.Id));

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

                    toReturn.Sales.Add(sale);
                }

                return sale;
            };

            var props = new {
                toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = toReturn.ClientAgreement?.Agreement?.WithVATAccounting ?? false
            };

            _connection.Query(sqlExpression, types, mapper, props);

            string taxFreeSqlExpression =
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
                "LEFT JOIN [StathamPassport] " +
                "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
                "LEFT JOIN [Order] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].OrderID = [Order].ID " +
                "LEFT JOIN [SaleInvoiceDocument] " +
                "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
                "WHERE [TaxFree].Deleted = 0 " +
                "AND [TaxFree].TaxFreePackListID = @Id " +
                "ORDER BY [TaxFree].[Number]";

            Type[] taxFreeTypes = {
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
                typeof(StathamPassport),
                typeof(Order),
                typeof(Sale),
                typeof(SaleInvoiceDocument)
            };

            bool isPlCurrency = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

            Func<object[], TaxFree> taxFreeMapper = objects => {
                TaxFree taxFree = (TaxFree)objects[0];
                Statham statham = (Statham)objects[1];
                StathamCar stathamCar = (StathamCar)objects[2];
                User taxFreeResponsible = (User)objects[3];
                TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
                TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
                TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[6];
                OrderItem item = (OrderItem)objects[7];
                Product product = (Product)objects[8];
                MeasureUnit measureUnit = (MeasureUnit)objects[9];
                StathamPassport stathamPassport = (StathamPassport)objects[10];
                SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[13];

                if (!toReturn.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                    if (taxFreeItem != null) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;

                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = item.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : item.ExchangeRateAmount;

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                4,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmount =
                            decimal.Round(item.PricePerItemWithoutVat * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat = item.PricePerItem;
//                                decimal.Round(
//                                    item.PricePerItem,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

                        taxFree.UnitPriceWithVat += taxFreeItem.UnitPriceWithVat;
//                                decimal.Round(
//                                    taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty),
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
                            decimal.Round(item.TotalAmountLocal * (saleInvoiceDocument?.Vat ?? 23) / 100, 4, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl += item.TotalAmountLocal;
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl = //taxFreeItem.TotalWithVatPl;
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFreePackListOrderItem.OrderItem = item;

                        taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

                        taxFree.TaxFreeItems.Add(taxFreeItem);
                    }

                    if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                    taxFree.Statham = statham;
                    taxFree.StathamPassport = stathamPassport;
                    taxFree.StathamCar = stathamCar;
                    taxFree.Responsible = taxFreeResponsible;

                    toReturn.TaxFrees.Add(taxFree);
                } else {
                    TaxFree fromList = toReturn.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                    if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;

                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = item.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1))
                            : item.ExchangeRateAmount;

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                4,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmount = decimal.Round(item.PricePerItemWithoutVat * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat = item.PricePerItem;
//                                decimal.Round(
//                                    item.PricePerItem,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

                        fromList.UnitPriceWithVat += taxFreeItem.UnitPriceWithVat;
//                                decimal.Round(
//                                    fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty),
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
                            decimal.Round(item.TotalAmountLocal * (saleInvoiceDocument?.Vat ?? 23) / 100, 4, MidpointRounding.AwayFromZero);

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl += item.TotalAmountLocal;
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl = //taxFreeItem.TotalWithVatPl;
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFreePackListOrderItem.OrderItem = item;

                        taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

                        fromList.TaxFreeItems.Add(taxFreeItem);
                    }

                    if (taxFreeDocument != null && !fromList.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) fromList.TaxFreeDocuments.Add(taxFreeDocument);
                }

                return taxFree;
            };

            foreach (TaxFree taxFree in toReturn.TaxFrees) {
                taxFree.TotalWithVat = decimal.Round(taxFree.TotalWithVat, 2, MidpointRounding.AwayFromZero);
                taxFree.TotalWithVatPl = decimal.Round(taxFree.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
            }

            _connection.Query(
                taxFreeSqlExpression,
                taxFreeTypes,
                taxFreeMapper,
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

            toReturn.TaxFreePackListOrderItems =
                _connection.Query<TaxFreePackListOrderItem, OrderItem, Product, MeasureUnit, TaxFreePackListOrderItem>(
                    "SELECT * " +
                    "FROM [TaxFreePackListOrderItem] " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
                    "LEFT JOIN [Product] " +
                    "ON [OrderItem].ProductID = [Product].ID " +
                    "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                    "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                    "AND [MeasureUnit].CultureCode = @Culture " +
                    "WHERE [TaxFreePackListOrderItem].TaxFreePackListID = @Id",
                    (item, orderItem, product, measureUnit) => {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        orderItem.Product = product;

                        item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 2);

                        item.NetWeight =
                            item.NetWeight > 0
                                ? item.NetWeight
                                : product.Weight > 0
                                    ? product.Weight
                                    : 0.07;

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        if (orderItem.ExchangeRateAmount.Equals(decimal.Zero)) {
                            orderItem.TotalAmountLocal =
                                orderItem.TotalAmount =
                                    decimal.Round(
                                        orderItem.PricePerItemWithoutVat * Convert.ToDecimal(item.Qty),
                                        4,
                                        MidpointRounding.AwayFromZero
                                    );

                            item.UnitPriceLocal =
                                decimal.Round(
                                    orderItem.PricePerItemWithoutVat,
                                    4,
                                    MidpointRounding.AwayFromZero
                                );
                        } else {
                            orderItem.TotalAmountLocal =
                                decimal.Round(
                                    orderItem.PricePerItemWithoutVat * orderItem.ExchangeRateAmount * Convert.ToDecimal(item.Qty),
                                    4,
                                    MidpointRounding.AwayFromZero
                                );

                            orderItem.TotalAmount = decimal.Round(orderItem.PricePerItemWithoutVat * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                            item.UnitPriceLocal =
                                decimal.Round(
                                    orderItem.PricePerItemWithoutVat * orderItem.ExchangeRateAmount,
                                    4,
                                    MidpointRounding.AwayFromZero
                                );
                        }

                        toReturn.TotalUnspecifiedWeight =
                            Math.Round(toReturn.TotalUnspecifiedWeight + item.NetWeight * item.UnpackedQty, 3, MidpointRounding.AwayFromZero);

                        orderItem.TotalAmount =
                            decimal.Round(
                                orderItem.PricePerItemWithoutVat * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPriceLocal * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.Coef = Convert.ToDouble(item.UnitPriceLocal) / item.NetWeight;

                        item.OrderItem = orderItem;

                        return item;
                    },
                    new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                ).ToList();

            toReturn.TotalUnspecifiedAmount =
                decimal.Round(toReturn.TaxFreePackListOrderItems.Sum(i => i.OrderItem.TotalAmount), 2, MidpointRounding.AwayFromZero);

            toReturn.TotalUnspecifiedAmountLocal =
                decimal.Round(toReturn.TaxFreePackListOrderItems.Sum(i => i.OrderItem.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
        } else {
            decimal plnExchangeRate = GetPlnExchangeRate(toReturn.FromDate.AddDays(-1));

            bool isPlCurrency = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

            toReturn.SupplyOrderUkraineCartItems =
                _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, Client, SupplyOrderUkraineCartItem>(
                    "SELECT * " +
                    "FROM [SupplyOrderUkraineCartItem] " +
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
                    "LEFT JOIN [Client] AS [Supplier] " +
                    "ON [SupplyOrderUkraineCartItem].SupplierID = [Supplier].ID " +
                    "WHERE [SupplyOrderUkraineCartItem].TaxFreePackListID = @Id " +
                    "ORDER BY [Product].VendorCode",
                    (item, product, measureUnit, createdBy, updatedBy, responsible, supplier) => {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;
                        item.Supplier = supplier;

                        if (double.TryParse(product.PackingStandard, out double packageSize))
                            item.PackageSize = packageSize;
                        else
                            item.PackageSize = 0;

                        item.NetWeight =
                            item.NetWeight > 0
                                ? item.NetWeight
                                : product.Weight > 0
                                    ? product.Weight
                                    : 0.07;

                        item.TotalNetWeight = Math.Round(item.NetWeight * item.UnpackedQty, 3, MidpointRounding.AwayFromZero);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * toReturn.MarginAmount / 100m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        // item.UnitPrice =
                        //     decimal.Round(
                        //         item.UnitPrice + item.UnitPrice * 0.23m,
                        //         2,
                        //         MidpointRounding.AwayFromZero
                        //     );

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.UnitPriceLocal =
                            decimal.Round(
                                item.UnitPrice * plnExchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.Coef = Convert.ToDouble(item.UnitPriceLocal) / item.NetWeight;

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPriceLocal * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.TotalUnspecifiedWeight =
                            Math.Round(toReturn.TotalUnspecifiedWeight + item.NetWeight * item.UnpackedQty, 3, MidpointRounding.AwayFromZero);

                        return item;
                    },
                    new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                ).ToList();

            toReturn.TotalUnspecifiedAmount =
                decimal.Round(toReturn.SupplyOrderUkraineCartItems.Sum(i => i.TotalAmount), 2, MidpointRounding.AwayFromZero);

            toReturn.TotalUnspecifiedAmountLocal =
                decimal.Round(toReturn.SupplyOrderUkraineCartItems.Sum(i => i.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);

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
                "LEFT JOIN [StathamPassport] " +
                "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
                "WHERE [TaxFree].Deleted = 0 " +
                "AND [TaxFree].TaxFreePackListID = @Id " +
                "ORDER BY [TaxFree].[Number]";

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
                StathamPassport stathamPassport = (StathamPassport)objects[12];

                if (!toReturn.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                    if (taxFreeItem != null) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        item.TotalNetWeight = taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1));

                        item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * toReturn.MarginAmount / 100m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl = item.UnitPrice * 0.23m;

                        taxFreeItem.UnitPriceWithVat = item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + taxFreeItem.VatAmountPl,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat = item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.UnitPriceLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVatPl = item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPriceLocal * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(
                                taxFreeItem.VatAmountPl * Convert.ToDecimal(taxFreeItem.Qty) * exchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFree.TotalWithVat = decimal.Round(taxFree.TotalWithVat + taxFreeItem.TotalWithVat, 2, MidpointRounding.AwayFromZero);

//                            item.TotalAmount =
//                                decimal.Round(
//                                    item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            item.TotalAmountLocal =
//                                decimal.Round(
//                                    item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

//                            taxFreeItem.UnitPriceWithVat =
//                                decimal.Round(
//                                    item.UnitPrice + item.UnitPrice * 0.23m,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFree.UnitPriceWithVat =
//                                decimal.Round(
//                                    taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.TotalWithVat =
//                                decimal.Round(
//                                    item.TotalAmount + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFree.TotalWithVat =
//                                decimal.Round(
//                                    taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

//                            taxFreeItem.VatAmountPl =
//                                decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

//                            taxFree.VatAmountPl =
//                                decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            taxFreeItem.TotalWithVatPl =
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            taxFree.TotalWithVatPl =
//                                decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.SupplyOrderUkraineCartItem = item;

                        taxFree.TaxFreeItems.Add(taxFreeItem);
                    }

                    if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                    taxFree.Statham = statham;
                    taxFree.StathamPassport = stathamPassport;
                    taxFree.StathamCar = stathamCar;
                    taxFree.Responsible = taxFreeResponsible;

                    toReturn.TaxFrees.Add(taxFree);
                } else {
                    TaxFree fromList = toReturn.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                    if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        decimal exchangeRate = GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1));

                        item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * toReturn.MarginAmount / 100m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl = item.UnitPrice * 0.23m;

                        taxFreeItem.UnitPriceWithVat = item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + taxFreeItem.VatAmountPl,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat = item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.UnitPriceLocal =
                            decimal.Round(
                                item.UnitPrice * exchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVatPl = item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPriceLocal * Convert.ToDecimal(taxFreeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.VatAmountPl =
                            decimal.Round(
                                taxFreeItem.VatAmountPl * Convert.ToDecimal(taxFreeItem.Qty) * exchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl =
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVat = decimal.Round(fromList.TotalWithVat + taxFreeItem.TotalWithVat, 2, MidpointRounding.AwayFromZero);

//                            item.TotalAmount =
//                                decimal.Round(
//                                    item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            item.TotalAmountLocal =
//                                decimal.Round(
//                                    item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.UnitPriceWithVat =
//                                decimal.Round(
//                                    item.UnitPrice + item.UnitPrice * 0.23m,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            fromList.UnitPriceWithVat =
//                                decimal.Round(
//                                    fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.TotalWithVat =
//                                decimal.Round(
//                                    item.TotalAmount + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            fromList.TotalWithVat =
//                                decimal.Round(
//                                    fromList.TotalWithVat + taxFreeItem.TotalWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.VatAmountPl =
//                                decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);
//
//                            fromList.VatAmountPl =
//                                decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            taxFreeItem.TotalWithVatPl =
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            fromList.TotalWithVatPl =
//                                decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

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
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        toReturn.TotalWeight = Math.Round(toReturn.TaxFrees.Sum(t => t.TotalNetWeight), 2, MidpointRounding.AwayFromZero);
        toReturn.TotalAmount = decimal.Round(toReturn.TaxFrees.Sum(t => t.TotalWithVat), 2, MidpointRounding.AwayFromZero);
        toReturn.TotalAmountLocal = decimal.Round(toReturn.TaxFrees.Sum(t => t.TotalWithVatPl), 2, MidpointRounding.AwayFromZero);
        toReturn.TotalVatAmountLocal = decimal.Round(toReturn.TaxFrees.Sum(t => t.VatAmountPl), 2, MidpointRounding.AwayFromZero);

        return toReturn;
    }

    public TaxFreePackList GetByIdForConsignment(long id) {
        TaxFreePackList toReturn = null;

        Type[] types = {
            typeof(TaxFreePackList),
            typeof(Organization),
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation),
            typeof(ProductAvailability),
            typeof(ConsignmentItem)
        };

        Func<object[], TaxFreePackList> mapper = objects => {
            TaxFreePackList packList = (TaxFreePackList)objects[0];
            Organization organization = (Organization)objects[1];
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[2];
            SupplyOrderUkraineCartItemReservation cartItemReservation = (SupplyOrderUkraineCartItemReservation)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[5];

            if (toReturn == null) {
                packList.Organization = organization;

                toReturn = packList;
            }

            if (cartItem == null) return packList;

            if (toReturn.SupplyOrderUkraineCartItems.Any(i => i.Id.Equals(cartItem.Id)))
                cartItem = toReturn.SupplyOrderUkraineCartItems.First(i => i.Id.Equals(cartItem.Id));
            else
                toReturn.SupplyOrderUkraineCartItems.Add(cartItem);

            if (cartItemReservation == null) return packList;

            cartItemReservation.ProductAvailability = productAvailability;
            cartItemReservation.ConsignmentItem = consignmentItem;

            cartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

            return packList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [TaxFreePackList] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].TaxFreePackListID = [TaxFreePackList].ID " +
            "AND [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyOrderUkraineCartItemReservation].ConsignmentItemID " +
            "WHERE [TaxFreePackList].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public TaxFreePackList GetByIdForConsignmentMovement(long id) {
        TaxFreePackList toReturn = null;

        Type[] types = {
            typeof(TaxFreePackList),
            typeof(Organization),
            typeof(TaxFree),
            typeof(TaxFreeItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation)
        };

        Func<object[], TaxFreePackList> mapper = objects => {
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[0];
            Organization organization = (Organization)objects[1];
            TaxFree taxFree = (TaxFree)objects[2];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[3];
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[4];
            SupplyOrderUkraineCartItemReservation cartItemReservation = (SupplyOrderUkraineCartItemReservation)objects[5];

            if (toReturn == null) {
                taxFreePackList.Organization = organization;

                toReturn = taxFreePackList;
            }

            if (taxFree == null) return taxFreePackList;

            if (toReturn.TaxFrees.Any(t => t.Id.Equals(taxFree.Id)))
                taxFree = toReturn.TaxFrees.First(t => t.Id.Equals(taxFree.Id));
            else
                toReturn.TaxFrees.Add(taxFree);

            if (taxFreeItem == null) return taxFreePackList;

            if (taxFree.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                taxFreeItem = taxFree.TaxFreeItems.First(i => i.Id.Equals(taxFreeItem.Id));
            } else {
                taxFreeItem.SupplyOrderUkraineCartItem = cartItem;

                taxFree.TaxFreeItems.Add(taxFreeItem);
            }

            if (cartItemReservation == null) return taxFreePackList;

            taxFreeItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

            return taxFreePackList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [TaxFreePackList] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].TaxFreePackListID = [TaxFreePackList].ID " +
            "AND [TaxFree].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "WHERE [TaxFreePackList].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public TaxFreePackList GetByIdForConsignmentMovementFromSale(long id) {
        TaxFreePackList toReturn = null;

        Type[] types = {
            typeof(TaxFreePackList),
            typeof(Organization),
            typeof(TaxFree),
            typeof(TaxFreeItem),
            typeof(TaxFreePackListOrderItem)
        };

        Func<object[], TaxFreePackList> mapper = objects => {
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[0];
            Organization organization = (Organization)objects[1];
            TaxFree taxFree = (TaxFree)objects[2];
            TaxFreeItem taxFreeItem = (TaxFreeItem)objects[3];
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[4];

            if (toReturn == null) {
                taxFreePackList.Organization = organization;

                toReturn = taxFreePackList;
            }

            if (taxFree == null) return taxFreePackList;

            if (toReturn.TaxFrees.Any(t => t.Id.Equals(taxFree.Id)))
                taxFree = toReturn.TaxFrees.First(t => t.Id.Equals(taxFree.Id));
            else
                toReturn.TaxFrees.Add(taxFree);

            if (taxFreeItem == null) return taxFreePackList;

            taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

            taxFree.TaxFreeItems.Add(taxFreeItem);

            return taxFreePackList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [TaxFreePackList] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].TaxFreePackListID = [TaxFreePackList].ID " +
            "AND [TaxFree].Deleted = 0 " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].TaxFreeID = [TaxFree].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "WHERE [TaxFreePackList].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public TaxFreePackList GetByNetId(Guid netId) {
        TaxFreePackList toReturn =
            _connection.Query<TaxFreePackList, User, Organization, SupplyOrderUkraine, Client, ClientAgreement, Agreement, TaxFreePackList>(
                "SELECT * " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [TaxFreePackList].NetUID = @NetId",
                (packList, responsible, organization, orderUkraine, client, clientAgreement, agreement) => {
                    if (clientAgreement != null) clientAgreement.Agreement = agreement;

                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.SupplyOrderUkraine = orderUkraine;
                    packList.Client = client;
                    packList.ClientAgreement = clientAgreement;

                    return packList;
                },
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).SingleOrDefault();

        if (toReturn == null) return null;

        if (toReturn.IsFromSale) {
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
                "WHEN [OrderItem].[IsFromReSale] = 0 " +
                "THEN dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                "END) AS [CurrentPrice] " +
                ", (CASE " +
                "WHEN [OrderItem].[IsFromReSale] = 0 " +
                "THEN dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                "END) AS [CurrentLocalPrice] " +
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
                "LEFT OUTER JOIN ClientSubClient " +
                "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
                "LEFT OUTER JOIN Client AS SubClient " +
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
                "WHERE Sale.TaxFreePackListID = @Id " +
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

                if (toReturn.Sales.Any(s => s.Id.Equals(sale.Id))) {
                    Sale saleFromList = toReturn.Sales.First(c => c.Id.Equals(sale.Id));

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

                    toReturn.Sales.Add(sale);
                }

                return sale;
            };

            var props = new {
                toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = toReturn.ClientAgreement?.Agreement?.WithVATAccounting ?? false
            };

            _connection.Query(sqlExpression, types, mapper, props);

            string taxFreeSqlExpression =
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
                "LEFT JOIN [StathamPassport] " +
                "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
                "LEFT JOIN [Order] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].OrderID = [Order].ID " +
                "LEFT JOIN [SaleInvoiceDocument] " +
                "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
                "WHERE [TaxFree].Deleted = 0 " +
                "AND [TaxFree].TaxFreePackListID = @Id " +
                "ORDER BY [TaxFree].[Number]";

            Type[] taxFreeTypes = {
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
                typeof(StathamPassport),
                typeof(Order),
                typeof(Sale),
                typeof(SaleInvoiceDocument)
            };

            bool isPlCurrency = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

            Func<object[], TaxFree> taxFreeMapper = objects => {
                TaxFree taxFree = (TaxFree)objects[0];
                Statham statham = (Statham)objects[1];
                StathamCar stathamCar = (StathamCar)objects[2];
                User taxFreeResponsible = (User)objects[3];
                TaxFreeDocument taxFreeDocument = (TaxFreeDocument)objects[4];
                TaxFreeItem taxFreeItem = (TaxFreeItem)objects[5];
                TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[6];
                OrderItem item = (OrderItem)objects[7];
                Product product = (Product)objects[8];
                MeasureUnit measureUnit = (MeasureUnit)objects[9];
                StathamPassport stathamPassport = (StathamPassport)objects[10];
                SaleInvoiceDocument saleInvoiceDocument = (SaleInvoiceDocument)objects[13];

                if (!toReturn.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                    if (taxFreeItem != null) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;

                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        taxFree.TotalNetWeight = Math.Round(taxFree.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = item.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(taxFree.DateOfPrint?.AddDays(-1) ?? taxFree.Created.AddDays(-1))
                            : item.ExchangeRateAmount;

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                4,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmount =
                            decimal.Round(item.PricePerItemWithoutVat * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat = item.PricePerItem;

                        taxFree.UnitPriceWithVat += taxFreeItem.UnitPriceWithVat;

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty),
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
                            decimal.Round(item.TotalAmountLocal * (saleInvoiceDocument?.Vat ?? 23) / 100, 4, MidpointRounding.AwayFromZero);

                        taxFree.VatAmountPl =
                            decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl += item.TotalAmountLocal;

                        taxFree.TotalWithVatPl =
                            decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFreePackListOrderItem.OrderItem = item;

                        taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

                        taxFree.TaxFreeItems.Add(taxFreeItem);
                    }

                    if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                    taxFree.Statham = statham;
                    taxFree.StathamPassport = stathamPassport;
                    taxFree.StathamCar = stathamCar;
                    taxFree.Responsible = taxFreeResponsible;

                    toReturn.TaxFrees.Add(taxFree);
                } else {
                    TaxFree fromList = toReturn.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                    if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;

                        taxFreeItem.TotalNetWeight = Math.Round(taxFreePackListOrderItem.NetWeight * taxFreeItem.Qty, 2);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 2);

                        decimal exchangeRate = item.ExchangeRateAmount.Equals(decimal.Zero)
                            ? 1m //GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1))
                            : item.ExchangeRateAmount;

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.PricePerItem * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
                                4,
                                MidpointRounding.AwayFromZero
                            );

                        item.TotalAmount = decimal.Round(item.PricePerItemWithoutVat * Convert.ToDecimal(taxFreeItem.Qty), 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.UnitPriceWithVat = item.PricePerItem;

                        fromList.UnitPriceWithVat += taxFreeItem.UnitPriceWithVat;

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                item.PricePerItem * Convert.ToDecimal(taxFreeItem.Qty),
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
                            decimal.Round(item.TotalAmountLocal * (saleInvoiceDocument?.Vat ?? 23) / 100, 4, MidpointRounding.AwayFromZero);

                        fromList.VatAmountPl =
                            decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.TotalWithVatPl += item.TotalAmountLocal;
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalWithVatPl = //taxFreeItem.TotalWithVatPl;
                            decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFreePackListOrderItem.OrderItem = item;

                        taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;

                        fromList.TaxFreeItems.Add(taxFreeItem);
                    }

                    if (taxFreeDocument != null && !fromList.TaxFreeDocuments.Any(d => d.Id.Equals(taxFreeDocument.Id))) fromList.TaxFreeDocuments.Add(taxFreeDocument);
                }

                return taxFree;
            };

            foreach (TaxFree taxFree in toReturn.TaxFrees) {
                taxFree.TotalWithVat = decimal.Round(taxFree.TotalWithVat, 2, MidpointRounding.AwayFromZero);
                taxFree.TotalWithVatPl = decimal.Round(taxFree.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);
            }

            _connection.Query(
                taxFreeSqlExpression,
                taxFreeTypes,
                taxFreeMapper,
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

            toReturn.TaxFreePackListOrderItems =
                _connection.Query<TaxFreePackListOrderItem, OrderItem, Product, MeasureUnit, TaxFreePackListOrderItem>(
                    "SELECT * " +
                    "FROM [TaxFreePackListOrderItem] " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
                    "LEFT JOIN [Product] " +
                    "ON [OrderItem].ProductID = [Product].ID " +
                    "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                    "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                    "AND [MeasureUnit].CultureCode = @Culture " +
                    "WHERE [TaxFreePackListOrderItem].TaxFreePackListID = @Id",
                    (item, orderItem, product, measureUnit) => {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        orderItem.Product = product;

                        item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 2);

                        item.NetWeight =
                            item.NetWeight > 0
                                ? item.NetWeight
                                : product.Weight > 0
                                    ? product.Weight
                                    : 0.07;

                        item.NetWeight = Math.Round(item.NetWeight, 2);

                        if (orderItem.ExchangeRateAmount.Equals(decimal.Zero)) {
                            orderItem.TotalAmountLocal =
                                orderItem.TotalAmount =
                                    decimal.Round(
                                        orderItem.PricePerItemWithoutVat * Convert.ToDecimal(item.Qty),
                                        4,
                                        MidpointRounding.AwayFromZero
                                    );

                            item.UnitPriceLocal =
                                decimal.Round(
                                    orderItem.PricePerItemWithoutVat,
                                    4,
                                    MidpointRounding.AwayFromZero
                                );
                        } else {
                            orderItem.TotalAmountLocal =
                                decimal.Round(
                                    orderItem.PricePerItemWithoutVat * orderItem.ExchangeRateAmount * Convert.ToDecimal(item.Qty),
                                    4,
                                    MidpointRounding.AwayFromZero
                                );

                            orderItem.TotalAmount = decimal.Round(orderItem.PricePerItemWithoutVat * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                            item.UnitPriceLocal =
                                decimal.Round(
                                    orderItem.PricePerItemWithoutVat * orderItem.ExchangeRateAmount,
                                    4,
                                    MidpointRounding.AwayFromZero
                                );
                        }

                        toReturn.TotalUnspecifiedWeight =
                            Math.Round(toReturn.TotalUnspecifiedWeight + item.NetWeight * item.UnpackedQty, 3, MidpointRounding.AwayFromZero);

                        orderItem.TotalAmount =
                            decimal.Round(
                                orderItem.PricePerItemWithoutVat * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        orderItem.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPriceLocal * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.Coef = Convert.ToDouble(item.UnitPriceLocal) / item.NetWeight;

                        item.OrderItem = orderItem;

                        return item;
                    },
                    new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                ).ToList();

            toReturn.TotalUnspecifiedAmount =
                decimal.Round(toReturn.TaxFreePackListOrderItems.Sum(i => i.OrderItem.TotalAmount), 2, MidpointRounding.AwayFromZero);

            toReturn.TotalUnspecifiedAmountLocal =
                decimal.Round(toReturn.TaxFreePackListOrderItems.Sum(i => i.OrderItem.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
        } else {
            decimal plnExchangeRate = GetPlnExchangeRate(toReturn.FromDate.AddDays(-1));

            bool isPlCurrency = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

            toReturn.SupplyOrderUkraineCartItems =
                _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, Client, SupplyOrderUkraineCartItem>(
                    "SELECT * " +
                    "FROM [SupplyOrderUkraineCartItem] " +
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
                    "LEFT JOIN [Client] AS [Supplier] " +
                    "ON [SupplyOrderUkraineCartItem].SupplierID = [Supplier].ID " +
                    "WHERE [SupplyOrderUkraineCartItem].TaxFreePackListID = @Id " +
                    "AND [SupplyOrderUkraineCartItem].Deleted = 0 " +
                    "ORDER BY [Product].VendorCode",
                    (item, product, measureUnit, createdBy, updatedBy, responsible, supplier) => {
                        product.MeasureUnit = measureUnit;

                        product.Name =
                            isPlCurrency
                                ? product.NameUA
                                : product.NameUA;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;
                        item.Supplier = supplier;

                        if (double.TryParse(product.PackingStandard, out double packageSize))
                            item.PackageSize = packageSize;
                        else
                            item.PackageSize = 0;

                        item.NetWeight =
                            item.NetWeight > 0
                                ? item.NetWeight
                                : product.Weight > 0
                                    ? product.Weight
                                    : 0.07;

                        item.TotalNetWeight = Math.Round(item.NetWeight * item.UnpackedQty, 2);

                        item.NetWeight = Math.Round(item.NetWeight, 3, MidpointRounding.AwayFromZero);

                        item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * toReturn.MarginAmount / 100m,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        // item.UnitPrice =
                        //     decimal.Round(
                        //         item.UnitPrice + item.UnitPrice * 0.23m,
                        //         2,
                        //         MidpointRounding.AwayFromZero
                        //     );

                        item.TotalAmount =
                            decimal.Round(
                                item.UnitPrice * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.UnitPriceLocal =
                            decimal.Round(
                                item.UnitPrice * plnExchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        item.Coef = Convert.ToDouble(item.UnitPriceLocal) / item.NetWeight;

                        item.TotalAmountLocal =
                            decimal.Round(
                                item.UnitPriceLocal * Convert.ToDecimal(item.UnpackedQty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        toReturn.TotalUnspecifiedWeight =
                            Math.Round(toReturn.TotalUnspecifiedWeight + item.NetWeight * item.UnpackedQty, 3, MidpointRounding.AwayFromZero);

                        return item;
                    },
                    new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                ).ToList();

            toReturn.TotalUnspecifiedAmount =
                decimal.Round(toReturn.SupplyOrderUkraineCartItems.Sum(i => i.TotalAmount), 2, MidpointRounding.AwayFromZero);

            toReturn.TotalUnspecifiedAmountLocal =
                decimal.Round(toReturn.SupplyOrderUkraineCartItems.Sum(i => i.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);

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
                "LEFT JOIN [StathamPassport] " +
                "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
                "WHERE [TaxFree].Deleted = 0 " +
                "AND [TaxFree].TaxFreePackListID = @Id " +
                "ORDER BY [TaxFree].[Number]";

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
                StathamPassport stathamPassport = (StathamPassport)objects[12];

                if (!toReturn.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
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
                                item.UnitPrice + item.UnitPrice * toReturn.MarginAmount / 100m,
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
                                taxFreeItem.UnitPriceWithVat * Convert.ToDecimal(taxFreeItem.Qty),
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

//                            item.TotalAmount =
//                                decimal.Round(
//                                    item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            item.TotalAmountLocal =
//                                decimal.Round(
//                                    item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

//                            taxFreeItem.UnitPriceWithVat =
//                                decimal.Round(
//                                    item.UnitPrice + item.UnitPrice * 0.23m,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFree.UnitPriceWithVat =
//                                decimal.Round(
//                                    taxFree.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.TotalWithVat =
//                                decimal.Round(
//                                    item.TotalAmount + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFree.TotalWithVat =
//                                decimal.Round(
//                                    taxFree.TotalWithVat + taxFreeItem.TotalWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );

//                            taxFreeItem.VatAmountPl =
//                                decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);

//                            taxFree.VatAmountPl =
//                                decimal.Round(taxFree.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            taxFreeItem.TotalWithVatPl =
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            taxFree.TotalWithVatPl =
//                                decimal.Round(taxFree.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

                        taxFreeItem.SupplyOrderUkraineCartItem = item;

                        taxFree.TaxFreeItems.Add(taxFreeItem);
                    }

                    if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                    taxFree.Statham = statham;
                    taxFree.StathamPassport = stathamPassport;
                    taxFree.StathamCar = stathamCar;
                    taxFree.Responsible = taxFreeResponsible;

                    toReturn.TaxFrees.Add(taxFree);
                } else {
                    TaxFree fromList = toReturn.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                    if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 3);

                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + taxFreeItem.TotalNetWeight, 3);

                        item.TotalNetWeight = Math.Round(item.NetWeight * taxFreeItem.Qty, 3);

                        item.NetWeight = Math.Round(item.NetWeight, 3);

                        decimal exchangeRate = GetPlnExchangeRate(fromList.DateOfPrint?.AddDays(-1) ?? fromList.Created.AddDays(-1));

                        item.UnitPrice =
                            decimal.Round(
                                item.UnitPrice + item.UnitPrice * toReturn.MarginAmount / 100m,
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

                        fromList.UnitPriceWithVat =
                            decimal.Round(
                                fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        taxFreeItem.TotalWithVat =
                            decimal.Round(
                                taxFreeItem.UnitPriceWithVat * Convert.ToDecimal(taxFreeItem.Qty),
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

//                            item.TotalAmount =
//                                decimal.Round(
//                                    item.UnitPrice * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            item.TotalAmountLocal =
//                                decimal.Round(
//                                    item.UnitPrice * exchangeRate * Convert.ToDecimal(taxFreeItem.Qty),
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.UnitPriceWithVat =
//                                decimal.Round(
//                                    item.UnitPrice + item.UnitPrice * 0.23m,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            fromList.UnitPriceWithVat =
//                                decimal.Round(
//                                    fromList.UnitPriceWithVat + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.TotalWithVat =
//                                decimal.Round(
//                                    item.TotalAmount + taxFreeItem.UnitPriceWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            fromList.TotalWithVat =
//                                decimal.Round(
//                                    fromList.TotalWithVat + taxFreeItem.TotalWithVat,
//                                    2,
//                                    MidpointRounding.AwayFromZero
//                                );
//
//                            taxFreeItem.VatAmountPl =
//                                decimal.Round(item.TotalAmountLocal * 0.23m, 2, MidpointRounding.AwayFromZero);
//
//                            fromList.VatAmountPl =
//                                decimal.Round(fromList.VatAmountPl + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            taxFreeItem.TotalWithVatPl =
//                                decimal.Round(item.TotalAmountLocal + taxFreeItem.VatAmountPl, 2, MidpointRounding.AwayFromZero);
//
//                            fromList.TotalWithVatPl =
//                                decimal.Round(fromList.TotalWithVatPl + taxFreeItem.TotalWithVatPl, 2, MidpointRounding.AwayFromZero);

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
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        toReturn.TotalWeight = Math.Round(toReturn.TaxFrees.Sum(t => t.TotalNetWeight), 2, MidpointRounding.AwayFromZero);
        toReturn.TotalAmount = decimal.Round(toReturn.TaxFrees.Sum(t => t.TotalWithVat), 2, MidpointRounding.AwayFromZero);
        toReturn.TotalAmountLocal = decimal.Round(toReturn.TaxFrees.Sum(t => t.TotalWithVatPl), 2, MidpointRounding.AwayFromZero);
        toReturn.TotalVatAmountLocal = decimal.Round(toReturn.TaxFrees.Sum(t => t.VatAmountPl), 2, MidpointRounding.AwayFromZero);

        return toReturn;
    }

    public TaxFreePackList GetByNetIdForConsignment(Guid netId) {
        TaxFreePackList toReturn = null;

        Type[] types = {
            typeof(TaxFreePackList),
            typeof(Organization),
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation),
            typeof(ProductAvailability),
            typeof(ConsignmentItem)
        };

        Func<object[], TaxFreePackList> mapper = objects => {
            TaxFreePackList packList = (TaxFreePackList)objects[0];
            Organization organization = (Organization)objects[1];
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[2];
            SupplyOrderUkraineCartItemReservation cartItemReservation = (SupplyOrderUkraineCartItemReservation)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[5];

            if (toReturn == null) {
                packList.Organization = organization;

                toReturn = packList;
            }

            if (cartItem == null) return packList;

            if (toReturn.SupplyOrderUkraineCartItems.Any(i => i.Id.Equals(cartItem.Id)))
                cartItem = toReturn.SupplyOrderUkraineCartItems.First(i => i.Id.Equals(cartItem.Id));
            else
                toReturn.SupplyOrderUkraineCartItems.Add(cartItem);

            if (cartItemReservation == null) return packList;

            cartItemReservation.ProductAvailability = productAvailability;
            cartItemReservation.ConsignmentItem = consignmentItem;

            cartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

            return packList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [TaxFreePackList] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].TaxFreePackListID = [TaxFreePackList].ID " +
            "AND [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyOrderUkraineCartItemReservation].ConsignmentItemID " +
            "WHERE [TaxFreePackList].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId }
        );

        return toReturn;
    }

    public IEnumerable<TaxFreePackList> GetAllNotSent() {
        IEnumerable<TaxFreePackList> packLists =
            _connection.Query<TaxFreePackList, User, Organization, Client, ClientAgreement, Agreement, TaxFreePackList>(
                "SELECT * " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [TaxFreePackList].Deleted = 0 " +
                "AND [TaxFreePackList].IsSent = 0 " +
                "AND [TaxFreePackList].IsFromSale = 0 " +
                "ORDER BY [TaxFreePackList].ID DESC",
                (packList, responsible, organization, client, clientAgreement, agreement) => {
                    if (clientAgreement != null) clientAgreement.Agreement = agreement;

                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.Client = client;
                    packList.ClientAgreement = clientAgreement;

                    return packList;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        if (!packLists.Any()) return packLists;

        object props = new { Ids = packLists.Select(p => p.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItem] " +
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
            "WHERE [SupplyOrderUkraineCartItem].TaxFreePackListID IN @Ids",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                if (!item.TaxFreePackListId.HasValue) return item;

                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                packLists.First(p => p.Id.Equals(item.TaxFreePackListId.Value)).SupplyOrderUkraineCartItems.Add(item);

                return item;
            },
            props
        );

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
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].Deleted = 0 " +
            "AND [TaxFree].TaxFreePackListID IN @Ids";

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
            StathamPassport stathamPassport = (StathamPassport)objects[12];

            TaxFreePackList packListFromList = packLists.First(p => p.Id.Equals(taxFree.TaxFreePackListId));

            if (!packListFromList.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                if (taxFreeItem != null) {
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

                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFree.Statham = statham;
                taxFree.StathamPassport = stathamPassport;
                taxFree.StathamCar = stathamCar;
                taxFree.Responsible = taxFreeResponsible;

                packListFromList.TaxFrees.Add(taxFree);
            } else {
                TaxFree fromList = packListFromList.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
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
            props
        );

        return packLists;
    }

    public IEnumerable<TaxFreePackList> GetAllNotSentFromSales() {
        IEnumerable<TaxFreePackList> packLists =
            _connection.Query<TaxFreePackList, User, Organization, Client, ClientAgreement, Agreement, TaxFreePackList>(
                "SELECT * " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [TaxFreePackList].Deleted = 0 " +
                "AND [TaxFreePackList].IsSent = 0 " +
                "AND [TaxFreePackList].IsFromSale = 1 " +
                "ORDER BY [TaxFreePackList].ID DESC",
                (packList, responsible, organization, client, clientAgreement, agreement) => {
                    if (clientAgreement != null) clientAgreement.Agreement = agreement;

                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.Client = client;
                    packList.ClientAgreement = clientAgreement;

                    return packList;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        if (!packLists.Any()) return packLists;

        string salesSqlExpression =
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
            salesSqlExpression += ",[Product].[NameUA] AS [Name] ";
            salesSqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            salesSqlExpression += ",[Product].[NameUA] AS [Name] ";
            salesSqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        salesSqlExpression +=
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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, [Agreement].[WithVATAccounting], [OrderItem].[ID]) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, [Agreement].[WithVATAccounting], [OrderItem].[ID]) AS CurrentLocalPrice " +
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
            "LEFT OUTER JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "LEFT OUTER JOIN Client AS SubClient " +
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
            "WHERE Sale.TaxFreePackListID IN @Ids " +
            "ORDER BY Sale.Created DESC";

        Type[] salesTypes = {
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

        Func<object[], Sale> salesMapper = objects => {
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

            if (!sale.TaxFreePackListId.HasValue) return sale;

            TaxFreePackList packList = packLists.First(p => p.Id.Equals(sale.TaxFreePackListId.Value));

            if (packList.Sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = packList.Sales.First(c => c.Id.Equals(sale.Id));

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

                if (orderItem != null) {
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

                packList.Sales.Add(sale);
            }

            return sale;
        };

        object props = new { Ids = packLists.Select(p => p.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(salesSqlExpression, salesTypes, salesMapper, props);

        _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItem] " +
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
            "WHERE [SupplyOrderUkraineCartItem].TaxFreePackListID IN @Ids",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                if (!item.TaxFreePackListId.HasValue) return item;

                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                packLists.First(p => p.Id.Equals(item.TaxFreePackListId.Value)).SupplyOrderUkraineCartItems.Add(item);

                return item;
            },
            props
        );

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
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].Id = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] AS [OrderItemProduct] " +
            "ON [OrderItemProduct].ID = [OrderItem].ProductID " +
            "LEFT JOIN [MeasureUnit] AS [OrderItemProductMeasureUnit] " +
            "ON [OrderItemProduct].MeasureUnitID = [OrderItemProductMeasureUnit].ID " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].Deleted = 0 " +
            "AND [TaxFree].TaxFreePackListID IN @Ids";

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
            TaxFreePackListOrderItem taxFreePackListOrderItem = (TaxFreePackListOrderItem)objects[12];
            OrderItem orderItem = (OrderItem)objects[13];
            Product orderItemProduct = (Product)objects[14];
            MeasureUnit orderItemProductMeasureUnit = (MeasureUnit)objects[15];
            StathamPassport stathamPassport = (StathamPassport)objects[16];

            TaxFreePackList packListFromList = packLists.First(p => p.Id.Equals(taxFree.TaxFreePackListId));

            if (!packListFromList.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                if (taxFreeItem != null) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.SupplyOrderUkraineCartItem = item;
                    }

                    if (orderItem != null) {
                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;

                        taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    }

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFree.Statham = statham;
                taxFree.StathamPassport = stathamPassport;
                taxFree.StathamCar = stathamCar;
                taxFree.Responsible = taxFreeResponsible;

                packListFromList.TaxFrees.Add(taxFree);
            } else {
                TaxFree fromList = packListFromList.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;

                        taxFreeItem.SupplyOrderUkraineCartItem = item;
                    }

                    if (orderItem != null) {
                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        taxFreePackListOrderItem.OrderItem = orderItem;

                        taxFreeItem.TaxFreePackListOrderItem = taxFreePackListOrderItem;
                    }

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
            props
        );

        _connection.Query<TaxFreePackListOrderItem, OrderItem, Product, MeasureUnit, TaxFreePackListOrderItem>(
            "SELECT * " +
            "FROM [TaxFreePackListOrderItem] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [TaxFreePackListOrderItem].TaxFreePackListID IN @Ids",
            (item, orderItem, product, measureUnit) => {
                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 2);

                item.NetWeight = Math.Round(item.NetWeight, 2);

                if (orderItem.ExchangeRateAmount.Equals(decimal.Zero)) {
                    orderItem.TotalAmountLocal =
                        orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);
                } else {
                    orderItem.TotalAmountLocal =
                        decimal.Round(
                            orderItem.PricePerItem * orderItem.ExchangeRateAmount * Convert.ToDecimal(item.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    orderItem.TotalAmount = decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);
                }

                item.OrderItem = orderItem;

                packLists.First(p => p.Id.Equals(item.TaxFreePackListId)).TaxFreePackListOrderItems.Add(item);

                return item;
            },
            props
        );

        return packLists;
    }

    public IEnumerable<TaxFreePackList> GetAllSent() {
        IEnumerable<TaxFreePackList> packLists =
            _connection.Query<TaxFreePackList, User, Organization, Client, TaxFreePackList>(
                "SELECT * " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "WHERE [TaxFreePackList].Deleted = 0 " +
                "AND [TaxFreePackList].IsSent = 1 " +
                "AND [TaxFreePackList].IsFromSale = 0 " +
                "AND [TaxFreePackList].SupplyOrderUkraineID IS NULL " +
                "ORDER BY [TaxFreePackList].ID DESC",
                (packList, responsible, organization, client) => {
                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.Client = client;

                    return packList;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        if (!packLists.Any()) return packLists;

        object props = new { Ids = packLists.Select(p => p.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItem] " +
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
            "WHERE [SupplyOrderUkraineCartItem].TaxFreePackListID IN @Ids",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                if (!item.TaxFreePackListId.HasValue) return item;

                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                packLists.First(p => p.Id.Equals(item.TaxFreePackListId.Value)).SupplyOrderUkraineCartItems.Add(item);

                return item;
            },
            props
        );

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
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [TaxFree].StathamPassportID " +
            "WHERE [TaxFree].Deleted = 0 " +
            "AND [TaxFree].TaxFreePackListID IN @Ids";

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
            StathamPassport stathamPassport = (StathamPassport)objects[12];

            TaxFreePackList packListFromList = packLists.First(p => p.Id.Equals(taxFree.TaxFreePackListId));

            if (!packListFromList.TaxFrees.Any(t => t.Id.Equals(taxFree.Id))) {
                if (taxFreeItem != null) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

                    taxFreeItem.SupplyOrderUkraineCartItem = item;

                    taxFree.TaxFreeItems.Add(taxFreeItem);
                }

                if (taxFreeDocument != null) taxFree.TaxFreeDocuments.Add(taxFreeDocument);

                taxFree.Statham = statham;
                taxFree.StathamPassport = stathamPassport;
                taxFree.StathamCar = stathamCar;
                taxFree.Responsible = taxFreeResponsible;

//                        if (taxFree.DateOfPrint.HasValue) {
//                            taxFree.DateOfPrint = TimeZoneInfo.ConvertTimeFromUtc(taxFree.DateOfPrint.Value, current);
//                        }
//
//                        if (taxFree.DateOfIssue.HasValue) {
//                            taxFree.DateOfIssue = TimeZoneInfo.ConvertTimeFromUtc(taxFree.DateOfIssue.Value, current);
//                        }
//
//                        if (taxFree.DateOfStathamPayment.HasValue) {
//                            taxFree.DateOfStathamPayment = TimeZoneInfo.ConvertTimeFromUtc(taxFree.DateOfStathamPayment.Value, current);
//                        }
//
//                        if (taxFree.DateOfTabulation.HasValue) {
//                            taxFree.DateOfTabulation = TimeZoneInfo.ConvertTimeFromUtc(taxFree.DateOfTabulation.Value, current);
//                        }
//
//                        if (taxFree.FormedDate.HasValue) {
//                            taxFree.FormedDate = TimeZoneInfo.ConvertTimeFromUtc(taxFree.FormedDate.Value, current);
//                        }
//
//                        if (taxFree.SelectedDate.HasValue) {
//                            taxFree.SelectedDate = TimeZoneInfo.ConvertTimeFromUtc(taxFree.SelectedDate.Value, current);
//                        }
//
//                        if (taxFree.ReturnedDate.HasValue) {
//                            taxFree.ReturnedDate = TimeZoneInfo.ConvertTimeFromUtc(taxFree.ReturnedDate.Value, current);
//                        }
//
//                        if (taxFree.ClosedDate.HasValue) {
//                            taxFree.ClosedDate = TimeZoneInfo.ConvertTimeFromUtc(taxFree.ClosedDate.Value, current);
//                        }
//
//                        if (taxFree.CanceledDate.HasValue) {
//                            taxFree.CanceledDate = TimeZoneInfo.ConvertTimeFromUtc(taxFree.CanceledDate.Value, current);
//                        }
//
//                        taxFree.Created = TimeZoneInfo.ConvertTimeFromUtc(taxFree.Created, current);
//                        taxFree.Updated = TimeZoneInfo.ConvertTimeFromUtc(taxFree.Updated, current);

                packListFromList.TaxFrees.Add(taxFree);
            } else {
                TaxFree fromList = packListFromList.TaxFrees.First(t => t.Id.Equals(taxFree.Id));

                if (taxFreeItem != null && !fromList.TaxFreeItems.Any(i => i.Id.Equals(taxFreeItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

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
            props
        );

        return packLists;
    }

    public IEnumerable<TaxFreePackList> GetAllFiltered(DateTime from, DateTime to, long limit, long offset) {
        IEnumerable<TaxFreePackList> packLists =
            _connection.Query<TaxFreePackList, User, Organization, SupplyOrderUkraine, Client, ClientAgreement, Agreement, TaxFreePackList>(
                "WITH [Search_CTE] " +
                "AS (" +
                "SELECT [TaxFreePackList].ID " +
                ", ROW_NUMBER() OVER(ORDER BY [TaxFreePackList].ID DESC) AS [RowNumber] " +
                "FROM [TaxFreePackList] " +
                "WHERE [TaxFreePackList].Deleted = 0 " +
                "AND [TaxFreePackList].FromDate >= @From " +
                "AND [TaxFreePackList].FromDate <= @To " +
                ") " +
                "SELECT [TaxFreePackList].* " +
                ", (" +
                "SELECT COUNT(1) " +
                "FROM [TaxFree] " +
                "WHERE [TaxFree].Deleted = 0 " +
                "AND [TaxFree].TaxFreePackListID = [TaxFreePackList].ID " +
                ") AS [TaxFreesCount] " +
                ", [Responsible].* " +
                ", [Organization].* " +
                ", [SupplyOrderUkraine].* " +
                ", [Client].* " +
                ", [ClientAgreement].* " +
                ", [Agreement].* " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [TaxFreePackList].ID IN (" +
                "SELECT [Search_CTE].ID " +
                "FROM [Search_CTE] " +
                "WHERE [Search_CTE].RowNumber > @Offset " +
                "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [TaxFreePackList].ID DESC",
                (packList, responsible, organization, orderUkraine, client, clientAgreement, agreement) => {
                    if (clientAgreement != null) clientAgreement.Agreement = agreement;

                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.SupplyOrderUkraine = orderUkraine;
                    packList.Client = client;
                    packList.ClientAgreement = clientAgreement;

                    return packList;
                },
                new {
                    From = from,
                    To = to,
                    Limit = limit,
                    Offset = offset,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );

        return packLists;
    }

    public IEnumerable<TaxFreePackList> GetAllFilteredForPrintDocument(DateTime from, DateTime to) {
        IEnumerable<TaxFreePackList> packLists =
            _connection.Query<TaxFreePackList, User, Organization, SupplyOrderUkraine, Client, ClientAgreement, Agreement, TaxFreePackList>(
                "SELECT [TaxFreePackList].* " +
                ", CASE " +
                "WHEN [TaxFreePackList].[IsSent] = 1 " +
                "THEN N'DocumentStatusDone' " +
                "ELSE N'Не Проведений' " +
                "END AS [Status] " +
                ", ( " +
                "SELECT COUNT(1) " +
                "FROM [TaxFree] " +
                "WHERE [TaxFree].Deleted = 0 " +
                "AND [TaxFree].TaxFreePackListID = [TaxFreePackList].ID " +
                ") AS [TaxFreesCount] " +
                ", [Responsible].* " +
                ", [Organization].* " +
                ", [SupplyOrderUkraine].* " +
                ", [Client].* " +
                ", [ClientAgreement].* " +
                ", [Agreement].* " +
                "FROM [TaxFreePackList] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [TaxFreePackList].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [TaxFreePackList].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [TaxFreePackList].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [TaxFreePackList].Deleted = 0 " +
                "AND [TaxFreePackList].FromDate >= @From " +
                "AND [TaxFreePackList].FromDate <= @To " +
                "ORDER BY [TaxFreePackList].ID DESC ",
                (packList, responsible, organization, orderUkraine, client, clientAgreement, agreement) => {
                    if (clientAgreement != null) clientAgreement.Agreement = agreement;

                    packList.Responsible = responsible;
                    packList.Organization = organization;
                    packList.SupplyOrderUkraine = orderUkraine;
                    packList.Client = client;
                    packList.ClientAgreement = clientAgreement;

                    return packList;
                },
                new {
                    From = from,
                    To = to,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );

        return packLists;
    }

    private decimal GetPlnExchangeRate(DateTime fromDate) {
        Currency pln =
            _exchangeRateConnection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'pln'"
            ).SingleOrDefault();

        if (pln == null) return 1m;

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
            ).FirstOrDefault();

        return exchangeRate?.Amount ?? 1m;
    }
}