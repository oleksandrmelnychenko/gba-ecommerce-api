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
using GBA.Domain.Entities.Clients.OrganizationClients;
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
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SadRepository : ISadRepository {
    private readonly IDbConnection _connection;

    private readonly IDbConnection _exchangeRateConnection;

    public SadRepository(IDbConnection connection, IDbConnection exchangeRateConnection) {
        _connection = connection;

        _exchangeRateConnection = exchangeRateConnection;
    }

    public long Add(Sad sad) {
        return _connection.Query<long>(
            "INSERT INTO [Sad] (Number, Comment, IsSend, StathamId, StathamCarId, ResponsibleId, OrganizationId, FromDate, MarginAmount, OrganizationClientId, " +
            "OrganizationClientAgreementId, IsFromSale, SadType, ClientId, StathamPassportId, ClientAgreementId, VatPercent, Updated) " +
            "VALUES (@Number, @Comment, @IsSend, @StathamId, @StathamCarId, @ResponsibleId, @OrganizationId, @FromDate, @MarginAmount, @OrganizationClientId, " +
            "@OrganizationClientAgreementId, @IsFromSale, @SadType, @ClientId, @StathamPassportId, @ClientAgreementId, @VatPercent, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            sad
        ).Single();
    }

    public void Update(Sad sad) {
        _connection.Execute(
            "UPDATE [Sad] " +
            "SET Comment = @Comment, StathamId = @StathamId, StathamCarId = @StathamCarId, ResponsibleId = @ResponsibleId, OrganizationId = @OrganizationId, " +
            "FromDate = @FromDate, IsSend = @IsSend, SupplyOrderUkraineId = @SupplyOrderUkraineId, MarginAmount = @MarginAmount, OrganizationClientId = @OrganizationClientId, " +
            "OrganizationClientAgreementId = @OrganizationClientAgreementId, ClientId = @ClientId, StathamPassportId = @StathamPassportId, " +
            "ClientAgreementId = @ClientAgreementId, VatPercent = @VatPercent, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            sad
        );
    }

    public void Delete(long id) {
        _connection.Execute(
            "UPDATE [Sad] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public Sad GetLastRecord() {
        return _connection.Query<Sad>(
            "SELECT TOP(1) * " +
            "FROM [Sad] " +
            "WHERE [Sad].Deleted = 0 " +
            "ORDER BY [Sad].ID DESC"
        ).SingleOrDefault();
    }

    public Sad GetById(long id) {
        Type[] sadTypes = {
            typeof(Sad),
            typeof(User),
            typeof(Organization),
            typeof(Statham),
            typeof(StathamCar),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Client),
            typeof(StathamPassport),
            typeof(OrganizationClientAgreement),
            typeof(ClientAgreement),
            typeof(Agreement)
        };

        Sad toReturn = null;

        Func<object[], Sad> sadMapper = objects => {
            Sad sad = (Sad)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            Statham statham = (Statham)objects[3];
            StathamCar stathamCar = (StathamCar)objects[4];
            OrganizationClient organizationClient = (OrganizationClient)objects[5];
            OrganizationClientAgreement agreement = (OrganizationClientAgreement)objects[6];
            Client client = (Client)objects[7];
            StathamPassport stathamPassport = (StathamPassport)objects[8];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[9];
            ClientAgreement clientAgreement = (ClientAgreement)objects[10];
            Agreement clientAgreementAgreement = (Agreement)objects[11];

            if (toReturn == null) {
                if (organizationClientAgreement != null) organizationClient.OrganizationClientAgreements.Add(organizationClientAgreement);

                if (clientAgreement != null) clientAgreement.Agreement = clientAgreementAgreement;

                sad.Responsible = responsible;
                sad.Organization = organization;
                sad.Statham = statham;
                sad.StathamPassport = stathamPassport;
                sad.StathamCar = stathamCar;
                sad.OrganizationClient = organizationClient;
                sad.OrganizationClientAgreement = agreement;
                sad.Client = client;
                sad.ClientAgreement = clientAgreement;

//                    sad.FromDate = TimeZoneInfo.ConvertTimeFromUtc(sad.FromDate, current);

                toReturn = sad;
            } else if (organizationClientAgreement != null) {
                toReturn.OrganizationClient.OrganizationClientAgreements.Add(organizationClientAgreement);
            }

            return sad;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [Sad].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [Sad].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [Sad].StathamCarID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [Sad].ClientID " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [Sad].StathamPassportID " +
            "LEFT JOIN [OrganizationClientAgreement] AS [NonDeletedAgreement] " +
            "ON [NonDeletedAgreement].OrganizationClientID = [OrganizationClient].ID " +
            "AND [NonDeletedAgreement].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sad].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].Id = [ClientAgreement].AgreementID " +
            "WHERE [Sad].ID = @Id",
            sadTypes,
            sadMapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

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
                "WHERE Sale.SadID = @Id " +
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

//                                orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(orderItem.Created, current);

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

//                                orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(orderItem.Created, current);

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

//                            if (sale.ChangedToInvoice.HasValue) {
//                                sale.ChangedToInvoice = TimeZoneInfo.ConvertTimeFromUtc(sale.ChangedToInvoice.Value, current);
//                            }
//
//                            sale.Created = TimeZoneInfo.ConvertTimeFromUtc(sale.Created, current);

                    toReturn.Sales.Add(sale);
                }

                return sale;
            };

            var props = new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

            _connection.Query(sqlExpression, types, mapper, props);

            Type[] itemTypes = {
                typeof(SadItem),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(SadDocument),
                typeof(Client)
            };

            Func<object[], SadItem> itemMapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                OrderItem item = (OrderItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                SadDocument document = (SadDocument)objects[4];
                Client supplier = (Client)objects[5];

                if (sadItem != null && !toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.OrderItem = item;

                    decimal exchangeRate =
                        !item.ExchangeRateAmount.Equals(decimal.Zero)
                            ? item.ExchangeRateAmount
                            : 1m; //GetPlnExchangeRate(toReturn.FromDate.AddDays(-1));

                    sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3);
                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    sadItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountWithMargin =
                        decimal.Round(sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100, 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountLocal =
                        decimal.Round((sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100) * exchangeRate, 4, MidpointRounding.AwayFromZero);

                    sadItem.TotalVatAmount =
                        decimal.Round(
                            decimal.Round(sadItem.UnitPrice * exchangeRate * Convert.ToDecimal(sadItem.Qty), 4, MidpointRounding.AwayFromZero)
                            * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    sadItem.TotalVatAmountWithMargin =
                        decimal.Round(
                            sadItem.TotalAmountLocal * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    toReturn.TotalQty += sadItem.Qty;

                    toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + sadItem.TotalNetWeight, 3);

                    toReturn.TotalAmount =
                        decimal.Round(toReturn.TotalAmount + sadItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountLocal =
                        decimal.Round(toReturn.TotalAmountLocal + sadItem.TotalAmountLocal, 4, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountWithMargin =
                        decimal.Round(toReturn.TotalAmountWithMargin + sadItem.TotalAmountWithMargin, 2, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmount =
                        decimal.Round(toReturn.TotalVatAmount + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmountWithMargin =
                        decimal.Round(toReturn.TotalVatAmountWithMargin + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.SadItems.Add(sadItem);
                }

                if (document != null && !toReturn.SadDocuments.Any(d => d.Id.Equals(document.Id))) toReturn.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [OrderItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [SadDocument].* " +
                ", [Supplier].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SadItem].OrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [OrderItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                itemTypes,
                itemMapper,
                props
            );

            Type[] palletTypes = {
                typeof(SadPallet),
                typeof(SadPalletType),
                typeof(SadPalletItem),
                typeof(SadItem),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Client)
            };

            Func<object[], SadItem> palletMapper = objects => {
                SadPallet sadPallet = (SadPallet)objects[0];
                SadPalletType sadPalletType = (SadPalletType)objects[1];
                SadPalletItem sadPalletItem = (SadPalletItem)objects[2];
                SadItem sadItem = (SadItem)objects[3];
                OrderItem item = (OrderItem)objects[4];
                Product product = (Product)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];
                Client supplier = (Client)objects[7];

                if (!toReturn.SadPallets.Any(p => p.Id.Equals(sadPallet.Id))) {
                    sadPallet.TotalGrossWeight = sadPalletType.Weight;

                    if (sadPalletItem != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        sadItem.Supplier = supplier;
                        sadItem.OrderItem = item;

                        //sadItem.NetWeight = item.NetWeight;
                        //sadItem.UnitPrice = item.UnitPrice;

                        sadPalletItem.SadItem = sadItem;

                        sadPalletItem.TotalNetWeight =
                            Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                        sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                        sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                        sadPalletItem.TotalAmount =
                            decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                        sadPalletItem.TotalAmountLocal =
                            decimal.Round(
                                sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        sadPallet.TotalAmount =
                            decimal.Round(sadPallet.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        sadPallet.TotalAmountLocal =
                            decimal.Round(sadPallet.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                        sadPallet.TotalNetWeight =
                            Math.Round(sadPallet.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.TotalGrossWeight =
                            Math.Round(sadPallet.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.SadPalletItems.Add(sadPalletItem);
                    }

                    sadPallet.SadPalletType = sadPalletType;

                    toReturn.SadPallets.Add(sadPallet);
                } else {
                    SadPallet fromList = toReturn.SadPallets.First(p => p.Id.Equals(sadPallet.Id));

                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.OrderItem = item;

                    //sadItem.NetWeight = item.NetWeight;
                    //sadItem.UnitPrice = item.UnitPrice;

                    sadPalletItem.SadItem = sadItem;

                    sadPalletItem.TotalNetWeight =
                        Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                    sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    sadPalletItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadPalletItem.TotalAmountLocal =
                        decimal.Round(sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty), 2,
                            MidpointRounding.AwayFromZero);

                    fromList.TotalAmount =
                        decimal.Round(fromList.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    fromList.TotalAmountLocal =
                        decimal.Round(fromList.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                    fromList.TotalNetWeight =
                        Math.Round(fromList.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.TotalGrossWeight =
                        Math.Round(fromList.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.SadPalletItems.Add(sadPalletItem);
                }

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadPallet].* " +
                ", [SadPalletType].* " +
                ", [SadPalletItem].* " +
                ", [SadItem].* " +
                ", [OrderItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [Supplier].* " +
                "FROM [SadPallet] " +
                "LEFT JOIN [SadPalletType] " +
                "ON [SadPalletType].ID = [SadPallet].SadPalletTypeID " +
                "LEFT JOIN [SadPalletItem] " +
                "ON [SadPalletItem].SadPalletID = [SadPallet].ID " +
                "AND [SadPalletItem].Deleted = 0 " +
                "LEFT JOIN [SadItem] " +
                "ON [SadPalletItem].SadItemID = [SadItem].ID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SadItem].OrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [OrderItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "WHERE [SadPallet].SadID = @Id " +
                "AND [SadPallet].Deleted = 0",
                palletTypes,
                palletMapper,
                props
            );

            if (toReturn.SadPallets.Any()) {
                foreach (SadPallet pallet in toReturn.SadPallets) {
                    double gross = pallet.SadPalletType.Weight / pallet.SadPalletItems.Sum(i => i.Qty);

                    foreach (SadPalletItem item in pallet.SadPalletItems)
                        item.TotalGrossWeight =
                            Math.Round(item.TotalGrossWeight + gross * item.Qty, 2);
                }

                toReturn.TotalGrossWeight = Math.Round(toReturn.SadPallets.Sum(p => p.TotalGrossWeight), 3);
            } else {
                toReturn.TotalGrossWeight = toReturn.TotalNetWeight;
            }
        } else {
            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(SadDocument),
                typeof(Client)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                SadDocument document = (SadDocument)objects[7];
                Client supplier = (Client)objects[8];

                if (sadItem != null && !toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;
                    item.Supplier = supplier;

                    sadItem.NetWeight = item.NetWeight;
                    sadItem.UnitPrice = item.UnitPrice;

                    sadItem.SupplyOrderUkraineCartItem = item;
                    sadItem.Supplier = supplier;

                    sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3);
                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    decimal exchangeRate = GetPlnExchangeRate(toReturn.FromDate.AddDays(-1));

                    sadItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountWithMargin =
                        decimal.Round(sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100, 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountLocal =
                        decimal.Round((sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100) * exchangeRate, 4, MidpointRounding.AwayFromZero);

                    sadItem.TotalVatAmount =
                        decimal.Round(
                            decimal.Round(sadItem.UnitPrice * exchangeRate * Convert.ToDecimal(sadItem.Qty), 4, MidpointRounding.AwayFromZero)
                            * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    sadItem.TotalVatAmountWithMargin =
                        decimal.Round(
                            sadItem.TotalAmountLocal * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    toReturn.TotalQty += sadItem.Qty;

                    toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + sadItem.TotalNetWeight, 3);

                    toReturn.TotalAmount =
                        decimal.Round(toReturn.TotalAmount + sadItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountLocal =
                        decimal.Round(toReturn.TotalAmountLocal + sadItem.TotalAmountLocal, 4, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountWithMargin =
                        decimal.Round(toReturn.TotalAmountWithMargin + sadItem.TotalAmountWithMargin, 2, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmount =
                        decimal.Round(toReturn.TotalVatAmount + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmountWithMargin =
                        decimal.Round(toReturn.TotalVatAmountWithMargin + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.SadItems.Add(sadItem);
                }

                if (document != null && !toReturn.SadDocuments.Any(d => d.Id.Equals(document.Id))) toReturn.SadDocuments.Add(document);

                return sadItem;
            };

            var props = new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [CreatedBy].* " +
                ", [UpdatedBy].* " +
                ", [Responsible].* " +
                ", [SadDocument].* " +
                ", [Supplier].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraineCartItem].SupplierID " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                props
            );

            Type[] palletTypes = {
                typeof(SadPallet),
                typeof(SadPalletType),
                typeof(SadPalletItem),
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Client)
            };

            Func<object[], SadItem> palletMapper = objects => {
                SadPallet sadPallet = (SadPallet)objects[0];
                SadPalletType sadPalletType = (SadPalletType)objects[1];
                SadPalletItem sadPalletItem = (SadPalletItem)objects[2];
                SadItem sadItem = (SadItem)objects[3];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[4];
                Product product = (Product)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];
                Client supplier = (Client)objects[7];

                if (!toReturn.SadPallets.Any(p => p.Id.Equals(sadPallet.Id))) {
                    sadPallet.TotalGrossWeight = sadPalletType.Weight;

                    if (sadPalletItem != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        sadItem.Supplier = supplier;
                        sadItem.SupplyOrderUkraineCartItem = item;

                        sadItem.NetWeight = item.NetWeight;
                        sadItem.UnitPrice = item.UnitPrice;

                        sadPalletItem.SadItem = sadItem;

                        sadPalletItem.TotalNetWeight =
                            Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                        sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                        sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                        sadPalletItem.TotalAmount =
                            decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                        sadPalletItem.TotalAmountLocal =
                            decimal.Round(
                                sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        sadPallet.TotalAmount =
                            decimal.Round(sadPallet.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        sadPallet.TotalAmountLocal =
                            decimal.Round(sadPallet.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                        sadPallet.TotalNetWeight =
                            Math.Round(sadPallet.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.TotalGrossWeight =
                            Math.Round(sadPallet.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.SadPalletItems.Add(sadPalletItem);
                    }

                    sadPallet.SadPalletType = sadPalletType;

                    toReturn.SadPallets.Add(sadPallet);
                } else {
                    SadPallet fromList = toReturn.SadPallets.First(p => p.Id.Equals(sadPallet.Id));

                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.SupplyOrderUkraineCartItem = item;

                    sadItem.NetWeight = item.NetWeight;
                    sadItem.UnitPrice = item.UnitPrice;

                    sadPalletItem.SadItem = sadItem;

                    sadPalletItem.TotalNetWeight =
                        Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                    sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    sadPalletItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadPalletItem.TotalAmountLocal =
                        decimal.Round(sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty), 2,
                            MidpointRounding.AwayFromZero);

                    fromList.TotalAmount =
                        decimal.Round(fromList.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    fromList.TotalAmountLocal =
                        decimal.Round(fromList.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                    fromList.TotalNetWeight =
                        Math.Round(fromList.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.TotalGrossWeight =
                        Math.Round(fromList.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.SadPalletItems.Add(sadPalletItem);
                }

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadPallet].* " +
                ", [SadPalletType].* " +
                ", [SadPalletItem].* " +
                ", [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [Supplier].* " +
                "FROM [SadPallet] " +
                "LEFT JOIN [SadPalletType] " +
                "ON [SadPalletType].ID = [SadPallet].SadPalletTypeID " +
                "LEFT JOIN [SadPalletItem] " +
                "ON [SadPalletItem].SadPalletID = [SadPallet].ID " +
                "AND [SadPalletItem].Deleted = 0 " +
                "LEFT JOIN [SadItem] " +
                "ON [SadPalletItem].SadItemID = [SadItem].ID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
                "LEFT JOIN [Product] " +
                "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "WHERE [SadPallet].SadID = @Id " +
                "AND [SadPallet].Deleted = 0",
                palletTypes,
                palletMapper,
                props
            );

            if (toReturn.SadPallets.Any()) {
                foreach (SadPallet pallet in toReturn.SadPallets) {
                    double gross = pallet.SadPalletType.Weight / pallet.SadPalletItems.Sum(i => i.Qty);

                    foreach (SadPalletItem item in pallet.SadPalletItems)
                        item.TotalGrossWeight =
                            Math.Round(item.TotalGrossWeight + gross * item.Qty, 2);
                }

                toReturn.TotalGrossWeight = Math.Round(toReturn.SadPallets.Sum(p => p.TotalGrossWeight), 3);
            } else {
                toReturn.TotalGrossWeight = toReturn.TotalNetWeight;
            }
        }

        if (toReturn.TotalAmount > 0 && toReturn.TotalNetWeight > 0)
            toReturn.SadCoefficient =
                Math.Round(toReturn.TotalNetWeight / Convert.ToDouble(toReturn.TotalAmount), 4, MidpointRounding.AwayFromZero);

        return toReturn;
    }

    public Sad GetByIdWithoutIncludes(long id) {
        return _connection.Query<Sad>(
            "SELECT * " +
            "FROM [Sad] " +
            "WHERE [Sad].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public Sad GetByIdForConsignment(long id) {
        Sad toReturn = null;

        Type[] types = {
            typeof(Sad),
            typeof(Organization),
            typeof(SadItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation),
            typeof(ProductAvailability),
            typeof(ConsignmentItem)
        };

        Func<object[], Sad> mapper = objects => {
            Sad sad = (Sad)objects[0];
            Organization organization = (Organization)objects[1];
            SadItem sadItem = (SadItem)objects[2];
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[3];
            SupplyOrderUkraineCartItemReservation cartItemReservation = (SupplyOrderUkraineCartItemReservation)objects[4];
            ProductAvailability availability = (ProductAvailability)objects[5];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[6];

            if (toReturn == null) {
                sad.Organization = organization;

                toReturn = sad;
            }

            if (sadItem == null) return sad;

            if (toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                sadItem = toReturn.SadItems.First(i => i.Id.Equals(sadItem.Id));
            } else {
                sadItem.SupplyOrderUkraineCartItem = cartItem;

                toReturn.SadItems.Add(sadItem);
            }

            if (cartItemReservation == null) return sad;

            cartItemReservation.ProductAvailability = availability;
            cartItemReservation.ConsignmentItem = consignmentItem;

            sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

            return sad;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SadID = [Sad].ID " +
            "AND [SadItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID = [ProductAvailability].ID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyOrderUkraineCartItemReservation].ConsignmentItemID " +
            "WHERE [Sad].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public Sad GetByIdForConsignmentFromSale(long id) {
        Sad toReturn = null;

        Type[] types = {
            typeof(Sad),
            typeof(Organization),
            typeof(SadItem)
        };

        Func<object[], Sad> mapper = objects => {
            Sad sad = (Sad)objects[0];
            Organization organization = (Organization)objects[1];
            SadItem sadItem = (SadItem)objects[2];

            if (toReturn == null) {
                sad.Organization = organization;

                toReturn = sad;
            }

            if (sadItem == null) return sad;

            toReturn.SadItems.Add(sadItem);

            return sad;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SadID = [Sad].ID " +
            "AND [SadItem].Deleted = 0 " +
            "WHERE [Sad].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public Sad GetByNetId(Guid netId) {
        Type[] sadTypes = {
            typeof(Sad),
            typeof(User),
            typeof(Organization),
            typeof(Statham),
            typeof(StathamCar),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Client),
            typeof(StathamPassport),
            typeof(OrganizationClientAgreement),
            typeof(ClientAgreement),
            typeof(Agreement)
        };

        Sad toReturn = null;

        Func<object[], Sad> sadMapper = objects => {
            Sad sad = (Sad)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            Statham statham = (Statham)objects[3];
            StathamCar stathamCar = (StathamCar)objects[4];
            OrganizationClient organizationClient = (OrganizationClient)objects[5];
            OrganizationClientAgreement agreement = (OrganizationClientAgreement)objects[6];
            Client client = (Client)objects[7];
            StathamPassport stathamPassport = (StathamPassport)objects[8];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[9];
            ClientAgreement clientAgreement = (ClientAgreement)objects[10];
            Agreement clientAgreementAgreement = (Agreement)objects[11];

            if (toReturn == null) {
                if (organizationClientAgreement != null) organizationClient.OrganizationClientAgreements.Add(organizationClientAgreement);

                if (clientAgreement != null) clientAgreement.Agreement = clientAgreementAgreement;

                sad.Responsible = responsible;
                sad.Organization = organization;
                sad.Statham = statham;
                sad.StathamPassport = stathamPassport;
                sad.StathamCar = stathamCar;
                sad.OrganizationClient = organizationClient;
                sad.OrganizationClientAgreement = agreement;
                sad.Client = client;
                sad.ClientAgreement = clientAgreement;

                toReturn = sad;
            } else if (organizationClientAgreement != null) {
                toReturn.OrganizationClient.OrganizationClientAgreements.Add(organizationClientAgreement);
            }

            return sad;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [Sad].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Statham] " +
            "ON [Statham].ID = [Sad].StathamID " +
            "LEFT JOIN [StathamCar] " +
            "ON [StathamCar].ID = [Sad].StathamCarID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [Sad].ClientID " +
            "LEFT JOIN [StathamPassport] " +
            "ON [StathamPassport].ID = [Sad].StathamPassportID " +
            "LEFT JOIN [OrganizationClientAgreement] AS [NonDeletedAgreement] " +
            "ON [NonDeletedAgreement].OrganizationClientID = [OrganizationClient].ID " +
            "AND [NonDeletedAgreement].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sad].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].Id = [ClientAgreement].AgreementID " +
            "WHERE [Sad].NetUID = @NetId",
            sadTypes,
            sadMapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

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
                "WHEN [OrderItem].[IsFromReSale] = 1 " +
                "THEN dbo.GetCalculatedProductPriceWithShares_ReSale(Product.NetUID, ClientAgreement.NetUID, @Culture, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, @WithVat, [OrderItem].[ID]) " +
                "END) AS [CurrentPrice] " +
                ", (CASE " +
                "WHEN [OrderItem].[IsFromReSale] = 1 " +
                "THEN dbo.GetCalculatedProductLocalPriceWithShares_ReSale(Product.NetUID, ClientAgreement.NetUID, @Culture, [OrderItem].[ID]) " +
                "ELSE dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, ClientAgreement.NetUID, @Culture, @WithVat, [OrderItem].[ID]) " +
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
                "WHERE Sale.SadID = @Id " +
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

            Type[] itemTypes = {
                typeof(SadItem),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(SadDocument),
                typeof(Client)
            };

            Func<object[], SadItem> itemMapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                OrderItem item = (OrderItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                SadDocument document = (SadDocument)objects[4];
                Client supplier = (Client)objects[5];

                if (sadItem != null && !toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.OrderItem = item;

                    decimal exchangeRate =
                        !item.ExchangeRateAmount.Equals(decimal.Zero)
                            ? item.ExchangeRateAmount
                            : 1m; //GetPlnExchangeRate(toReturn.FromDate.AddDays(-1));

                    sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3);
                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    sadItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountWithMargin =
                        decimal.Round(sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100, 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountLocal =
                        decimal.Round((sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100) * exchangeRate, 4, MidpointRounding.AwayFromZero);

                    sadItem.TotalVatAmount =
                        decimal.Round(
                            decimal.Round(sadItem.UnitPrice * exchangeRate * Convert.ToDecimal(sadItem.Qty), 4, MidpointRounding.AwayFromZero)
                            * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    sadItem.TotalVatAmountWithMargin =
                        decimal.Round(
                            sadItem.TotalAmountLocal * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    toReturn.TotalQty += sadItem.Qty;

                    toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + sadItem.TotalNetWeight, 3);

                    toReturn.TotalAmount =
                        decimal.Round(toReturn.TotalAmount + sadItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountLocal =
                        decimal.Round(toReturn.TotalAmountLocal + sadItem.TotalAmountLocal, 4, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountWithMargin =
                        decimal.Round(toReturn.TotalAmountWithMargin + sadItem.TotalAmountWithMargin, 2, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmount =
                        decimal.Round(toReturn.TotalVatAmount + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmountWithMargin =
                        decimal.Round(toReturn.TotalVatAmountWithMargin + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.SadItems.Add(sadItem);
                }

                if (document != null && !toReturn.SadDocuments.Any(d => d.Id.Equals(document.Id))) toReturn.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [OrderItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [SadDocument].* " +
                ", [Supplier].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SadItem].OrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [OrderItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                itemTypes,
                itemMapper,
                props
            );

            Type[] palletTypes = {
                typeof(SadPallet),
                typeof(SadPalletType),
                typeof(SadPalletItem),
                typeof(SadItem),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Client)
            };

            Func<object[], SadItem> palletMapper = objects => {
                SadPallet sadPallet = (SadPallet)objects[0];
                SadPalletType sadPalletType = (SadPalletType)objects[1];
                SadPalletItem sadPalletItem = (SadPalletItem)objects[2];
                SadItem sadItem = (SadItem)objects[3];
                OrderItem item = (OrderItem)objects[4];
                Product product = (Product)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];
                Client supplier = (Client)objects[7];

                if (!toReturn.SadPallets.Any(p => p.Id.Equals(sadPallet.Id))) {
                    sadPallet.TotalGrossWeight = sadPalletType.Weight;

                    if (sadPalletItem != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        sadItem.Supplier = supplier;
                        sadItem.OrderItem = item;

                        sadPalletItem.SadItem = sadItem;

                        sadPalletItem.TotalNetWeight =
                            Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                        sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                        sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                        sadPalletItem.TotalAmount =
                            decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                        sadPalletItem.TotalAmountLocal =
                            decimal.Round(
                                sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        sadPallet.TotalAmount =
                            decimal.Round(sadPallet.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        sadPallet.TotalAmountLocal =
                            decimal.Round(sadPallet.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                        sadPallet.TotalNetWeight =
                            Math.Round(sadPallet.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.TotalGrossWeight =
                            Math.Round(sadPallet.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.SadPalletItems.Add(sadPalletItem);
                    }

                    sadPallet.SadPalletType = sadPalletType;

                    toReturn.SadPallets.Add(sadPallet);
                } else {
                    SadPallet fromList = toReturn.SadPallets.First(p => p.Id.Equals(sadPallet.Id));

                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.OrderItem = item;

                    sadPalletItem.SadItem = sadItem;

                    sadPalletItem.TotalNetWeight =
                        Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                    sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    sadPalletItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadPalletItem.TotalAmountLocal =
                        decimal.Round(sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty), 2,
                            MidpointRounding.AwayFromZero);

                    fromList.TotalAmount =
                        decimal.Round(fromList.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    fromList.TotalAmountLocal =
                        decimal.Round(fromList.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                    fromList.TotalNetWeight =
                        Math.Round(fromList.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.TotalGrossWeight =
                        Math.Round(fromList.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.SadPalletItems.Add(sadPalletItem);
                }

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadPallet].* " +
                ", [SadPalletType].* " +
                ", [SadPalletItem].* " +
                ", [SadItem].* " +
                ", [OrderItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [Supplier].* " +
                "FROM [SadPallet] " +
                "LEFT JOIN [SadPalletType] " +
                "ON [SadPalletType].ID = [SadPallet].SadPalletTypeID " +
                "LEFT JOIN [SadPalletItem] " +
                "ON [SadPalletItem].SadPalletID = [SadPallet].ID " +
                "AND [SadPalletItem].Deleted = 0 " +
                "LEFT JOIN [SadItem] " +
                "ON [SadPalletItem].SadItemID = [SadItem].ID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SadItem].OrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [OrderItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "WHERE [SadPallet].SadID = @Id " +
                "AND [SadPallet].Deleted = 0",
                palletTypes,
                palletMapper,
                props
            );

            if (toReturn.SadPallets.Any()) {
                foreach (SadPallet pallet in toReturn.SadPallets) {
                    double gross = pallet.SadPalletType.Weight / pallet.SadPalletItems.Sum(i => i.Qty);

                    foreach (SadPalletItem item in pallet.SadPalletItems)
                        item.TotalGrossWeight =
                            Math.Round(item.TotalGrossWeight + gross * item.Qty, 2);
                }

                toReturn.TotalGrossWeight = Math.Round(toReturn.SadPallets.Sum(p => p.TotalGrossWeight), 3);
            } else {
                toReturn.TotalGrossWeight = toReturn.TotalNetWeight;
            }
        } else {
            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(SadDocument),
                typeof(Client)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                SadDocument document = (SadDocument)objects[7];
                Client supplier = (Client)objects[8];

                if (sadItem != null && !toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;
                    item.Supplier = supplier;

                    sadItem.NetWeight = item.NetWeight;
                    sadItem.UnitPrice = item.UnitPrice;

                    sadItem.SupplyOrderUkraineCartItem = item;
                    sadItem.Supplier = supplier;

                    sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3);
                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    decimal exchangeRate = GetPlnExchangeRate(toReturn.FromDate.AddDays(-1));

                    sadItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountWithMargin =
                        decimal.Round(sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100, 2, MidpointRounding.AwayFromZero);
                    sadItem.TotalAmountLocal =
                        decimal.Round((sadItem.TotalAmount + sadItem.TotalAmount * toReturn.MarginAmount / 100) * exchangeRate, 4, MidpointRounding.AwayFromZero);

                    sadItem.TotalVatAmount =
                        decimal.Round(
                            decimal.Round(sadItem.UnitPrice * exchangeRate * Convert.ToDecimal(sadItem.Qty), 4, MidpointRounding.AwayFromZero)
                            * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    sadItem.TotalVatAmountWithMargin =
                        decimal.Round(
                            sadItem.TotalAmountLocal * toReturn.VatPercent / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    toReturn.TotalQty += sadItem.Qty;

                    toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + sadItem.TotalNetWeight, 3);

                    toReturn.TotalAmount =
                        decimal.Round(toReturn.TotalAmount + sadItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountLocal =
                        decimal.Round(toReturn.TotalAmountLocal + sadItem.TotalAmountLocal, 4, MidpointRounding.AwayFromZero);
                    toReturn.TotalAmountWithMargin =
                        decimal.Round(toReturn.TotalAmountWithMargin + sadItem.TotalAmountWithMargin, 2, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmount =
                        decimal.Round(toReturn.TotalVatAmount + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.TotalVatAmountWithMargin =
                        decimal.Round(toReturn.TotalVatAmountWithMargin + sadItem.TotalVatAmount, 4, MidpointRounding.AwayFromZero);

                    toReturn.SadItems.Add(sadItem);
                }

                if (document != null && !toReturn.SadDocuments.Any(d => d.Id.Equals(document.Id))) toReturn.SadDocuments.Add(document);

                return sadItem;
            };

            var props = new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [CreatedBy].* " +
                ", [UpdatedBy].* " +
                ", [Responsible].* " +
                ", [SadDocument].* " +
                ", [Supplier].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraineCartItem].SupplierID " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                props
            );

            Type[] palletTypes = {
                typeof(SadPallet),
                typeof(SadPalletType),
                typeof(SadPalletItem),
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Client)
            };

            Func<object[], SadItem> palletMapper = objects => {
                SadPallet sadPallet = (SadPallet)objects[0];
                SadPalletType sadPalletType = (SadPalletType)objects[1];
                SadPalletItem sadPalletItem = (SadPalletItem)objects[2];
                SadItem sadItem = (SadItem)objects[3];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[4];
                Product product = (Product)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];
                Client supplier = (Client)objects[7];

                if (!toReturn.SadPallets.Any(p => p.Id.Equals(sadPallet.Id))) {
                    sadPallet.TotalGrossWeight = sadPalletType.Weight;

                    if (sadPalletItem != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        sadItem.Supplier = supplier;
                        sadItem.SupplyOrderUkraineCartItem = item;

                        sadItem.NetWeight = item.NetWeight;
                        sadItem.UnitPrice = item.UnitPrice;

                        sadPalletItem.SadItem = sadItem;

                        sadPalletItem.TotalNetWeight =
                            Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                        sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                        sadItem.NetWeight = Math.Round(sadItem.NetWeight, 2);

                        sadPalletItem.TotalAmount =
                            decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                        sadPalletItem.TotalAmountLocal =
                            decimal.Round(
                                sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        sadPallet.TotalAmount =
                            decimal.Round(sadPallet.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        sadPallet.TotalAmountLocal =
                            decimal.Round(sadPallet.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                        sadPallet.TotalNetWeight =
                            Math.Round(sadPallet.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.TotalGrossWeight =
                            Math.Round(sadPallet.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                        sadPallet.SadPalletItems.Add(sadPalletItem);
                    }

                    sadPallet.SadPalletType = sadPalletType;

                    toReturn.SadPallets.Add(sadPallet);
                } else {
                    SadPallet fromList = toReturn.SadPallets.First(p => p.Id.Equals(sadPallet.Id));

                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.SupplyOrderUkraineCartItem = item;

                    sadItem.NetWeight = item.NetWeight;
                    sadItem.UnitPrice = item.UnitPrice;

                    sadPalletItem.SadItem = sadItem;

                    sadPalletItem.TotalNetWeight =
                        Math.Round(sadItem.NetWeight * sadPalletItem.Qty, 3);
                    sadPalletItem.TotalGrossWeight = sadPalletItem.TotalNetWeight;

                    sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);

                    sadPalletItem.TotalAmount =
                        decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty), 2, MidpointRounding.AwayFromZero);
                    sadPalletItem.TotalAmountLocal =
                        decimal.Round(sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty), 2,
                            MidpointRounding.AwayFromZero);

                    fromList.TotalAmount =
                        decimal.Round(fromList.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    fromList.TotalAmountLocal =
                        decimal.Round(fromList.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                    fromList.TotalNetWeight =
                        Math.Round(fromList.TotalNetWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.TotalGrossWeight =
                        Math.Round(fromList.TotalGrossWeight + sadPalletItem.TotalNetWeight, 3);

                    fromList.SadPalletItems.Add(sadPalletItem);
                }

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadPallet].* " +
                ", [SadPalletType].* " +
                ", [SadPalletItem].* " +
                ", [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [Supplier].* " +
                "FROM [SadPallet] " +
                "LEFT JOIN [SadPalletType] " +
                "ON [SadPalletType].ID = [SadPallet].SadPalletTypeID " +
                "LEFT JOIN [SadPalletItem] " +
                "ON [SadPalletItem].SadPalletID = [SadPallet].ID " +
                "AND [SadPalletItem].Deleted = 0 " +
                "LEFT JOIN [SadItem] " +
                "ON [SadPalletItem].SadItemID = [SadItem].ID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
                "LEFT JOIN [Product] " +
                "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "WHERE [SadPallet].SadID = @Id " +
                "AND [SadPallet].Deleted = 0",
                palletTypes,
                palletMapper,
                props
            );

            if (toReturn.SadPallets.Any()) {
                foreach (SadPallet pallet in toReturn.SadPallets) {
                    double gross = pallet.SadPalletType.Weight / pallet.SadPalletItems.Sum(i => i.Qty);

                    foreach (SadPalletItem item in pallet.SadPalletItems)
                        item.TotalGrossWeight =
                            Math.Round(item.TotalGrossWeight + gross * item.Qty, 2);
                }

                toReturn.TotalGrossWeight = Math.Round(toReturn.SadPallets.Sum(p => p.TotalGrossWeight), 3);
            } else {
                toReturn.TotalGrossWeight = toReturn.TotalNetWeight;
            }
        }

        if (toReturn.TotalAmount > 0 && toReturn.TotalNetWeight > 0)
            toReturn.SadCoefficient =
                Math.Round(toReturn.TotalNetWeight / Convert.ToDouble(toReturn.TotalAmount), 4, MidpointRounding.AwayFromZero);

        return toReturn;
    }

    public Sad GetByNetIdForConsignment(Guid netId) {
        Sad toReturn = null;

        Type[] types = {
            typeof(Sad),
            typeof(Organization),
            typeof(SadItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation),
            typeof(ProductAvailability),
            typeof(ConsignmentItem)
        };

        Func<object[], Sad> mapper = objects => {
            Sad sad = (Sad)objects[0];
            Organization organization = (Organization)objects[1];
            SadItem sadItem = (SadItem)objects[2];
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[3];
            SupplyOrderUkraineCartItemReservation cartItemReservation = (SupplyOrderUkraineCartItemReservation)objects[4];
            ProductAvailability availability = (ProductAvailability)objects[5];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[6];

            if (toReturn == null) {
                sad.Organization = organization;

                toReturn = sad;
            }

            if (sadItem == null) return sad;

            if (toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                sadItem = toReturn.SadItems.First(i => i.Id.Equals(sadItem.Id));
            } else {
                sadItem.SupplyOrderUkraineCartItem = cartItem;

                toReturn.SadItems.Add(sadItem);
            }

            if (cartItemReservation == null) return sad;

            cartItemReservation.ProductAvailability = availability;
            cartItemReservation.ConsignmentItem = consignmentItem;

            sadItem.SupplyOrderUkraineCartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

            return sad;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SadID = [Sad].ID " +
            "AND [SadItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID = [ProductAvailability].ID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyOrderUkraineCartItemReservation].ConsignmentItemID " +
            "WHERE [Sad].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public Sad GetByNetIdWithItems(Guid netId) {
        Sad toReturn = null;

        _connection.Query<Sad, Organization, SadItem, OrderItem, SupplyOrderUkraineCartItem, Sad>(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SadID = [Sad].ID " +
            "AND [SadItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SadItem].OrderItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "WHERE [Sad].NetUID = @NetId",
            (sad, organization, sadItem, orderItem, cartItem) => {
                if (toReturn == null) {
                    sad.Organization = organization;

                    toReturn = sad;
                }

                if (sadItem == null) return sad;

                sadItem.OrderItem = orderItem;
                sadItem.SupplyOrderUkraineCartItem = cartItem;

                toReturn.SadItems.Add(sadItem);

                return sad;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public Sad GetByNetIdWithProductSpecification(Guid netId) {
        Sad toReturn =
            _connection.Query<Sad, User, Organization, Statham, StathamCar, OrganizationClient, OrganizationClientAgreement, Sad>(
                    "SELECT * " +
                    "FROM [Sad] " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [Sad].ResponsibleID " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [Sad].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [Statham] " +
                    "ON [Statham].ID = [Sad].StathamID " +
                    "LEFT JOIN [StathamCar] " +
                    "ON [StathamCar].ID = [Sad].StathamCarID " +
                    "LEFT JOIN [OrganizationClient] " +
                    "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                    "LEFT JOIN [OrganizationClientAgreement] " +
                    "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                    "WHERE [Sad].NetUID = @NetId",
                    (sad, responsible, organization, statham, stathamCar, organizationClient, agreement) => {
                        sad.Responsible = responsible;
                        sad.Organization = organization;
                        sad.Statham = statham;
                        sad.StathamCar = stathamCar;
                        sad.OrganizationClient = organizationClient;
                        sad.OrganizationClientAgreement = agreement;

                        return sad;
                    },
                    new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        if (toReturn.IsFromSale) {
            List<long> productIds = new();

            Type[] types = {
                typeof(SadItem),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(SadDocument)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                OrderItem item = (OrderItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                SadDocument document = (SadDocument)objects[4];

                if (sadItem != null && !toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.OrderItem = item;

                    toReturn.SadItems.Add(sadItem);

                    productIds.Add(product.Id);
                }

                if (document != null && !toReturn.SadDocuments.Any(d => d.Id.Equals(document.Id))) toReturn.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [OrderItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [SadDocument].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SadItem].OrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [OrderItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

            _connection.Query<ProductSpecification, User, ProductSpecification>(
                "SELECT [ProductSpecification].* " +
                ", [User].* " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductSpecification].AddedByID " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].ProductID IN @ProductIds " +
                "AND [OrderProductSpecification].SadID = @SadId " +
                "AND [ProductSpecification].Locale = @Culture",
                (specification, user) => {
                    specification.AddedBy = user;

                    toReturn
                        .SadItems
                        .First(i => i.OrderItem.ProductId.Equals(specification.ProductId))
                        .OrderItem
                        .Product
                        .ProductSpecifications
                        .Add(specification);

                    return specification;
                },
                new { ProductIds = productIds, SadId = toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        } else {
            List<long> productIds = new();

            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(SadDocument)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                SadDocument document = (SadDocument)objects[7];

                if (sadItem != null && !toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

                    sadItem.SupplyOrderUkraineCartItem = item;

                    toReturn.SadItems.Add(sadItem);

                    productIds.Add(product.Id);
                }

                if (document != null && !toReturn.SadDocuments.Any(d => d.Id.Equals(document.Id))) toReturn.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [CreatedBy].* " +
                ", [UpdatedBy].* " +
                ", [Responsible].* " +
                ", [SadDocument].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

            _connection.Query<ProductSpecification, User, ProductSpecification>(
                "SELECT [ProductSpecification].* " +
                ", [User].* " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductSpecification].AddedByID " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].ProductID IN @ProductIds " +
                "AND [OrderProductSpecification].SadID = @SadId " +
                "AND [ProductSpecification].Locale = @Culture",
                (specification, user) => {
                    specification.AddedBy = user;

                    toReturn
                        .SadItems
                        .First(i => i.SupplyOrderUkraineCartItem.ProductId.Equals(specification.ProductId))
                        .SupplyOrderUkraineCartItem
                        .Product
                        .ProductSpecifications
                        .Add(specification);

                    return specification;
                },
                new { ProductIds = productIds, SadId = toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return toReturn;
    }

    public Sad GetByNetIdWithProductSpecification(Guid netId, string specificationLocale) {
        Sad toReturn = null;

        _connection.Query<Sad, SadItem, OrderItem, SupplyOrderUkraineCartItem, Product, ProductSpecification, Sad>(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SadID = [Sad].ID " +
            "AND [SadItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SadItem].OrderItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Product] " +
            "ON ( " +
            "[Product].ID = [OrderItem].ProductID " +
            "OR " +
            "[Product].ID = [SupplyOrderUkraineCartItem].ProductID " +
            ") " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = ( " +
            "SELECT TOP(1) [JoinSpecification].ID " +
            "FROM [ProductSpecification] AS [JoinSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [JoinSpecification].ID = [OrderProductSpecification].ProductSpecificationId " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "WHERE [JoinSpecification].Locale = @SpecificationLocale " +
            "AND [JoinSpecification].ProductID = [Product].ID " +
            "AND [OrderProductSpecification].SadId = [Sad].ID " +
            "ORDER BY [JoinSpecification].ID DESC" +
            ") " +
            "WHERE [Sad].NetUID = @NetId",
            (sad, sadItem, orderItem, cartItem, product, specification) => {
                if (toReturn == null)
                    toReturn = sad;

                if (sadItem == null) return sad;

                if (orderItem != null)
                    orderItem.Product = product;
                else
                    cartItem.Product = product;

                sadItem.ProductSpecification = specification;
                sadItem.OrderItem = orderItem;
                sadItem.SupplyOrderUkraineCartItem = cartItem;

                toReturn.SadItems.Add(sadItem);

                return sad;
            },
            new { NetId = netId, SpecificationLocale = specificationLocale }
        );

        return toReturn;
    }

    public Sad GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<Sad>(
            "SELECT * " +
            "FROM [Sad] " +
            "WHERE [Sad].NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public Sad GetByNetIdAndProductId(Guid netId, long productId) {
        Sad toReturn = null;

        _connection.Query<Sad, Organization, SadItem, OrderItem, SupplyOrderUkraineCartItem, Sad>(
            "SELECT * " +
            "FROM [Sad] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Sad].OrganizationID " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SadID = [Sad].ID " +
            "AND [SadItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SadItem].OrderItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "WHERE [Sad].NetUID = @NetId " +
            "AND (" +
            "[OrderItem].ProductID = @ProductId " +
            "OR " +
            "[SupplyOrderUkraineCartItem].ProductID = @ProductId" +
            ")",
            (sad, organization, sadItem, orderItem, cartItem) => {
                if (toReturn == null) {
                    sad.Organization = organization;

                    toReturn = sad;
                }

                sadItem.OrderItem = orderItem;
                sadItem.SupplyOrderUkraineCartItem = cartItem;

                toReturn.SadItems.Add(sadItem);

                return sad;
            },
            new { NetId = netId, ProductId = productId }
        );

        return toReturn;
    }

    public Sad GetByNetIdWithProductMovement(Guid netId) {
        Sad toReturn =
            _connection.Query<Sad, User, Organization, Statham, StathamCar, OrganizationClient, OrganizationClientAgreement, Sad>(
                    "SELECT * " +
                    "FROM [Sad] " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [Sad].ResponsibleID " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [Sad].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [Statham] " +
                    "ON [Statham].ID = [Sad].StathamID " +
                    "LEFT JOIN [StathamCar] " +
                    "ON [StathamCar].ID = [Sad].StathamCarID " +
                    "LEFT JOIN [OrganizationClient] " +
                    "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                    "LEFT JOIN [OrganizationClientAgreement] " +
                    "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                    "WHERE [Sad].NetUID = @NetId",
                    (sad, responsible, organization, statham, stathamCar, organizationClient, agreement) => {
                        sad.Responsible = responsible;
                        sad.Organization = organization;
                        sad.Statham = statham;
                        sad.StathamCar = stathamCar;
                        sad.OrganizationClient = organizationClient;
                        sad.OrganizationClientAgreement = agreement;

                        return sad;
                    },
                    new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        Type[] types = {
            typeof(SadItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User)
        };

        Func<object[], SadItem> mapper = objects => {
            SadItem sadItem = (SadItem)objects[0];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            User createdBy = (User)objects[4];
            User updatedBy = (User)objects[5];
            User responsible = (User)objects[6];

            if (toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) return sadItem;

            product.MeasureUnit = measureUnit;

            item.Product = product;
            item.CreatedBy = createdBy;
            item.UpdatedBy = updatedBy;
            item.Responsible = responsible;

            sadItem.SupplyOrderUkraineCartItem = item;

            toReturn.SadItems.Add(sadItem);

            return sadItem;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SadItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
            "WHERE [SadItem].SadID = @Id " +
            "AND [SadItem].Deleted = 0",
            types,
            mapper,
            new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public Sad GetForDocumentsExportByNetIdAndCulture(Guid netId, string culture) {
        Sad toReturn =
            _connection.Query<Sad, User, Organization, Statham, StathamCar, OrganizationClient, OrganizationClientAgreement, Sad>(
                    "SELECT * " +
                    "FROM [Sad] " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [Sad].ResponsibleID " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [Sad].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [Statham] " +
                    "ON [Statham].ID = [Sad].StathamID " +
                    "LEFT JOIN [StathamCar] " +
                    "ON [StathamCar].ID = [Sad].StathamCarID " +
                    "LEFT JOIN [OrganizationClient] " +
                    "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                    "LEFT JOIN [OrganizationClientAgreement] " +
                    "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                    "WHERE [Sad].NetUID = @NetId",
                    (sad, responsible, organization, statham, stathamCar, organizationClient, agreement) => {
                        sad.Responsible = responsible;
                        sad.Organization = organization;
                        sad.Statham = statham;
                        sad.StathamCar = stathamCar;
                        sad.OrganizationClient = organizationClient;
                        sad.OrganizationClientAgreement = agreement;

                        return sad;
                    },
                    new { NetId = netId, Culture = culture }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        Type[] types = {
            typeof(SadItem),
            typeof(SupplyOrderUkraineCartItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User),
            typeof(User)
        };

        Func<object[], SadItem> mapper = objects => {
            SadItem sadItem = (SadItem)objects[0];
            SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
            Product product = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            User createdBy = (User)objects[4];
            User updatedBy = (User)objects[5];
            User responsible = (User)objects[6];

            if (toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) return sadItem;

            product.MeasureUnit = measureUnit;

            item.Product = product;
            item.CreatedBy = createdBy;
            item.UpdatedBy = updatedBy;
            item.Responsible = responsible;

            sadItem.SupplyOrderUkraineCartItem = item;

            toReturn.SadItems.Add(sadItem);

            return sadItem;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SadItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
            "WHERE [SadItem].SadID = @Id " +
            "AND [SadItem].Deleted = 0",
            types,
            mapper,
            new { toReturn.Id, Culture = culture }
        );

        return toReturn;
    }

    public Sad GetForDocumentsExportByNetIdAndCultureWithProductSpecification(Guid netId, string culture) {
        Type[] sadTypes = {
            typeof(Sad),
            typeof(User),
            typeof(Organization),
            typeof(Statham),
            typeof(StathamCar),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Client),
            typeof(StathamPassport)
        };

        Func<object[], Sad> sadMapper = objects => {
            Sad sad = (Sad)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            Statham statham = (Statham)objects[3];
            StathamCar stathamCar = (StathamCar)objects[4];
            OrganizationClient organizationClient = (OrganizationClient)objects[5];
            OrganizationClientAgreement agreement = (OrganizationClientAgreement)objects[6];
            Client client = (Client)objects[7];
            StathamPassport stathamPassport = (StathamPassport)objects[8];

            sad.Responsible = responsible;
            sad.Organization = organization;
            sad.Statham = statham;
            sad.StathamPassport = stathamPassport;
            sad.StathamCar = stathamCar;
            sad.OrganizationClient = organizationClient;
            sad.OrganizationClientAgreement = agreement;
            sad.Client = client;

            return sad;
        };

        Sad toReturn =
            _connection.Query(
                    "SELECT * " +
                    "FROM [Sad] " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [Responsible].ID = [Sad].ResponsibleID " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [Sad].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [Statham] " +
                    "ON [Statham].ID = [Sad].StathamID " +
                    "LEFT JOIN [StathamCar] " +
                    "ON [StathamCar].ID = [Sad].StathamCarID " +
                    "LEFT JOIN [OrganizationClient] " +
                    "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                    "LEFT JOIN [OrganizationClientAgreement] " +
                    "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                    "LEFT JOIN [Client] " +
                    "ON [Client].ID = [Sad].ClientID " +
                    "LEFT JOIN [StathamPassport] " +
                    "ON [StathamPassport].ID = [Sad].StathamPassportID " +
                    "WHERE [Sad].NetUID = @NetId",
                    sadTypes,
                    sadMapper,
                    new { NetId = netId, Culture = culture }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        if (toReturn.IsFromSale) {
            List<long> productIds = new();

            Type[] types = {
                typeof(Sale),
                typeof(Order),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(ProductLocation),
                typeof(ProductPlacement),
                typeof(PackingListPackageOrderItem),
                typeof(SupplyInvoiceOrderItem),
                typeof(SupplyOrderItem),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Country),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Client),
                typeof(RegionCode)
            };

            Func<object[], Sale> mapper = objects => {
                Sale sale = (Sale)objects[0];
                Order order = (Order)objects[1];
                OrderItem orderItem = (OrderItem)objects[2];
                Product product = (Product)objects[3];
                MeasureUnit measureUnit = (MeasureUnit)objects[4];
                ProductLocation productLocation = (ProductLocation)objects[5];
                ProductPlacement productPlacement = (ProductPlacement)objects[6];
                PackingListPackageOrderItem packListItem = (PackingListPackageOrderItem)objects[7];
                Client supplier = (Client)objects[12];
                Country country = (Country)objects[13];
                ClientAgreement clientAgreement = (ClientAgreement)objects[14];
                Agreement agreement = (Agreement)objects[15];
                Client client = (Client)objects[16];
                RegionCode regionCode = (RegionCode)objects[17];

                if (!toReturn.Sales.Any(s => s.Id.Equals(sale.Id))) {
                    if (orderItem != null) {
                        if (productLocation != null) {
                            supplier.Country = country;

                            packListItem.Supplier = supplier;

                            productLocation.ProductPlacement = productPlacement;

                            orderItem.ProductLocations.Add(productLocation);
                        }

                        product.MeasureUnit = measureUnit;

                        orderItem.Product = product;

                        order.OrderItems.Add(orderItem);

                        productIds.Add(product.Id);
                    }

                    client.RegionCode = regionCode;

                    clientAgreement.Agreement = agreement;
                    clientAgreement.Client = client;

                    sale.Order = order;
                    sale.ClientAgreement = clientAgreement;

                    toReturn.Sales.Add(sale);
                } else {
                    Sale saleFromList = toReturn.Sales.First(s => s.Id.Equals(sale.Id));

                    if (!saleFromList.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                        if (productLocation != null && packListItem != null) {
                            supplier.Country = country;

                            packListItem.Supplier = supplier;

                            productLocation.ProductPlacement = productPlacement;

                            orderItem.ProductLocations.Add(productLocation);
                        }

                        product.MeasureUnit = measureUnit;

                        orderItem.Product = product;

                        saleFromList.Order.OrderItems.Add(orderItem);

                        productIds.Add(product.Id);
                    } else {
                        supplier.Country = country;

                        packListItem.Supplier = supplier;

                        productLocation.ProductPlacement = productPlacement;

                        saleFromList.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).ProductLocations.Add(productLocation);
                    }
                }

                return sale;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [Sale] " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "AND [OrderItem].Deleted = 0 " +
                "AND [OrderItem].Qty > 0 " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [OrderItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].OrderItemID = [OrderItem].ID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductPlacement].PackingListPackageOrderItemID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Supplier].CountryID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [ClientAgreement].ClientID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "WHERE [Sale].SadID = @Id",
                types,
                mapper,
                new { toReturn.Id, Culture = culture }
            );

            _connection.Query<ProductSpecification, User, ProductSpecification>(
                "SELECT [ProductSpecification].* " +
                ", [User].* " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductSpecification].AddedByID " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].ProductID IN @ProductIds " +
                "AND [OrderProductSpecification].SadID = @SadId " +
                "AND [ProductSpecification].Locale = N'uk'",
                (specification, user) => {
                    specification.AddedBy = user;

                    foreach (Sale sale in toReturn.Sales.Where(s => s.Order.OrderItems.Any(i => i.ProductId.Equals(specification.ProductId))))
                    foreach (OrderItem item in sale.Order.OrderItems.Where(i => i.ProductId.Equals(specification.ProductId)))
                        item.UkProductSpecification = specification;

                    return specification;
                },
                new { ProductIds = productIds, SadId = toReturn.Id }
            );

            _connection.Query<ProductSpecification, User, ProductSpecification>(
                "SELECT [ProductSpecification].* " +
                ", [User].* " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductSpecification].AddedByID " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].ProductID IN @ProductIds " +
                "AND [OrderProductSpecification].SadID = @SadId " +
                "AND [ProductSpecification].Locale = N'pl'",
                (specification, user) => {
                    specification.AddedBy = user;

                    foreach (Sale sale in toReturn.Sales.Where(s => s.Order.OrderItems.Any(i => i.ProductId.Equals(specification.ProductId))))
                    foreach (OrderItem item in sale.Order.OrderItems.Where(i => i.ProductId.Equals(specification.ProductId))) {
                        item.ProductSpecification = specification;

                        item.Product.ProductSpecifications.Add(specification);
                    }

                    return specification;
                },
                new { ProductIds = productIds, SadId = toReturn.Id }
            );
        } else {
            List<long> productIds = new();

            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(PackingListPackageOrderItem),
                typeof(Client),
                typeof(Country)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
                Client supplier = (Client)objects[8];
                Country country = (Country)objects[9];

                if (toReturn.SadItems.Any(i => i.Id.Equals(sadItem.Id))) return sadItem;

                product.MeasureUnit = measureUnit;

                if (supplier != null) supplier.Country = country;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;
                item.Supplier = supplier;

                sadItem.SupplyOrderUkraineCartItem = item;

                sadItem.NetWeight = Math.Round(packageOrderItem?.NetWeight ?? item.NetWeight, 3, MidpointRounding.AwayFromZero);
                sadItem.GrossWeight = Math.Round(packageOrderItem?.GrossWeight ?? item.NetWeight, 3, MidpointRounding.AwayFromZero);

                sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3, MidpointRounding.AwayFromZero);
                sadItem.TotalGrossWeight = Math.Round(sadItem.GrossWeight * sadItem.Qty, 3, MidpointRounding.AwayFromZero);

                if (toReturn.MarginAmount != 0m)
                    item.UnitPrice += item.UnitPrice * toReturn.MarginAmount / 100;

                toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight + sadItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                toReturn.TotalGrossWeight = Math.Round(toReturn.TotalGrossWeight + sadItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

                toReturn.SadItems.Add(sadItem);

                productIds.Add(product.Id);

                return sadItem;
            };

            var props = new { toReturn.Id, Culture = culture };

            _connection.Query(
                "SELECT * " +
                "FROM [SadItem] " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [SupplyOrderUkraineCartItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrderUkraineCartItem].SupplierID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "WHERE [SadItem].SadID = @Id " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                props
            );

            _connection.Query<ProductSpecification, User, ProductSpecification>(
                "SELECT [ProductSpecification].* " +
                ", [User].* " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductSpecification].AddedByID " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].ProductID IN @ProductIds " +
                "AND [OrderProductSpecification].SadID = @SadId " +
                "AND [ProductSpecification].Locale = N'uk'",
                (specification, user) => {
                    specification.AddedBy = user;

                    SadItem item =
                        toReturn
                            .SadItems
                            .First(i => i.SupplyOrderUkraineCartItem.ProductId.Equals(specification.ProductId));

                    item.UkProductSpecification = specification;

                    return specification;
                },
                new { ProductIds = productIds, SadId = toReturn.Id }
            );

            _connection.Query<ProductSpecification, User, ProductSpecification>(
                "SELECT [ProductSpecification].* " +
                ", [User].* " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductSpecification].AddedByID " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].ProductID IN @ProductIds " +
                "AND [OrderProductSpecification].SadID = @SadId " +
                "AND [ProductSpecification].Locale = N'pl'",
                (specification, user) => {
                    specification.AddedBy = user;

                    SadItem item =
                        toReturn
                            .SadItems
                            .First(i => i.SupplyOrderUkraineCartItem.ProductId.Equals(specification.ProductId));

                    item.ProductSpecification = specification;

                    item
                        .SupplyOrderUkraineCartItem
                        .Product
                        .ProductSpecifications
                        .Add(specification);

                    return specification;
                },
                new { ProductIds = productIds, SadId = toReturn.Id }
            );

            Type[] palletTypes = {
                typeof(SadPallet),
                typeof(SadPalletType),
                typeof(SadPalletItem),
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Client),
                typeof(PackingListPackageOrderItem)
            };

            Func<object[], SadItem> palletMapper = objects => {
                SadPallet sadPallet = (SadPallet)objects[0];
                SadPalletType sadPalletType = (SadPalletType)objects[1];
                SadPalletItem sadPalletItem = (SadPalletItem)objects[2];
                SadItem sadItem = (SadItem)objects[3];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[4];
                Product product = (Product)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];
                Client supplier = (Client)objects[7];
                PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[8];

                if (!toReturn.SadPallets.Any(p => p.Id.Equals(sadPallet.Id))) {
                    sadPallet.TotalGrossWeight = sadPalletType.Weight;

                    if (sadPalletItem != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        sadItem.Supplier = supplier;
                        sadItem.SupplyOrderUkraineCartItem = item;

                        sadItem.UnitPrice = item.UnitPrice;

                        sadItem.NetWeight = Math.Round(packageOrderItem?.NetWeight ?? item.NetWeight, 3, MidpointRounding.AwayFromZero);
                        sadItem.GrossWeight = Math.Round(packageOrderItem?.GrossWeight ?? item.NetWeight, 3, MidpointRounding.AwayFromZero);

                        sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3, MidpointRounding.AwayFromZero);
                        sadItem.TotalGrossWeight = Math.Round(sadItem.GrossWeight * sadItem.Qty, 3, MidpointRounding.AwayFromZero);

                        sadPalletItem.SadItem = sadItem;

                        sadPalletItem.TotalNetWeight = sadItem.TotalNetWeight;
                        sadPalletItem.TotalGrossWeight = sadItem.TotalGrossWeight;

                        sadPalletItem.TotalAmount =
                            decimal.Round(
                                sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );
                        sadPalletItem.TotalAmountLocal =
                            decimal.Round(
                                sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        sadPallet.TotalAmount =
                            decimal.Round(sadPallet.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        sadPallet.TotalAmountLocal =
                            decimal.Round(sadPallet.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                        sadPallet.TotalNetWeight = Math.Round(sadPallet.TotalNetWeight + sadItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                        sadPallet.TotalGrossWeight = Math.Round(sadPallet.TotalGrossWeight + sadItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

                        sadPallet.SadPalletItems.Add(sadPalletItem);
                    }

                    sadPallet.SadPalletType = sadPalletType;

                    toReturn.SadPallets.Add(sadPallet);
                } else {
                    SadPallet fromList = toReturn.SadPallets.First(p => p.Id.Equals(sadPallet.Id));

                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    sadItem.Supplier = supplier;
                    sadItem.SupplyOrderUkraineCartItem = item;

                    sadItem.UnitPrice = item.UnitPrice;

                    sadItem.NetWeight = Math.Round(packageOrderItem?.NetWeight ?? item.NetWeight, 3, MidpointRounding.AwayFromZero);
                    sadItem.GrossWeight = Math.Round(packageOrderItem?.GrossWeight ?? item.NetWeight, 3, MidpointRounding.AwayFromZero);

                    sadItem.TotalNetWeight = Math.Round(sadItem.NetWeight * sadItem.Qty, 3, MidpointRounding.AwayFromZero);
                    sadItem.TotalGrossWeight = Math.Round(sadItem.GrossWeight * sadItem.Qty, 3, MidpointRounding.AwayFromZero);

                    sadPalletItem.SadItem = sadItem;

                    sadPalletItem.TotalNetWeight = sadItem.TotalNetWeight;
                    sadPalletItem.TotalGrossWeight = sadItem.TotalGrossWeight;

                    sadPalletItem.TotalAmount =
                        decimal.Round(
                            sadItem.UnitPrice * Convert.ToDecimal(sadPalletItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    sadPalletItem.TotalAmountLocal =
                        decimal.Round(
                            sadItem.UnitPrice * GetPlnExchangeRate(toReturn.FromDate.AddDays(-1)) * Convert.ToDecimal(sadPalletItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    fromList.TotalAmount =
                        decimal.Round(fromList.TotalAmount + sadPalletItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    fromList.TotalAmountLocal =
                        decimal.Round(fromList.TotalAmountLocal + sadPalletItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);

                    fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + sadItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                    fromList.TotalGrossWeight = Math.Round(fromList.TotalGrossWeight + sadItem.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

                    fromList.SadPalletItems.Add(sadPalletItem);
                }

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadPallet].* " +
                ", [SadPalletType].* " +
                ", [SadPalletItem].* " +
                ", [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [Supplier].* " +
                ", [PackingListPackageOrderItem].* " +
                "FROM [SadPallet] " +
                "LEFT JOIN [SadPalletType] " +
                "ON [SadPalletType].ID = [SadPallet].SadPalletTypeID " +
                "LEFT JOIN [SadPalletItem] " +
                "ON [SadPalletItem].SadPalletID = [SadPallet].ID " +
                "AND [SadPalletItem].Deleted = 0 " +
                "LEFT JOIN [SadItem] " +
                "ON [SadPalletItem].SadItemID = [SadItem].ID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
                "LEFT JOIN [Product] " +
                "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SadItem].SupplierID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [SupplyOrderUkraineCartItem].PackingListPackageOrderItemID " +
                "WHERE [SadPallet].SadID = @Id " +
                "AND [SadPallet].Deleted = 0",
                palletTypes,
                palletMapper,
                props
            );

            if (!toReturn.SadPallets.Any()) return toReturn;

            foreach (SadPallet pallet in toReturn.SadPallets) {
                double gross = pallet.SadPalletType.Weight / pallet.SadPalletItems.Sum(i => i.Qty);

                foreach (SadPalletItem item in pallet.SadPalletItems)
                    item.TotalGrossWeight =
                        Math.Round(item.TotalGrossWeight + gross * item.Qty, 3, MidpointRounding.AwayFromZero);
            }

            toReturn.TotalGrossWeight = Math.Round(toReturn.SadPallets.Sum(p => p.TotalGrossWeight), 3, MidpointRounding.AwayFromZero);
        }

        return toReturn;
    }

    public IEnumerable<Sad> GetAllSent() {
        IEnumerable<Sad> sads =
            _connection.Query<Sad, User, Organization, Statham, StathamCar, OrganizationClient, OrganizationClientAgreement, Sad>(
                "SELECT * " +
                "FROM [Sad] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [Sad].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [Sad].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Statham] " +
                "ON [Statham].ID = [Sad].StathamID " +
                "LEFT JOIN [StathamCar] " +
                "ON [StathamCar].ID = [Sad].StathamCarID " +
                "LEFT JOIN [OrganizationClient] " +
                "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                "LEFT JOIN [OrganizationClientAgreement] " +
                "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                "WHERE [Sad].Deleted = 0 " +
                "AND [Sad].IsSend = 1 " +
                "AND [Sad].IsFromSale = 0 " +
                "AND [Sad].SupplyOrderUkraineID IS NULL " +
                "ORDER BY [Sad].FromDate DESC",
                (sad, responsible, organization, statham, stathamCar, organizationClient, agreement) => {
                    sad.Responsible = responsible;
                    sad.Organization = organization;
                    sad.Statham = statham;
                    sad.StathamCar = stathamCar;
                    sad.OrganizationClient = organizationClient;
                    sad.OrganizationClientAgreement = agreement;

                    return sad;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        if (sads.Any()) {
            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(SadDocument)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                SadDocument document = (SadDocument)objects[7];

                Sad fromList = sads.First(s => s.Id.Equals(sadItem.SadId));

                if (sadItem != null && !fromList.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

                    sadItem.SupplyOrderUkraineCartItem = item;

                    fromList.SadItems.Add(sadItem);
                }

                if (document != null && !fromList.SadDocuments.Any(d => d.Id.Equals(document.Id))) fromList.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [CreatedBy].* " +
                ", [UpdatedBy].* " +
                ", [Responsible].* " +
                ", [SadDocument].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "WHERE [SadItem].SadID IN @Ids " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                new { Ids = sads.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return sads;
    }

    public IEnumerable<Sad> GetAllNotSent(SadType type) {
        IEnumerable<Sad> sads =
            _connection.Query<Sad, User, Organization, Statham, StathamCar, OrganizationClient, OrganizationClientAgreement, Sad>(
                "SELECT * " +
                "FROM [Sad] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [Sad].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [Sad].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Statham] " +
                "ON [Statham].ID = [Sad].StathamID " +
                "LEFT JOIN [StathamCar] " +
                "ON [StathamCar].ID = [Sad].StathamCarID " +
                "LEFT JOIN [OrganizationClient] " +
                "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                "LEFT JOIN [OrganizationClientAgreement] " +
                "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                "WHERE [Sad].Deleted = 0 " +
                "AND [Sad].IsSend = 0 " +
                "AND [Sad].IsFromSale = 0 " +
                "AND [Sad].SadType = @Type " +
                "ORDER BY [Sad].FromDate DESC",
                (sad, responsible, organization, statham, stathamCar, organizationClient, agreement) => {
                    sad.Responsible = responsible;
                    sad.Organization = organization;
                    sad.Statham = statham;
                    sad.StathamCar = stathamCar;
                    sad.OrganizationClient = organizationClient;
                    sad.OrganizationClientAgreement = agreement;

                    return sad;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Type = type }
            );

        if (sads.Any()) {
            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(SadDocument)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                SadDocument document = (SadDocument)objects[7];

                Sad fromList = sads.First(s => s.Id.Equals(sadItem.SadId));

                if (sadItem != null && !fromList.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;
                    item.CreatedBy = createdBy;
                    item.UpdatedBy = updatedBy;
                    item.Responsible = responsible;

                    sadItem.SupplyOrderUkraineCartItem = item;

                    fromList.SadItems.Add(sadItem);
                }

                if (document != null && !fromList.SadDocuments.Any(d => d.Id.Equals(document.Id))) fromList.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [CreatedBy].* " +
                ", [UpdatedBy].* " +
                ", [Responsible].* " +
                ", [SadDocument].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "WHERE [SadItem].SadID IN @Ids " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                new { Ids = sads.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return sads;
    }

    public IEnumerable<Sad> GetAllNotSentFromSale(SadType type) {
        IEnumerable<Sad> sads =
            _connection.Query<Sad, User, Organization, Statham, StathamCar, OrganizationClient, OrganizationClientAgreement, Sad>(
                "SELECT * " +
                "FROM [Sad] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [Sad].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [Sad].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Statham] " +
                "ON [Statham].ID = [Sad].StathamID " +
                "LEFT JOIN [StathamCar] " +
                "ON [StathamCar].ID = [Sad].StathamCarID " +
                "LEFT JOIN [OrganizationClient] " +
                "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                "LEFT JOIN [OrganizationClientAgreement] " +
                "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                "WHERE [Sad].Deleted = 0 " +
                "AND [Sad].IsSend = 0 " +
                "AND [Sad].IsFromSale = 1 " +
                "AND [Sad].SadType = @Type " +
                "ORDER BY [Sad].FromDate DESC",
                (sad, responsible, organization, statham, stathamCar, organizationClient, agreement) => {
                    sad.Responsible = responsible;
                    sad.Organization = organization;
                    sad.Statham = statham;
                    sad.StathamCar = stathamCar;
                    sad.OrganizationClient = organizationClient;
                    sad.OrganizationClientAgreement = agreement;

                    return sad;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Type = type }
            );

        if (!sads.Any()) return sads;

        string salesSqlExpression = "SELECT Sale.* " +
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

        salesSqlExpression += ",[Product].HasAnalogue " +
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
                              "WHERE Sale.SadID IN @Ids " +
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

            if (!sale.SadId.HasValue) return sale;

            Sad sadFromList = sads.First(s => s.Id.Equals(sale.SadId.Value));

            if (sadFromList.Sales.Any(s => s.Id.Equals(sale.Id))) {
                Sale saleFromList = sadFromList.Sales.First(c => c.Id.Equals(sale.Id));

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

//                            orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(orderItem.Created, current);

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

//                            orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(orderItem.Created, current);

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

//                        if (sale.ChangedToInvoice.HasValue) {
//                            sale.ChangedToInvoice = TimeZoneInfo.ConvertTimeFromUtc(sale.ChangedToInvoice.Value, current);
//                        }
//
//                        sale.Created = TimeZoneInfo.ConvertTimeFromUtc(sale.Created, current);

                sadFromList.Sales.Add(sale);
            }

            return sale;
        };

        var props = new {
            Ids = sads.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(salesSqlExpression, salesTypes, salesMapper, props);

        return sads;
    }

    public IEnumerable<Sad> GetAllFiltered(DateTime from, DateTime to, long limit, long offset) {
        Type[] sadTypes = {
            typeof(Sad),
            typeof(User),
            typeof(Organization),
            typeof(Statham),
            typeof(StathamCar),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Client),
            typeof(StathamPassport),
            typeof(ClientAgreement),
            typeof(Agreement)
        };

        Func<object[], Sad> sadMapper = objects => {
            Sad sad = (Sad)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            Statham statham = (Statham)objects[3];
            StathamCar stathamCar = (StathamCar)objects[4];
            OrganizationClient organizationClient = (OrganizationClient)objects[5];
            OrganizationClientAgreement agreement = (OrganizationClientAgreement)objects[6];
            Client client = (Client)objects[7];
            StathamPassport stathamPassport = (StathamPassport)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement clientAgreementAgreement = (Agreement)objects[10];

            if (clientAgreement != null) clientAgreement.Agreement = clientAgreementAgreement;

            sad.Responsible = responsible;
            sad.Organization = organization;
            sad.Statham = statham;
            sad.StathamPassport = stathamPassport;
            sad.StathamCar = stathamCar;
            sad.OrganizationClient = organizationClient;
            sad.OrganizationClientAgreement = agreement;
            sad.Client = client;
            sad.ClientAgreement = clientAgreement;

//                sad.FromDate = TimeZoneInfo.ConvertTimeFromUtc(sad.FromDate, current);

            return sad;
        };

        IEnumerable<Sad> sads =
            _connection.Query(
                ";WITH [Search_CTE] " +
                "AS (" +
                "SELECT [Sad].ID AS [ID] " +
                ", ROW_NUMBER() OVER(ORDER BY [Sad].FromDate DESC) AS [RowNumber]" +
                "FROM [Sad] " +
                "WHERE [Sad].FromDate >= @From " +
                "AND [Sad].FromDate <= @To " +
                "AND [Sad].Deleted = 0" +
                ")" +
                "SELECT * " +
                "FROM [Sad] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [Sad].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [Sad].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Statham] " +
                "ON [Statham].ID = [Sad].StathamID " +
                "LEFT JOIN [StathamCar] " +
                "ON [StathamCar].ID = [Sad].StathamCarID " +
                "LEFT JOIN [OrganizationClient] " +
                "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
                "LEFT JOIN [OrganizationClientAgreement] " +
                "ON [OrganizationClientAgreement].ID = [Sad].OrganizationClientAgreementID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [Sad].ClientID " +
                "LEFT JOIN [StathamPassport] " +
                "ON [StathamPassport].ID = [Sad].StathamPassportID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [Sad].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].Id = [ClientAgreement].AgreementID " +
                "WHERE [Sad].ID IN (" +
                "SELECT [Search_CTE].ID " +
                "FROM [Search_CTE] " +
                "WHERE [Search_CTE].RowNumber > @Offset " +
                "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [Sad].FromDate DESC",
                sadTypes,
                sadMapper,
                new {
                    From = from,
                    To = to,
                    Limit = limit,
                    Offset = offset,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });

        if (sads.Any()) {
            string salesSqlExpression = "SELECT Sale.* " +
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

            salesSqlExpression += ",[Product].HasAnalogue " +
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
                                  "WHERE Sale.SadID IN @Ids " +
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

                if (!sale.SadId.HasValue) return sale;

                Sad sadFromList = sads.First(s => s.Id.Equals(sale.SadId.Value));

                if (sadFromList.Sales.Any(s => s.Id.Equals(sale.Id))) {
                    Sale saleFromList = sadFromList.Sales.First(c => c.Id.Equals(sale.Id));

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

//                            orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(orderItem.Created, current);

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

//                            orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(orderItem.Created, current);

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

//                        if (sale.ChangedToInvoice.HasValue) {
//                            sale.ChangedToInvoice = TimeZoneInfo.ConvertTimeFromUtc(sale.ChangedToInvoice.Value, current);
//                        }
//
//                        sale.Created = TimeZoneInfo.ConvertTimeFromUtc(sale.Created, current);

                    sadFromList.Sales.Add(sale);
                }

                return sale;
            };

            var props = new { Ids = sads.Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

            _connection.Query(salesSqlExpression, salesTypes, salesMapper, props);

            Type[] types = {
                typeof(SadItem),
                typeof(SupplyOrderUkraineCartItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(User),
                typeof(User),
                typeof(User),
                typeof(SadDocument),
                typeof(Client),
                typeof(OrderItem),
                typeof(Product),
                typeof(MeasureUnit),
                typeof(Client)
            };

            Func<object[], SadItem> mapper = objects => {
                SadItem sadItem = (SadItem)objects[0];
                SupplyOrderUkraineCartItem item = (SupplyOrderUkraineCartItem)objects[1];
                Product product = (Product)objects[2];
                MeasureUnit measureUnit = (MeasureUnit)objects[3];
                User createdBy = (User)objects[4];
                User updatedBy = (User)objects[5];
                User responsible = (User)objects[6];
                SadDocument document = (SadDocument)objects[7];
                Client supplier = (Client)objects[8];
                OrderItem orderItem = (OrderItem)objects[9];
                Product orderItemProduct = (Product)objects[10];
                MeasureUnit orderItemProductMeasureUnit = (MeasureUnit)objects[11];
                Client orderItemSupplier = (Client)objects[12];

                Sad fromList = sads.First(s => s.Id.Equals(sadItem.SadId));

                if (sadItem != null && !fromList.SadItems.Any(i => i.Id.Equals(sadItem.Id))) {
                    if (item != null) {
                        product.MeasureUnit = measureUnit;

                        item.Product = product;
                        item.CreatedBy = createdBy;
                        item.UpdatedBy = updatedBy;
                        item.Responsible = responsible;
                        item.Supplier = supplier;

                        sadItem.NetWeight = item.NetWeight;
                        sadItem.UnitPrice = item.UnitPrice;

                        sadItem.TotalNetWeight =
                            Math.Round(sadItem.NetWeight * sadItem.Qty, 3);
                        sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);
                        sadItem.TotalAmount =
                            decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadItem.Qty), 2, MidpointRounding.AwayFromZero);
                        sadItem.TotalAmountWithMargin =
                            decimal.Round(sadItem.TotalAmount + sadItem.TotalAmount * fromList.MarginAmount / 100, 2, MidpointRounding.AwayFromZero);
                        sadItem.TotalAmountLocal =
                            decimal.Round(
                                (sadItem.TotalAmount + sadItem.TotalAmount * fromList.MarginAmount / 100) * GetPlnExchangeRate(fromList.FromDate.AddDays(-1)),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        fromList.TotalQty += sadItem.Qty;
                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + sadItem.TotalNetWeight, 3);
                        fromList.TotalAmount =
                            decimal.Round(fromList.TotalAmount + sadItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        fromList.TotalAmountLocal =
                            decimal.Round(fromList.TotalAmountLocal + sadItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                        fromList.TotalAmountWithMargin =
                            decimal.Round(fromList.TotalAmountWithMargin + sadItem.TotalAmountWithMargin, 2, MidpointRounding.AwayFromZero);
                    }

                    if (orderItem != null) {
                        orderItemProduct.MeasureUnit = orderItemProductMeasureUnit;

                        orderItem.Product = orderItemProduct;

                        decimal exchangeRate =
                            !orderItem.ExchangeRateAmount.Equals(decimal.Zero)
                                ? orderItem.ExchangeRateAmount
                                : GetPlnExchangeRate(fromList.FromDate.AddDays(-1));

                        sadItem.TotalNetWeight =
                            Math.Round(sadItem.NetWeight * sadItem.Qty, 3);
                        sadItem.NetWeight = Math.Round(sadItem.NetWeight, 3);
                        sadItem.TotalAmount =
                            decimal.Round(sadItem.UnitPrice * Convert.ToDecimal(sadItem.Qty), 2, MidpointRounding.AwayFromZero);
                        sadItem.TotalAmountWithMargin =
                            decimal.Round(sadItem.TotalAmount + sadItem.TotalAmount * fromList.MarginAmount / 100, 2, MidpointRounding.AwayFromZero);
                        sadItem.TotalAmountLocal =
                            decimal.Round((sadItem.TotalAmount + sadItem.TotalAmount * fromList.MarginAmount / 100) * exchangeRate, 2, MidpointRounding.AwayFromZero);

                        fromList.TotalQty += sadItem.Qty;
                        fromList.TotalNetWeight = Math.Round(fromList.TotalNetWeight + sadItem.TotalNetWeight, 3);
                        fromList.TotalAmount =
                            decimal.Round(fromList.TotalAmount + sadItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                        fromList.TotalAmountLocal =
                            decimal.Round(fromList.TotalAmountLocal + sadItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                        fromList.TotalAmountWithMargin =
                            decimal.Round(fromList.TotalAmountWithMargin + sadItem.TotalAmountWithMargin, 2, MidpointRounding.AwayFromZero);
                    }

                    sadItem.SupplyOrderUkraineCartItem = item;
                    sadItem.Supplier = orderItemSupplier;
                    sadItem.OrderItem = orderItem;

                    fromList.SadItems.Add(sadItem);
                }

                if (document != null && !fromList.SadDocuments.Any(d => d.Id.Equals(document.Id))) fromList.SadDocuments.Add(document);

                return sadItem;
            };

            _connection.Query(
                "SELECT [SadItem].* " +
                ", [SupplyOrderUkraineCartItem].* " +
                ", [Product].* " +
                ", [MeasureUnit].* " +
                ", [CreatedBy].* " +
                ", [UpdatedBy].* " +
                ", [Responsible].* " +
                ", [SadDocument].* " +
                ", [Supplier].* " +
                ", [OrderItem].* " +
                ", [OrderItemProduct].* " +
                ", [OrderItemProductMeasureUnit].* " +
                ", [SadItemSupplier].* " +
                "FROM [SadItem] " +
                "LEFT JOIN [Sad] " +
                "ON [Sad].ID = [SadItem].SadID " +
                "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
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
                "LEFT JOIN [SadDocument] " +
                "ON [SadDocument].SadID = [Sad].ID " +
                "AND [SadDocument].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraineCartItem].SupplierID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ID = [SadItem].OrderItemID " +
                "LEFT JOIN [Product] AS [OrderItemProduct] " +
                "ON [OrderItem].ProductID = [OrderItemProduct].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [OrderItemProductMeasureUnit] " +
                "ON [OrderItemProductMeasureUnit].ID = [OrderItemProduct].MeasureUnitID " +
                "AND [OrderItemProductMeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [SadItemSupplier] " +
                "ON [SadItemSupplier].ID = [SadItem].SupplierID " +
                "WHERE [SadItem].SadID IN @Ids " +
                "AND [SadItem].Deleted = 0",
                types,
                mapper,
                props
            );
        }

        return sads;
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
                )
                .FirstOrDefault();

        return exchangeRate?.Amount ?? 1m;
    }
}