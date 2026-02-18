using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Supplies.SupplyOrderModels;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkraineRepository : ISupplyOrderUkraineRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkraineRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderUkraine supplyOrderUkraine) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyOrderUkraine] (FromDate, IsPlaced, Number, Comment, ResponsibleId, OrganizationId, SupplierId, ClientAgreementId, ShipmentAmount, " +
            "IsDirectFromSupplier, InvNumber, AdditionalPaymentFromDate, AdditionalAmount, AdditionalPercent, AdditionalPaymentCurrencyId, Updated, [InvDate], " +
            "[VatPercent], [ShipmentAmountLocal], [IsPartialPlaced]) " +
            "VALUES (@FromDate, @IsPlaced, @Number, @Comment, @ResponsibleId, @OrganizationId, @SupplierId, @ClientAgreementId, @ShipmentAmount, " +
            "@IsDirectFromSupplier, @InvNumber, NULL, 0.00, 0.00, NULL, GETUTCDATE(), @InvDate, @VatPercent, @ShipmentAmountLocal, @IsPartialPlaced); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrderUkraine
        ).Single();
    }

    public void UpdateIsPlaced(SupplyOrderUkraine supplyOrderUkraine) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET IsPlaced = @IsPlaced, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrderUkraine].ID = @Id",
            supplyOrderUkraine
        );
    }

    public void UpdateAdditionalPaymentFields(SupplyOrderUkraine supplyOrderUkraine) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET AdditionalAmount = @AdditionalAmount, AdditionalPercent = @AdditionalPercent, AdditionalPaymentFromDate = @AdditionalPaymentFromDate, " +
            "AdditionalPaymentCurrencyId = @AdditionalPaymentCurrencyId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            supplyOrderUkraine
        );
    }

    public void UpdateOrganization(SupplyOrderUkraine supplyOrderUkraine) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET OrganizationId = @OrganizationId, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrderUkraine].ID = @Id",
            supplyOrderUkraine
        );
    }

    public void UpdateShipmentAmount(SupplyOrderUkraine supplyOrderUkraine) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET ShipmentAmount = @ShipmentAmount, Updated = GETUTCDATE(), [ShipmentAmountLocal] = @ShipmentAmountLocal " +
            "WHERE [SupplyOrderUkraine].ID = @Id",
            supplyOrderUkraine
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public SupplyOrderUkraine GetLastRecord() {
        return _connection.Query<SupplyOrderUkraine>(
            "SELECT TOP(1) * " +
            "FROM [SupplyOrderUkraine] " +
            "WHERE [SupplyOrderUkraine].Deleted = 0 " +
            "ORDER BY [SupplyOrderUkraine].ID DESC"
        ).SingleOrDefault();
    }

    public SupplyOrderUkraine GetById(long id) {
        Type[] types = {
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(ActReconciliation),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(TaxFreePackList),
            typeof(Sad),
            typeof(Currency),
            typeof(Currency)
        };

        Func<object[], SupplyOrderUkraine> mapper = objects => {
            SupplyOrderUkraine order = (SupplyOrderUkraine)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ActReconciliation act = (ActReconciliation)objects[3];
            Client supplier = (Client)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Organization agreementOrganization = (Organization)objects[7];
            ProviderPricing providerPricing = (ProviderPricing)objects[8];
            Currency currency = (Currency)objects[9];
            Pricing pricing = (Pricing)objects[10];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[11];
            Sad sad = (Sad)objects[12];
            Currency agreementCurrency = (Currency)objects[13];
            Currency additionalPaymentCurrency = (Currency)objects[14];

            if (providerPricing != null) {
                providerPricing.Currency = currency;
                providerPricing.Pricing = pricing;
            }

            agreement.Organization = agreementOrganization;
            agreement.Currency = agreementCurrency;
            agreement.ProviderPricing = providerPricing;

            clientAgreement.Agreement = agreement;

            order.Responsible = responsible;
            order.Organization = organization;
            order.Supplier = supplier;
            order.ClientAgreement = clientAgreement;
            order.TaxFreePackList = taxFreePackList;
            order.Sad = sad;
            order.AdditionalPaymentCurrency = additionalPaymentCurrency;

            if (act != null) order.ActReconciliations.Add(act);

            return order;
        };

        SupplyOrderUkraine toReturn =
            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderUkraine] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [ActReconciliation] " +
                "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                "AND [ActReconciliation].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [TaxFreePackList] " +
                "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
                "LEFT JOIN [Sad] " +
                "ON [SupplyOrderUkraine].ID = [Sad].SupplyOrderUkraineID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
                "ON [AdditionalPaymentCurrency].ID = [SupplyOrderUkraine].AdditionalPaymentCurrencyID " +
                "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
                "WHERE [SupplyOrderUkraine].ID = @Id",
                types,
                mapper,
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).SingleOrDefault();

        if (toReturn == null) return null;

        toReturn.SupplyOrderUkraineItems =
            _connection.Query<SupplyOrderUkraineItem, Product, SupplyOrderUkraineItem>(
                "SELECT * " +
                "FROM [SupplyOrderUkraineItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
                "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
                "AND [SupplyOrderUkraineItem].SupplyOrderUkraineID = @Id " +
                "ORDER BY [SupplyOrderUkraineItem].NotOrdered",
                (item, product) => {
                    item.Product = product;

                    item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                    item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 3, MidpointRounding.AwayFromZero);

                    item.NetWeight = Math.Round(item.NetWeight, 3, MidpointRounding.AwayFromZero);

                    toReturn.TotalNetPrice =
                        decimal.Round(toReturn.TotalNetPrice + item.NetPrice, 2, MidpointRounding.AwayFromZero);

                    toReturn.TotalNetWeight =
                        Math.Round(toReturn.TotalNetWeight + item.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

                    toReturn.TotalQty =
                        Math.Round(toReturn.TotalQty + item.Qty, 2, MidpointRounding.AwayFromZero);

                    return item;
                },
                new { toReturn.Id }
            ).ToList();

        _connection.Query<DynamicProductPlacementColumn, DynamicProductPlacementRow, SupplyOrderUkraineItem, DynamicProductPlacement, DynamicProductPlacementColumn>(
            "SELECT * " +
            "FROM [DynamicProductPlacementColumn] " +
            "LEFT JOIN [DynamicProductPlacementRow] " +
            "ON [DynamicProductPlacementRow].DynamicProductPlacementColumnID = [DynamicProductPlacementColumn].ID " +
            "AND [DynamicProductPlacementRow].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [DynamicProductPlacementRow].SupplyOrderUkraineItemID " +
            "LEFT JOIN [DynamicProductPlacement] " +
            "ON [DynamicProductPlacement].DynamicProductPlacementRowID = [DynamicProductPlacementRow].ID " +
            "AND [DynamicProductPlacement].Deleted = 0 " +
            "WHERE [DynamicProductPlacementColumn].Deleted = 0 " +
            "AND [DynamicProductPlacementColumn].SupplyOrderUkraineID = @Id",
            (column, row, item, placement) => {
                if (!toReturn.DynamicProductPlacementColumns.Any(c => c.Id.Equals(column.Id))) {
                    if (row != null) {
                        if (placement != null) row.DynamicProductPlacements.Add(placement);

                        row.SupplyOrderUkraineItem = item;

                        column.DynamicProductPlacementRows.Add(row);
                    }

                    toReturn.DynamicProductPlacementColumns.Add(column);
                } else {
                    if (row == null) return column;

                    DynamicProductPlacementColumn columnFromList = toReturn.DynamicProductPlacementColumns.First(c => c.Id.Equals(column.Id));

                    if (!columnFromList.DynamicProductPlacementRows.Any(r => r.Id.Equals(row.Id))) {
                        if (placement != null) row.DynamicProductPlacements.Add(placement);

                        row.SupplyOrderUkraineItem = item;

                        columnFromList.DynamicProductPlacementRows.Add(row);
                    } else {
                        if (placement != null) columnFromList.DynamicProductPlacementRows.First(r => r.Id.Equals(row.Id)).DynamicProductPlacements.Add(placement);
                    }
                }

                return column;
            },
            new { toReturn.Id }
        );

        _connection.Query<MergedService, SupplyOrganization, SupplyOrganizationAgreement, User, SupplyPaymentTask, User, Currency, MergedService>(
            "SELECT * " +
            "FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [MergedService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [MergedService].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [PaymentTaskUser] " +
            "ON [PaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].SupplyOrderUkraineID = @Id",
            (service, supplyOrganization, supplyOrganizationAgreement, user, supplyPaymentTask, paymentTaskUser, currency) => {
                if (supplyPaymentTask != null) supplyPaymentTask.User = paymentTaskUser;

                supplyOrganizationAgreement.Currency = currency;

                service.User = user;
                service.SupplyPaymentTask = supplyPaymentTask;
                service.SupplyOrganization = supplyOrganization;
                service.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.MergedServices.Add(service);

                return service;
            },
            new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn.MergedServices.Any()) {
            _connection.Query<InvoiceDocument, MergedService, InvoiceDocument>(
                "SELECT * " +
                "FROM [InvoiceDocument] " +
                "LEFT JOIN [MergedService] " +
                "ON [MergedService].ID = [InvoiceDocument].MergedServiceID " +
                "WHERE [InvoiceDocument].Deleted = 0 " +
                "AND [MergedService].ID IN @Ids",
                (document, service) => {
                    toReturn
                        .MergedServices
                        .First(s => s.Id.Equals(service.Id))
                        .InvoiceDocuments
                        .Add(document);

                    return document;
                },
                new { Ids = toReturn.MergedServices.Select(s => s.Id) }
            );

            _connection.Query<ServiceDetailItem, ServiceDetailItemKey, ServiceDetailItem>(
                "SELECT * " +
                "FROM [ServiceDetailItem] " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "WHERE [ServiceDetailItem].Deleted = 0 " +
                "AND [ServiceDetailItem].MergedServiceID IN @Ids",
                (item, itemKey) => {
                    item.ServiceDetailItemKey = itemKey;

                    toReturn
                        .MergedServices
                        .First(s => s.Id.Equals(item.MergedServiceId))
                        .ServiceDetailItems
                        .Add(item);

                    return item;
                },
                new { Ids = toReturn.MergedServices.Select(s => s.Id) }
            );
        }

        Type[] protocolsTypes = {
            typeof(SupplyOrderUkrainePaymentDeliveryProtocol),
            typeof(SupplyOrderUkrainePaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> protocolsMapper = objects => {
            SupplyOrderUkrainePaymentDeliveryProtocol protocol = (SupplyOrderUkrainePaymentDeliveryProtocol)objects[0];
            SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey = (SupplyOrderUkrainePaymentDeliveryProtocolKey)objects[1];
            User protocolUser = (User)objects[2];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[3];
            User supplyPaymentTaskUser = (User)objects[4];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
            User responsible = (User)objects[6];
            Organization organization = (Organization)objects[7];
            Client supplier = (Client)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];

            if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            supplyOrderUkraine.Supplier = supplier;
            supplyOrderUkraine.Responsible = responsible;
            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.ClientAgreement = clientAgreement;

            protocol.User = protocolUser;
            protocol.SupplyPaymentTask = supplyPaymentTask;
            protocol.SupplyOrderUkraine = supplyOrderUkraine;
            protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = protocolKey;

            toReturn.SupplyOrderUkrainePaymentDeliveryProtocols.Add(protocol);

            return protocol;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderUkrainePaymentDeliveryProtocolKey].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkrainePaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [ProtocolUser] " +
            "ON [ProtocolUser].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID = @Id " +
            "AND [SupplyOrderUkrainePaymentDeliveryProtocol].Deleted = 0",
            protocolsTypes,
            protocolsMapper,
            new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public SupplyOrderUkraine GetByNetId(Guid netId) {
        Type[] types = {
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(ActReconciliation),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(TaxFreePackList),
            typeof(Sad),
            typeof(Organization),
            typeof(OrganizationClient),
            typeof(Client),
            typeof(Organization),
            typeof(Client),
            typeof(Currency),
            typeof(DeliveryExpense)
        };

        Func<object[], SupplyOrderUkraine> mapper = objects => {
            SupplyOrderUkraine order = (SupplyOrderUkraine)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ActReconciliation act = (ActReconciliation)objects[3];
            Client supplier = (Client)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Organization agreementOrganization = (Organization)objects[7];
            ProviderPricing providerPricing = (ProviderPricing)objects[8];
            Currency currency = (Currency)objects[9];
            Pricing pricing = (Pricing)objects[10];
            Currency agreementCurrency = (Currency)objects[11];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[12];
            Sad sad = (Sad)objects[13];
            Organization sadOrganization = (Organization)objects[14];
            OrganizationClient sadOrganizationClient = (OrganizationClient)objects[15];
            Client sadClient = (Client)objects[16];
            Organization taxOrganization = (Organization)objects[17];
            Client taxClient = (Client)objects[18];
            Currency additionalPaymentCurrency = (Currency)objects[19];
            DeliveryExpense deliveryExpense = (DeliveryExpense)objects[20];

            if (providerPricing != null) {
                providerPricing.Currency = currency;
                providerPricing.Pricing = pricing;
            }

            if (sad != null) {
                sad.Client = sadClient;
                sad.Organization = sadOrganization;
                sad.OrganizationClient = sadOrganizationClient;
            }

            if (taxFreePackList != null) {
                taxFreePackList.Organization = taxOrganization;
                taxFreePackList.Client = taxClient;
            }

            agreement.Organization = agreementOrganization;
            agreement.ProviderPricing = providerPricing;
            agreement.Currency = agreementCurrency;

            clientAgreement.Agreement = agreement;

            order.Responsible = responsible;
            order.Organization = organization;
            order.Supplier = supplier;
            order.ClientAgreement = clientAgreement;
            order.TaxFreePackList = taxFreePackList;
            order.Sad = sad;
            order.AdditionalPaymentCurrency = additionalPaymentCurrency;

            if (act != null && !order.ActReconciliations.Any(x => x.Id.Equals(act.Id)))
                order.ActReconciliations.Add(act);

            if (deliveryExpense != null && !order.DeliveryExpenses.Any(e => e.Id.Equals(deliveryExpense.Id)))
                order.DeliveryExpenses.Add(deliveryExpense);

            return order;
        };

        SupplyOrderUkraine toReturn =
            _connection.Query(
                "SELECT " +
                "[SupplyOrderUkraine].* " +
                ", [Responsible].* " +
                ", [Organization].* " +
                ", [ActReconciliation].* " +
                ", [Supplier].* " +
                ", [ClientAgreement].* " +
                ", [Agreement].* " +
                ", [AgreementOrganization].* " +
                ", [ProviderPricing].* " +
                ", [Currency].* " +
                ", [Pricing].* " +
                ", [AgreementCurrency].* " +
                ", [TaxFreePackList].* " +
                ", [Sad].* " +
                ", [SadOrganization].* " +
                ", [SadOrganizationClient].* " +
                ", [SadClient].* " +
                ", [TaxOrganization].* " +
                ", [TaxClient].* " +
                ", [AdditionalPaymentCurrency].* " +
                ", [DeliveryExpense].* " +
                "FROM [SupplyOrderUkraine] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [ActReconciliation] " +
                "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                "AND [ActReconciliation].Deleted = 0 " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TaxFreePackList] " +
                "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
                "LEFT JOIN [Sad] " +
                "ON [SupplyOrderUkraine].ID = [Sad].SupplyOrderUkraineID " +
                "LEFT JOIN [views].[OrganizationView] AS [SadOrganization] " +
                "ON [SadOrganization].ID = [Sad].OrganizationID " +
                "AND [SadOrganization].CultureCode = @Culture " +
                "LEFT JOIN [OrganizationClient] AS [SadOrganizationClient] " +
                "ON [SadOrganizationClient].ID = [Sad].OrganizationClientID " +
                "LEFT JOIN [Client] AS [SadClient] " +
                "ON [SadClient].ID = [Sad].ClientID " +
                "LEFT JOIN [views].[OrganizationView] AS [TaxOrganization] " +
                "ON [TaxOrganization].ID = [TaxFreePackList].OrganizationID " +
                "AND [TaxOrganization].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [TaxClient] " +
                "ON [TaxClient].ID = [TaxFreePackList].ClientID " +
                "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
                "ON [AdditionalPaymentCurrency].ID = [SupplyOrderUkraine].AdditionalPaymentCurrencyID " +
                "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
                "LEFT JOIN [DeliveryExpense] " +
                "ON [DeliveryExpense].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                "WHERE [SupplyOrderUkraine].NetUID = @NetId",
                types,
                mapper,
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).FirstOrDefault();

        if (toReturn == null) return null;

        decimal grossDeliveryAmount = toReturn.DeliveryExpenses.Sum(e => e.GrossAmount);
        decimal accountingGrossDeliveryAmount = toReturn.DeliveryExpenses.Sum(e => e.AccountingGrossAmount);

        if (toReturn.ClientAgreement.Agreement.Currency != null) {
            Currency eur =
                _connection.Query<Currency>(
                    "SELECT TOP 1 * " +
                    "FROM [Currency] " +
                    "WHERE [Deleted] = 0 " +
                    "AND [Code] = 'eur'; ").FirstOrDefault();
            if (eur != null) {
                GovCrossExchangeRate govCrossExchangeRate = null;

                GovExchangeRate exchangeRateToEur =
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
                        "WHERE ([GovExchangeRate].CurrencyID = @FromId " +
                        "AND [GovExchangeRate].Code = @ToCode) " +
                        "OR ([GovExchangeRate].[CurrencyID] = @ToId " +
                        "AND [GovExchangeRate].Code = @FromCode) " +
                        "ORDER BY [GovExchangeRateHistory].Created DESC",
                        new {
                            FromId = eur.Id, FromCode = eur.Code, ToId = toReturn.ClientAgreement.Agreement.Currency.Id,
                            ToCode = toReturn.ClientAgreement.Agreement.Currency.Code, FromDate = toReturn.InvDate
                        }).FirstOrDefault();

                if (exchangeRateToEur == null)
                    govCrossExchangeRate =
                        _connection.Query<GovCrossExchangeRate>(
                            "SELECT TOP(1) " +
                            "[GovCrossExchangeRate].ID, " +
                            "(CASE " +
                            "WHEN [GovCrossExchangeRateHistory].Amount IS NOT NULL " +
                            "THEN [GovCrossExchangeRateHistory].Amount " +
                            "ELSE [GovCrossExchangeRateHistory].Amount " +
                            "END) AS [Amount] " +
                            "FROM [GovCrossExchangeRate] " +
                            "LEFT JOIN [GovCrossExchangeRateHistory] " +
                            "ON [GovCrossExchangeRateHistory].[GovCrossExchangeRateID] = [GovCrossExchangeRate].ID " +
                            "AND [GovCrossExchangeRateHistory].Created <= @FromDate " +
                            "WHERE ([GovCrossExchangeRate].CurrencyFromID = @From " +
                            "AND [GovCrossExchangeRate].CurrencyToID = @To) OR " +
                            "([GovCrossExchangeRate].CurrencyFromID = @To " +
                            "AND [GovCrossExchangeRate].CurrencyToID = @From) " +
                            "ORDER BY [GovCrossExchangeRateHistory].Created DESC ",
                            new { From = eur.Id, To = toReturn.ClientAgreement.Agreement.Currency.Id, FromDate = toReturn.InvDate }).FirstOrDefault();

                toReturn.ExchangeRateAmount = exchangeRateToEur?.Amount ?? govCrossExchangeRate?.Amount ?? 1;
            } else {
                toReturn.ExchangeRateAmount = 1;
            }
        }

        toReturn.SupplyOrderUkraineDocuments =
            _connection.Query<SupplyOrderUkraineDocument>(
                "SELECT * FROM [SupplyOrderUkraineDocument] " +
                "WHERE [SupplyOrderUkraineDocument].[SupplyOrderUkraineID] = @Id " +
                "AND [SupplyOrderUkraineDocument].[Deleted] = 0 ",
                new { toReturn.Id }).AsList();

        List<long> productIds = new();

        _connection.Query<SupplyOrderUkraineItem, Product, ProductPlacement, Storage, Client, ProductSpecification, MeasureUnit, SupplyOrderUkraineItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [SupplyOrderUkraineItem].[ProductSpecificationID] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineItem].SupplyOrderUkraineID = @Id " +
            "ORDER BY [SupplyOrderUkraineItem].NotOrdered",
            (item, product, placement, storage, supplier, productSpecification, measureUnit) => {
                if (!toReturn.SupplyOrderUkraineItems.Any(i => i.Id.Equals(item.Id))) {
                    if (placement != null) {
                        placement.Storage = storage;

                        item.ProductPlacements.Add(placement);
                    }

                    item.ProductSpecification = productSpecification;

                    product.MeasureUnit = measureUnit;
                    item.Product = product;
                    item.Supplier = supplier;

                    if (!productIds.Any(p => p.Equals(product.Id))) productIds.Add(product.Id);

                    item.NetPrice = item.UnitPrice * Convert.ToDecimal(item.Qty);

                    item.NetPriceLocal = item.UnitPriceLocal * Convert.ToDecimal(item.Qty);

                    item.GrossPrice = item.GrossUnitPrice * Convert.ToDecimal(item.Qty);

                    item.GrossPriceLocal = item.GrossUnitPriceLocal * Convert.ToDecimal(item.Qty);

                    item.AccountingGrossPrice = item.AccountingGrossUnitPrice * Convert.ToDecimal(item.Qty);

                    item.AccountingGrossPriceLocal = item.AccountingGrossUnitPriceLocal * Convert.ToDecimal(item.Qty);

                    item.NetWeight = Math.Round(item.NetWeight, 5);

                    item.GrossWeight = Math.Round(item.GrossWeight, 5);

                    item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 5, MidpointRounding.AwayFromZero);

                    item.TotalGrossWeight = Math.Round(item.GrossWeight * item.Qty, 5, MidpointRounding.AwayFromZero);

                    item.DeliveryAmount = item.UnitDeliveryAmount * Convert.ToDecimal(item.Qty); // deprecated

                    item.DeliveryAmountLocal = item.UnitDeliveryAmountLocal * Convert.ToDecimal(item.Qty); // deprecated

                    toReturn.TotalVatAmount += item.VatAmountLocal;

                    toReturn.SupplyOrderUkraineItems.Add(item);

                    toReturn.TotalNetPrice =
                        decimal.Round(toReturn.TotalNetPrice + item.NetPrice, 5, MidpointRounding.AwayFromZero);

                    toReturn.TotalGrossPrice =
                        decimal.Round(toReturn.TotalGrossPrice + item.GrossPrice, 5, MidpointRounding.AwayFromZero);

                    toReturn.TotalNetPriceLocal += item.NetPriceLocal;

                    toReturn.TotalGrossPriceLocal =
                        decimal.Round(toReturn.TotalGrossPriceLocal + item.GrossPriceLocal, 5, MidpointRounding.AwayFromZero);

                    toReturn.TotalAccountingGrossPrice += item.AccountingGrossPrice;

                    toReturn.TotalAccountingGrossPriceLocal += item.AccountingGrossPriceLocal;

                    toReturn.TotalNetWeight += item.TotalNetWeight;

                    toReturn.TotalGrossWeight += item.TotalGrossWeight;

                    toReturn.TotalNetPriceLocalWithVat += item.GrossPriceLocal;

                    toReturn.TotalQty =
                        Math.Round(toReturn.TotalQty + item.Qty, 3, MidpointRounding.AwayFromZero);
                } else {
                    if (placement == null) return item;

                    placement.Storage = storage;

                    toReturn.SupplyOrderUkraineItems.First(i => i.Id.Equals(item.Id)).ProductPlacements.Add(placement);
                }

                return item;
            },
            new { toReturn.Id }
        );

        foreach (SupplyOrderUkraineItem item in toReturn.SupplyOrderUkraineItems) {
            item.DeliveryExpenseAmount = grossDeliveryAmount * item.GrossPriceLocal / toReturn.TotalNetPriceLocalWithVat;
            item.AccountingDeliveryExpenseAmount = accountingGrossDeliveryAmount * item.AccountingGrossPriceLocal / toReturn.TotalNetPriceLocalWithVat;

            item.AccountingCost = item.AccountingGrossPriceLocal + item.AccountingDeliveryExpenseAmount;
            item.ManagementCost = item.GrossPriceLocal + item.DeliveryExpenseAmount + item.AccountingDeliveryExpenseAmount;

            toReturn.TotalDeliveryExpenseAmount += item.DeliveryExpenseAmount;
            toReturn.TotalAccountingDeliveryExpenseAmount += item.AccountingDeliveryExpenseAmount;
        }

        if (productIds.Any())
            _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                ";WITH [Search_CTE] " +
                "AS ( " +
                "SELECT MAX([ID]) AS [ID] " +
                "FROM [ProductPlacement] " +
                "WHERE [ProductPlacement].ProductID IN @Ids " +
                "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                "GROUP BY [ProductPlacement].CellNumber " +
                ", [ProductPlacement].RowNumber " +
                ", [ProductPlacement].StorageNumber " +
                ", [ProductPlacement].StorageID " +
                ") " +
                "SELECT * " +
                "FROM [ProductPlacement] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductPlacement].StorageID " +
                "WHERE [ProductPlacement].Deleted = 0 " +
                "AND [ProductPlacement].ID IN ( " +
                "SELECT [ID] " +
                "FROM [Search_CTE] " +
                ")",
                (placement, storage) => {
                    placement.Storage = storage;

                    foreach (SupplyOrderUkraineItem item in toReturn.SupplyOrderUkraineItems.Where(i => i.ProductId.Equals(placement.ProductId)))
                        item.Product.ProductPlacements.Add(placement);

                    return placement;
                },
                new { Ids = productIds }
            );

        productIds = new List<long>();

        _connection
            .Query<DynamicProductPlacementColumn, DynamicProductPlacementRow, SupplyOrderUkraineItem, Product, DynamicProductPlacement, DynamicProductPlacementColumn>(
                "SELECT * " +
                "FROM [DynamicProductPlacementColumn] " +
                "LEFT JOIN [DynamicProductPlacementRow] " +
                "ON [DynamicProductPlacementRow].DynamicProductPlacementColumnID = [DynamicProductPlacementColumn].ID " +
                "AND [DynamicProductPlacementRow].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [DynamicProductPlacementRow].SupplyOrderUkraineItemID " +
                "LEFT JOIN [Product] " +
                "ON [SupplyOrderUkraineItem].ProductID = [Product].ID " +
                "LEFT JOIN [DynamicProductPlacement] " +
                "ON [DynamicProductPlacement].DynamicProductPlacementRowID = [DynamicProductPlacementRow].ID " +
                "AND [DynamicProductPlacement].Deleted = 0 " +
                "WHERE [DynamicProductPlacementColumn].Deleted = 0 " +
                "AND [DynamicProductPlacementColumn].SupplyOrderUkraineID = @Id",
                (column, row, item, product, placement) => {
                    if (!toReturn.DynamicProductPlacementColumns.Any(c => c.Id.Equals(column.Id))) {
                        if (row != null) {
                            if (placement != null) row.DynamicProductPlacements.Add(placement);

                            productIds.Add(product.Id);

                            item.Product = product;

                            row.SupplyOrderUkraineItem = item;

                            column.DynamicProductPlacementRows.Add(row);
                        }

                        toReturn.DynamicProductPlacementColumns.Add(column);
                    } else {
                        DynamicProductPlacementColumn columnFromList = toReturn.DynamicProductPlacementColumns.First(c => c.Id.Equals(column.Id));

                        if (row == null) return column;

                        if (!columnFromList.DynamicProductPlacementRows.Any(r => r.Id.Equals(row.Id))) {
                            if (placement != null) row.DynamicProductPlacements.Add(placement);

                            productIds.Add(product.Id);

                            item.Product = product;

                            row.SupplyOrderUkraineItem = item;

                            columnFromList.DynamicProductPlacementRows.Add(row);
                        } else {
                            if (placement != null) columnFromList.DynamicProductPlacementRows.First(r => r.Id.Equals(row.Id)).DynamicProductPlacements.Add(placement);
                        }
                    }

                    return column;
                },
                new { toReturn.Id }
            );

        if (productIds.Any())
            _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                ";WITH [Search_CTE] " +
                "AS ( " +
                "SELECT MAX([ID]) AS [ID] " +
                "FROM [ProductPlacement] " +
                "WHERE [ProductPlacement].ProductID IN @Ids " +
                "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                "GROUP BY [ProductPlacement].CellNumber " +
                ", [ProductPlacement].RowNumber " +
                ", [ProductPlacement].StorageNumber " +
                ", [ProductPlacement].StorageID " +
                ") " +
                "SELECT * " +
                "FROM [ProductPlacement] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductPlacement].StorageID " +
                "WHERE [ProductPlacement].ID IN ( " +
                "SELECT [ID] " +
                "FROM [Search_CTE] " +
                ")",
                (placement, storage) => {
                    placement.Storage = storage;

                    foreach (DynamicProductPlacementColumn column in toReturn
                                 .DynamicProductPlacementColumns
                                 .Where(c => c.DynamicProductPlacementRows
                                     .Any(r => r.SupplyOrderUkraineItem.ProductId.Equals(placement.ProductId))))
                    foreach (DynamicProductPlacementRow row in column.DynamicProductPlacementRows.Where(r =>
                                 r.SupplyOrderUkraineItem.ProductId.Equals(placement.ProductId)))
                        row.SupplyOrderUkraineItem.Product.ProductPlacements.Add(placement);

                    return placement;
                },
                new { Ids = productIds }
            );

        Type[] mergedServiceTypes = new[] {
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(Currency)
        };

        Func<object[], MergedService> mergedServiceMapper = objects => {
            MergedService service = (MergedService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
            User serviceUser = (User)objects[3];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[4];
            SupplyPaymentTask accountingPaymentTask = (SupplyPaymentTask)objects[5];
            User paymentTaskUser = (User)objects[6];
            User accountingPaymentTaskUser = (User)objects[7];
            Currency currency = (Currency)objects[8];

            if (supplyPaymentTask != null) supplyPaymentTask.User = paymentTaskUser;

            supplyOrganizationAgreement.Currency = currency;

            service.User = serviceUser;
            service.SupplyPaymentTask = supplyPaymentTask;
            service.SupplyOrganization = supplyOrganization;
            accountingPaymentTask.User = accountingPaymentTaskUser;
            service.AccountingPaymentTask = accountingPaymentTask;
            service.SupplyOrganizationAgreement = supplyOrganizationAgreement;

            toReturn.MergedServices.Add(service);

            return service;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [MergedService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [MergedService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
            "ON [AccountingPaymentTask].ID = [MergedService].AccountingPaymentTaskID " +
            "LEFT JOIN [User] AS [PaymentTaskUser] " +
            "ON [PaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [User] AS [AccountingPaymentTaskUser] " +
            "ON [AccountingPaymentTaskUser].ID = [AccountingPaymentTask].UserID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].SupplyOrderUkraineID = @Id",
            mergedServiceTypes, mergedServiceMapper,
            new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn.MergedServices.Any()) {
            _connection.Query<InvoiceDocument, MergedService, InvoiceDocument>(
                "SELECT * " +
                "FROM [InvoiceDocument] " +
                "LEFT JOIN [MergedService] " +
                "ON [MergedService].ID = [InvoiceDocument].MergedServiceID " +
                "WHERE [InvoiceDocument].Deleted = 0 " +
                "AND [MergedService].ID IN @Ids",
                (document, service) => {
                    toReturn
                        .MergedServices
                        .First(s => s.Id.Equals(service.Id))
                        .InvoiceDocuments
                        .Add(document);

                    return document;
                },
                new { Ids = toReturn.MergedServices.Select(s => s.Id) }
            );

            _connection.Query<ServiceDetailItem, ServiceDetailItemKey, ServiceDetailItem>(
                "SELECT * " +
                "FROM [ServiceDetailItem] " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "WHERE [ServiceDetailItem].Deleted = 0 " +
                "AND [ServiceDetailItem].MergedServiceID IN @Ids",
                (item, itemKey) => {
                    item.ServiceDetailItemKey = itemKey;

                    toReturn
                        .MergedServices
                        .First(s => s.Id.Equals(item.MergedServiceId))
                        .ServiceDetailItems
                        .Add(item);

                    return item;
                },
                new { Ids = toReturn.MergedServices.Select(s => s.Id) }
            );
        }

        Type[] protocolsTypes = {
            typeof(SupplyOrderUkrainePaymentDeliveryProtocol),
            typeof(SupplyOrderUkrainePaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> protocolsMapper = objects => {
            SupplyOrderUkrainePaymentDeliveryProtocol protocol = (SupplyOrderUkrainePaymentDeliveryProtocol)objects[0];
            SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey = (SupplyOrderUkrainePaymentDeliveryProtocolKey)objects[1];
            User protocolUser = (User)objects[2];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[3];
            User supplyPaymentTaskUser = (User)objects[4];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
            User responsible = (User)objects[6];
            Organization organization = (Organization)objects[7];
            Client supplier = (Client)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];

            if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

            agreement.Currency = currency;

            clientAgreement.Agreement = agreement;

            supplyOrderUkraine.Supplier = supplier;
            supplyOrderUkraine.Responsible = responsible;
            supplyOrderUkraine.Organization = organization;
            supplyOrderUkraine.ClientAgreement = clientAgreement;

            protocol.User = protocolUser;
            protocol.SupplyPaymentTask = supplyPaymentTask;
            protocol.SupplyOrderUkraine = supplyOrderUkraine;
            protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = protocolKey;

            toReturn.SupplyOrderUkrainePaymentDeliveryProtocols.Add(protocol);

            return protocol;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderUkrainePaymentDeliveryProtocolKey].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkrainePaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [ProtocolUser] " +
            "ON [ProtocolUser].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID = @Id " +
            "AND [SupplyOrderUkrainePaymentDeliveryProtocol].Deleted = 0",
            protocolsTypes,
            protocolsMapper,
            new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<SupplyOrderUkraine> GetAll() {
        List<SupplyOrderUkraine> orders = new();

        Type[] types = {
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraineItem),
            typeof(Product),
            typeof(ActReconciliation),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(TaxFreePackList),
            typeof(Sad)
        };

        Func<object[], SupplyOrderUkraine> mapper = objects => {
            SupplyOrderUkraine order = (SupplyOrderUkraine)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[3];
            Product product = (Product)objects[4];
            ActReconciliation act = (ActReconciliation)objects[5];
            Client supplier = (Client)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Agreement agreement = (Agreement)objects[8];
            Organization agreementOrganization = (Organization)objects[9];
            ProviderPricing providerPricing = (ProviderPricing)objects[10];
            Currency currency = (Currency)objects[11];
            Pricing pricing = (Pricing)objects[12];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[13];
            Sad sad = (Sad)objects[14];

            if (orders.Any(o => o.Id.Equals(order.Id))) {
                SupplyOrderUkraine orderFromList = orders.First(o => o.Id.Equals(order.Id));

                if (item == null || orderFromList.SupplyOrderUkraineItems.Any(i => i.Id.Equals(item.Id))) return order;

                item.Product = product;

                item.QtyDifferent = item.Qty - item.PlacedQty;

                item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 3, MidpointRounding.AwayFromZero);

                item.NetWeight = Math.Round(item.NetWeight, 3, MidpointRounding.AwayFromZero);

                order.TotalNetPrice =
                    decimal.Round(order.TotalNetPrice + item.NetPrice, 2, MidpointRounding.AwayFromZero);

                order.TotalNetWeight =
                    Math.Round(order.TotalNetWeight + item.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

                orderFromList.TotalQty =
                    Math.Round(orderFromList.TotalQty + item.Qty, 2, MidpointRounding.AwayFromZero);

                orderFromList.SupplyOrderUkraineItems.Add(item);
            } else {
                if (item != null) {
                    item.Product = product;

                    item.QtyDifferent = item.Qty - item.PlacedQty;

                    item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                    item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 3, MidpointRounding.AwayFromZero);

                    item.NetWeight = Math.Round(item.NetWeight, 3, MidpointRounding.AwayFromZero);

                    order.TotalNetPrice =
                        decimal.Round(order.TotalNetPrice + item.NetPrice, 2, MidpointRounding.AwayFromZero);

                    order.TotalNetWeight =
                        Math.Round(order.TotalNetWeight + item.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

                    order.TotalQty =
                        Math.Round(order.TotalQty + item.Qty, 2, MidpointRounding.AwayFromZero);

                    order.SupplyOrderUkraineItems.Add(item);
                }

                if (providerPricing != null) {
                    providerPricing.Currency = currency;
                    providerPricing.Pricing = pricing;
                }

                agreement.Organization = agreementOrganization;
                agreement.ProviderPricing = providerPricing;

                clientAgreement.Agreement = agreement;

                order.Responsible = responsible;
                order.Organization = organization;
                order.Supplier = supplier;
                order.ClientAgreement = clientAgreement;
                order.TaxFreePackList = taxFreePackList;
                order.Sad = sad;

                if (act != null) order.ActReconciliations.Add(act);

                orders.Add(order);
            }

            return order;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [SupplyOrderUkraineItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [ActReconciliation].Deleted = 0 " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
            "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
            "AND [AgreementOrganization].CultureCode = @Culture " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [ProviderPricing].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "WHERE [SupplyOrderUkraine].Deleted = 0 " +
            "LEFT JOIN [Sad] " +
            "ON [SupplyOrderUkraine].ID = [Sad].SupplyOrderUkraineID " +
            "ORDER BY [SupplyOrderUkraine].FromDate DESC, [SupplyOrderUkraine].Number DESC, [SupplyOrderUkraineItem].NotOrdered",
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return orders;
    }

    public List<SupplyOrderUkraine> GetAllIncomeOrdersFiltered(
        DateTime from,
        DateTime to,
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        long limit,
        long offset) {
        List<SupplyOrderUkraine> orders = new();

        string sqlExpression =
            "; WITH [Search_CTE] " +
            "AS (" +
            "SELECT [SupplyOrderUkraine].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [SupplyOrderUkraine].FromDate DESC) AS [RowNumber] " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [SupplyOrderUkraineItem].Deleted = 0 " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID ";

        sqlExpression +=
            "WHERE [SupplyOrderUkraine].FromDate >= @From " +
            "AND [SupplyOrderUkraine].FromDate <= @To " +
            "AND [SupplyOrderUkraine].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId " +
            "AND [SupplyOrderUkraine].[Number] like N'%' + @Value + N'%' ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "AND [Supplier].NetUID = @SupplierNetId ";

        sqlExpression +=
            "GROUP BY [SupplyOrderUkraine].ID, [SupplyOrderUkraine].FromDate " +
            ") " +
            "SELECT * " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [SupplyOrderUkraineItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "WHERE [ProductIncomeItem].ID IS NOT NULL " +
            "AND [SupplyOrderUkraine].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ")";

        Type[] types = {
            typeof(SupplyOrderUkraine),
            typeof(Client),
            typeof(SupplyOrderUkraineItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductIncomeItem)
        };

        Func<object[], SupplyOrderUkraine> mapper = objects => {
            SupplyOrderUkraine order = (SupplyOrderUkraine)objects[0];
            Client supplier = (Client)objects[1];
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductIncomeItem incomeItem = (ProductIncomeItem)objects[5];

            if (!orders.Any(o => o.Id.Equals(order.Id))) {
                product.MeasureUnit = measureUnit;

                item.Product = product;

                item.ProductIncomeItems.Add(incomeItem);

                order.Supplier = supplier;

                order.SupplyOrderUkraineItems.Add(item);

                orders.Add(order);
            } else {
                SupplyOrderUkraine orderFromList = orders.First(o => o.Id.Equals(order.Id));

                if (!orderFromList.SupplyOrderUkraineItems.Any(i => i.Id.Equals(item.Id))) {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    item.ProductIncomeItems.Add(incomeItem);

                    orderFromList.SupplyOrderUkraineItems.Add(item);
                } else {
                    SupplyOrderUkraineItem itemFromList = orderFromList.SupplyOrderUkraineItems.First(i => i.Id.Equals(item.Id));

                    itemFromList.ProductIncomeItems.Add(incomeItem);
                }
            }

            return order;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Value = value,
                SupplierNetId = supplierNetId,
                StorageNetId = storageNetId,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return orders;
    }

    public decimal GetTotalEuroAmountForPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        DateTime from,
        DateTime to) {
        string sqlExpression =
            "; WITH [Total_CTE] " +
            "AS (" +
            "SELECT " +
            "ROUND([SupplyOrderUkraineItem].GrossUnitPrice * SUM([ProductIncomeItem].RemainingQty), 2) AS [TotalAmount] " +
            "FROM [SupplyOrderUkraineItem] " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID ";

        sqlExpression +=
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraine].Deleted = 0 " +
            "AND [Storage].NetUID = @StorageNetId ";

        if (supplierNetId.HasValue)
            sqlExpression +=
                "AND [Supplier].NetUID = @SupplierNetId ";

        sqlExpression +=
            "AND [SupplyOrderUkraine].FromDate >= @From " +
            "AND [SupplyOrderUkraine].FromDate <= @To " +
            "AND [SupplyOrderUkraine].[Number] like N'%' + @Value + N'%' " +
            "GROUP BY [SupplyOrderUkraineItem].ID, [SupplyOrderUkraineItem].GrossUnitPrice" +
            ")" +
            "SELECT " +
            "ISNULL(" +
            "ROUND(" +
            "SUM([Total_CTE].TotalAmount)" +
            ", 2)" +
            ", 0) " +
            "FROM [Total_CTE]";

        return _connection.Query<decimal>(
            sqlExpression,
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                Value = value,
                From = from,
                To = to
            }
        ).SingleOrDefault();
    }

    public List<SupplyOrderUkraine> GetAllFiltered(DateTime from, DateTime to, string supplierName, long? currencyId, long limit, long offset, bool nonPlaced) {
        List<SupplyOrderUkraine> orders = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS (" +
            "SELECT [SupplyOrderUkraine].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [SupplyOrderUkraine].FromDate DESC) AS [RowNumber] " +
            ", COUNT(*) OVER() [TotalRowsQty] " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [SupplyOrderUkraine].Deleted = 0 " +
            "AND [Supplier].FullName LIKE N'%' + @Name + N'%' " +
            "AND [SupplyOrderUkraine].FromDate >= @From " +
            "AND [SupplyOrderUkraine].FromDate <= @To ";

        if (currencyId.HasValue) sqlExpression += "AND [Currency].ID = @CurrencyId ";

        if (nonPlaced) sqlExpression += "AND [SupplyOrderUkraine].IsPlaced = 0";

        sqlExpression +=
            ") " +
            "SELECT [SupplyOrderUkraine].* " +
            ", (SELECT TOP 1 TotalRowsQty FROM [Search_CTE]) AS TotalRowsQty " +
            ", ( " +
            "ROUND( " +
            "( " +
            "SELECT SUM( " +
            "ROUND([Item].GrossUnitPriceLocal * [Item].Qty, 2) " +
            ") " +
            "FROM [SupplyOrderUkraineItem] AS [Item] " +
            "WHERE [Item].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [Item].Deleted = 0 " +
            ") " +
            ", 2) " +
            ") AS [TotalGrossPriceLocal] " +
            ",[Responsible].* " +
            ",[Organization].* " +
            ",[SupplyOrderUkraineItem].* " +
            ",[Product].* " +
            ",[ActReconciliation].* " +
            ",[Supplier].* " +
            ",[ClientAgreement].* " +
            ",[Agreement].* " +
            ",[AgreementOrganization].* " +
            ",[ProviderPricing].* " +
            ",[Currency].* " +
            ",[Pricing].* " +
            ",[TaxFreePackList].* " +
            ",[Sad].* " +
            ",[AdditionalPaymentCurrency].* " +
            ",[AgreementCurrency].* " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [SupplyOrderUkraineItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "AND [ActReconciliation].Deleted = 0 " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
            "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
            "AND [AgreementOrganization].CultureCode = @Culture " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [ProviderPricing].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [SupplyOrderUkraine].ID = [TaxFreePackList].SupplyOrderUkraineID " +
            "LEFT JOIN [Sad] " +
            "ON [SupplyOrderUkraine].ID = [Sad].SupplyOrderUkraineID " +
            "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
            "ON [AdditionalPaymentCurrency].ID = [SupplyOrderUkraine].AdditionalPaymentCurrencyID " +
            "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "WHERE [SupplyOrderUkraine].ID IN (" +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ") " +
            "ORDER BY [SupplyOrderUkraine].FromDate DESC";

        Type[] types = {
            typeof(SupplyOrderUkraine),
            typeof(User),
            typeof(Organization),
            typeof(SupplyOrderUkraineItem),
            typeof(Product),
            typeof(ActReconciliation),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(TaxFreePackList),
            typeof(Sad),
            typeof(Currency),
            typeof(Currency)
        };

        Func<object[], SupplyOrderUkraine> mapper = objects => {
            SupplyOrderUkraine order = (SupplyOrderUkraine)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[3];
            Product product = (Product)objects[4];
            ActReconciliation act = (ActReconciliation)objects[5];
            Client supplier = (Client)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Agreement agreement = (Agreement)objects[8];
            Organization agreementOrganization = (Organization)objects[9];
            ProviderPricing providerPricing = (ProviderPricing)objects[10];
            Currency currency = (Currency)objects[11];
            Pricing pricing = (Pricing)objects[12];
            TaxFreePackList taxFreePackList = (TaxFreePackList)objects[13];
            Sad sad = (Sad)objects[14];
            Currency additionalPaymentCurrency = (Currency)objects[15];
            Currency agreementCurrency = (Currency)objects[16];

            if (orders.Any(o => o.Id.Equals(order.Id))) {
                SupplyOrderUkraine orderFromList = orders.First(o => o.Id.Equals(order.Id));

                if (item == null || orderFromList.SupplyOrderUkraineItems.Any(i => i.Id.Equals(item.Id))) return order;

                item.Product = product;

                item.QtyDifferent = item.Qty - item.PlacedQty;

                item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 3, MidpointRounding.AwayFromZero);

                item.NetWeight = Math.Round(item.NetWeight, 3, MidpointRounding.AwayFromZero);

                order.TotalNetPrice =
                    decimal.Round(order.TotalNetPrice + item.NetPrice, 2, MidpointRounding.AwayFromZero);

                order.TotalNetWeight =
                    Math.Round(order.TotalNetWeight + item.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

                orderFromList.TotalQty =
                    Math.Round(orderFromList.TotalQty + item.Qty, 2, MidpointRounding.AwayFromZero);

                orderFromList.SupplyOrderUkraineItems.Add(item);
            } else {
                if (item != null) {
                    item.Product = product;

                    item.QtyDifferent = item.Qty - item.PlacedQty;

                    item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                    item.TotalNetWeight = Math.Round(item.NetWeight * item.Qty, 3, MidpointRounding.AwayFromZero);

                    item.NetWeight = Math.Round(item.NetWeight, 3, MidpointRounding.AwayFromZero);

                    order.TotalNetPrice =
                        decimal.Round(order.TotalNetPrice + item.NetPrice, 2, MidpointRounding.AwayFromZero);

                    order.TotalNetWeight =
                        Math.Round(order.TotalNetWeight + item.TotalNetWeight, 3, MidpointRounding.AwayFromZero);

                    order.TotalQty =
                        Math.Round(order.TotalQty + item.Qty, 2, MidpointRounding.AwayFromZero);

                    order.SupplyOrderUkraineItems.Add(item);
                }

                if (providerPricing != null) {
                    providerPricing.Currency = currency;
                    providerPricing.Pricing = pricing;
                }

                agreement.Organization = agreementOrganization;
                agreement.ProviderPricing = providerPricing;
                agreement.Currency = agreementCurrency;

                clientAgreement.Agreement = agreement;

                order.Responsible = responsible;
                order.Organization = organization;
                order.Supplier = supplier;
                order.ClientAgreement = clientAgreement;
                order.TaxFreePackList = taxFreePackList;
                order.Sad = sad;
                order.AdditionalPaymentCurrency = additionalPaymentCurrency;

                if (act != null) order.ActReconciliations.Add(act);

                orders.Add(order);
            }

            return order;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                From = from,
                To = to,
                Name = supplierName,
                CurrencyId = currencyId,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        if (!orders.Any()) return orders;

        IEnumerable<DeliveryExpense> deliveryExpenses = _connection.Query<DeliveryExpense>(
            "SELECT * FROM [DeliveryExpense] " +
            "WHERE [SupplyOrderUkraineID] IN @SupplyOrderUkraineIds ",
            new { SupplyOrderUkraineIds = orders.Select(e => e.Id) });

        foreach (DeliveryExpense deliveryExpense in deliveryExpenses) {
            SupplyOrderUkraine orderFromList = orders.FirstOrDefault(e => e.Id.Equals(deliveryExpense.SupplyOrderUkraineId));

            orderFromList?.DeliveryExpenses.Add(deliveryExpense);
        }

        Type[] paymentTypes = {
            typeof(SupplyOrderUkrainePaymentDeliveryProtocol),
            typeof(SupplyOrderUkrainePaymentDeliveryProtocolKey),
            typeof(SupplyPaymentTask)
        };

        Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> paymentMappers = objects => {
            SupplyOrderUkrainePaymentDeliveryProtocol protocol = (SupplyOrderUkrainePaymentDeliveryProtocol)objects[0];
            SupplyOrderUkrainePaymentDeliveryProtocolKey key = (SupplyOrderUkrainePaymentDeliveryProtocolKey)objects[1];
            SupplyPaymentTask task = (SupplyPaymentTask)objects[2];

            SupplyOrderUkraine existOrder = orders.First(x => x.Id.Equals(protocol.SupplyOrderUkraineId));

            protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = key;
            protocol.SupplyPaymentTask = task;

            if (existOrder.SupplyOrderUkrainePaymentDeliveryProtocols.Any(x => x.Id.Equals(protocol.Id)))
                return protocol;

            existOrder.SupplyOrderUkrainePaymentDeliveryProtocols.Add(protocol);

            existOrder.TotalProtocolsValue += protocol.Value;
            existOrder.TotalProtocolsDiscount += protocol.Discount;

            return protocol;
        };

        _connection.Query(
            "SELECT * FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderUkrainePaymentDeliveryProtocolKey].[ID] = " +
            "[SupplyOrderUkrainePaymentDeliveryProtocol].[SupplyOrderUkrainePaymentDeliveryProtocolKeyID] " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].[ID] = [SupplyOrderUkrainePaymentDeliveryProtocol].[SupplyPaymentTaskID] " +
            "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].[Deleted] = 0 " +
            "AND [SupplyOrderUkrainePaymentDeliveryProtocol].[SupplyOrderUkraineID] IN @Ids; ",
            paymentTypes,
            paymentMappers,
            new { Ids = orders.Select(o => o.Id) });

        return orders;
    }

    public void UpdateVatPercent(long id, decimal vatPercent) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET [Updated] = getutcdate() " +
            ", [VatPercent] = @VatPercent " +
            "WHERE [SupplyOrderUkraine].[ID] = @Id ",
            new { Id = id, VatPercent = vatPercent });
    }

    public void UpdateIsPartialPlaced(SupplyOrderUkraine fromDb) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraine] " +
            "SET [Updated] = getutcdate() " +
            ", [IsPartialPlaced] = @IsPartialPlaced " +
            "WHERE [SupplyOrderUkraine].[ID] = @Id; ",
            fromDb);
    }

    public List<SupplyOrderModel> GetAllForPrintDocument(DateTime from, DateTime to) {
        return _connection.Query<SupplyOrderModel>(
            ";WITH [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE] AS ( " +
            "SELECT " +
            "[SupplyOrderUkraine].[ID] " +
            ", ROUND(SUM([SupplyOrderUkraineItem].[UnitPriceLocal] * [SupplyOrderUkraineItem].[Qty] + [SupplyOrderUkraineItem].[VatAmountLocal]), 2) AS [TotalPrice] " +
            ", SUM([SupplyOrderUkraineItem].[Qty]) AS [TotalQty] " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[SupplyOrderUkraineID] = [SupplyOrderUkraine].[ID] " +
            "AND [SupplyOrderUkraineItem].[Deleted] = 0 " +
            "WHERE [SupplyOrderUkraine].[Deleted] = 0 " +
            "AND [SupplyOrderUkraine].[FromDate] >= @From " +
            "AND [SupplyOrderUkraine].[FromDate] <= @To " +
            "GROUP BY [SupplyOrderUkraine].[ID] " +
            ") " +
            "SELECT " +
            "[SupplyOrderUkraine].[Number] " +
            ", [SupplyOrderUkraine].[Created] " +
            ", [SupplyOrderUkraine].[FromDate] " +
            ", [SupplyOrderUkraine].[InvNumber] " +
            ", [SupplyOrderUkraine].[InvDate] " +
            ", CASE " +
            "WHEN [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE].[TotalPrice] IS NULL " +
            "THEN 0 " +
            "ELSE [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE].[TotalPrice] " +
            "END AS [TotalPrice] " +
            ", CASE " +
            "WHEN [Client].[Name] IS NULL " +
            "THEN [Client].[SupplierName] " +
            "ELSE [Client].[Name] " +
            "END AS [Supplier] " +
            ", [Agreement].[Name] AS [Agreement] " +
            ", [Currency].[Code] AS [Currency] " +
            ", CASE " +
            "WHEN [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE].[TotalQty] IS NULL " +
            "THEN 0 " +
            "ELSE [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE].[TotalQty] " +
            "END AS [Qty] " +
            ", [SupplyOrderUkraine].[AdditionalAmount] AS [AdditionalPrice] " +
            ", CASE " +
            "WHEN [Organization].[Name] IS NULL " +
            "THEN [Organization].[FullName] " +
            "ELSE [Organization].[Name] " +
            "END AS [Organization] " +
            ", CASE " +
            "WHEN [SupplyOrderUkraine].[IsPlaced] = 1 " +
            "THEN N'Так' " +
            "ELSE N'Ні' " +
            "END AS [Placed] " +
            ", [User].[LastName] AS [Responsible] " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE] " +
            "ON [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE].[ID] = [SupplyOrderUkraine].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrderUkraine].[OrganizationID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [SupplyOrderUkraine].[ResponsibleID] " +
            "WHERE [SupplyOrderUkraine].[ID] IN ( " +
            "SELECT [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE].[ID] " +
            "FROM [TOTAL_VALUE_SUPPLY_ORDER_UKRAINE] " +
            "); ",
            new { From = from, To = to }).AsList();
    }
}