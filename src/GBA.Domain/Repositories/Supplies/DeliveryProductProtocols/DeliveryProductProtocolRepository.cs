using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.Supplies.DeliveryProductProtocolModels;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;

namespace GBA.Domain.Repositories.Supplies.DeliveryProductProtocols;

public sealed class DeliveryProductProtocolRepository : IDeliveryProductProtocolRepository {
    private readonly IDbConnection _connection;

    public DeliveryProductProtocolRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long AddNew(DeliveryProductProtocol deliveryProductProtocol) {
        return _connection.Query<long>(
            "INSERT INTO [DeliveryProductProtocol] ([TransportationType], [UserID], [Updated], [Comment], [FromDate], " +
            "[IsCompleted], [IsPartiallyPlaced], [IsPlaced], [OrganizationID], [DeliveryProductProtocolNumberID]) " +
            "VALUES(@TransportationType, @UserID, @Updated, @Comment, @FromDate, @IsCompleted, @IsPartiallyPlaced, @IsPlaced, @OrganizationID, " +
            "@DeliveryProductProtocolNumberID); " +
            "SELECT SCOPE_IDENTITY() ",
            deliveryProductProtocol).Single();
    }

    public DeliveryProductProtocol GetById(long deliveryProductProtocolId) {
        return _connection.Query<DeliveryProductProtocol>(
            "SELECT * FROM [DeliveryProductProtocol] " +
            "WHERE [DeliveryProductProtocol].[ID] =@Id; ",
            new { Id = deliveryProductProtocolId }).FirstOrDefault();
    }

    public DeliveryProductProtocol GetByNetId(Guid netId) {
        DeliveryProductProtocol toReturn = new();

        Type[] protocolType = {
            typeof(DeliveryProductProtocol),
            typeof(User),
            typeof(Organization),
            typeof(DeliveryProductProtocolNumber),
            typeof(DeliveryProductProtocolDocument)
        };

        Func<object[], DeliveryProductProtocol> protocolMapper = objects => {
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[0];
            User user = (User)objects[1];
            Organization organization = (Organization)objects[2];
            DeliveryProductProtocolNumber protocolNumber = (DeliveryProductProtocolNumber)objects[3];
            DeliveryProductProtocolDocument document = (DeliveryProductProtocolDocument)objects[4];

            if (toReturn.Id.Equals(0))
                toReturn = protocol;
            toReturn.User = user;
            toReturn.Organization = organization;
            toReturn.DeliveryProductProtocolNumber = protocolNumber;
            if (document == null) return protocol;
            toReturn.DeliveryProductProtocolDocuments.Add(document);
            return protocol;
        };

        _connection.Query(
            "SELECT * FROM [DeliveryProductProtocol] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [DeliveryProductProtocol].[UserID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [DeliveryProductProtocol].[OrganizationID] " +
            "LEFT JOIN [DeliveryProductProtocolNumber] " +
            "ON [DeliveryProductProtocolNumber].[ID] = [DeliveryProductProtocol].[DeliveryProductProtocolNumberID] " +
            "LEFT JOIN [DeliveryProductProtocolDocument] " +
            "ON [DeliveryProductProtocolDocument].[DeliveryProductProtocolID] = [DeliveryProductProtocol].[ID] " +
            "AND [DeliveryProductProtocolDocument].[Deleted] = 0 " +
            "WHERE [DeliveryProductProtocol].[NetUID] = @NetId ",
            protocolType, protocolMapper, new { NetId = netId });

        if (toReturn == null || toReturn.Id.Equals(0)) throw new Exception("Delivery product protocol is not exist");

        bool hasSupplyInvoices = _connection.Query<bool>(
            "SELECT IIF(COUNT([SupplyInvoice].[ID])>0,1,0) " +
            "FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            new { toReturn.Id }).Single();

        if (hasSupplyInvoices) {
            Type[] supplyInvoicesType = {
                typeof(SupplyInvoice),
                typeof(PackingList),
                typeof(PackingListPackageOrderItem),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(Organization),
                typeof(SupplyInvoiceDeliveryDocument),
                typeof(SupplyDeliveryDocument)
            };

            Func<object[], SupplyInvoice> supplyInvoicesMapper = objects => {
                SupplyInvoice invoice = (SupplyInvoice)objects[0];
                PackingList packingList = (PackingList)objects[1];
                PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[2];
                SupplyOrder supplyOrder = (SupplyOrder)objects[3];
                Client client = (Client)objects[4];
                ClientAgreement clientAgreement = (ClientAgreement)objects[5];
                Agreement agreement = (Agreement)objects[6];
                Currency currency = (Currency)objects[7];
                Organization organization = (Organization)objects[8];
                SupplyInvoiceDeliveryDocument document = (SupplyInvoiceDeliveryDocument)objects[9];
                SupplyDeliveryDocument typeDocument = (SupplyDeliveryDocument)objects[10];

                if (!toReturn.SupplyInvoices.Any(x => x.Id.Equals(invoice.Id)))
                    toReturn.SupplyInvoices.Add(invoice);
                else
                    invoice = toReturn.SupplyInvoices.First(x => x.Id.Equals(invoice.Id));

                supplyOrder.Organization = organization;
                supplyOrder.Client = client;

                agreement.Currency = currency;
                clientAgreement.Agreement = agreement;
                supplyOrder.ClientAgreement = clientAgreement;

                invoice.SupplyOrder = supplyOrder;

                if (document != null)
                    if (!invoice.SupplyInvoiceDeliveryDocuments.Any(x => x.Id.Equals(document.Id))) {
                        document.SupplyDeliveryDocument = typeDocument;

                        invoice.SupplyInvoiceDeliveryDocuments.Add(document);
                    }

                if (packingList == null) return invoice;

                if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                    invoice.PackingLists.Add(packingList);
                else
                    packingList = invoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

                if (item != null)
                    if (!packingList.PackingListPackageOrderItems.Any(x => x.Id.Equals(item.Id))) {
                        item.TotalNetPrice = Convert.ToDecimal(item.Qty) * item.UnitPrice;
                        item.TotalGrossPrice = Convert.ToDecimal(item.Qty) * item.GrossUnitPriceEur;
                        item.AccountingTotalGrossPrice = Convert.ToDecimal(item.Qty) * item.AccountingGrossUnitPriceEur;
                        item.TotalNetWeight = item.Qty * item.NetWeight;
                        item.TotalGrossWeight = item.Qty * item.GrossWeight;
                        invoice.TotalQuantity = +item.Qty;

                        invoice.TotalNetPrice += item.UnitPrice * Convert.ToDecimal(item.Qty);

                        packingList.TotalNetPrice = decimal.Round(packingList.TotalNetPrice + item.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossPrice = decimal.Round(packingList.TotalGrossPrice + item.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.AccountingTotalGrossPrice =
                            decimal.Round(packingList.AccountingTotalGrossPrice + item.AccountingTotalGrossPrice, 2, MidpointRounding.AwayFromZero);
                        packingList.TotalNetWeight += Math.Round(item.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossWeight += Math.Round(item.TotalGrossWeight, 3, MidpointRounding.AwayFromZero);

                        item.UnitPrice = decimal.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                        item.UnitPriceEur = decimal.Round(item.UnitPriceEur, 2, MidpointRounding.AwayFromZero);
                        packingList.PackingListPackageOrderItems.Add(item);
                    }

                return invoice;
            };

            _connection.Query(
                ";WITH [TOTAL_VALUE_CTE] AS ( " +
                "SELECT " +
                "[SupplyInvoice].[ID] " +
                ",SUM( " +
                "CASE " +
                "WHEN [SupplyInvoiceBillOfLadingService].[Value] IS NULL " +
                "THEN 0 " +
                "ELSE dbo.GetGovExchangedToEuroValue([SupplyInvoiceBillOfLadingService].[Value], [AgreementBillOfLadingService].[CurrencyID], " +
                "CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [SupplyInvoice].[Created] " +
                "END) " +
                "END) + SUM( " +
                "CASE " +
                "WHEN [SupplyInvoiceMergedService].[Value] IS NULL " +
                "THEN 0 " +
                "ELSE dbo.GetGovExchangedToEuroValue([SupplyInvoiceMergedService].[Value], [AgreementMergedService].[CurrencyID], " +
                "CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [SupplyInvoice].[Created] " +
                "END) " +
                "END) AS [TotalSpending] " +
                ",SUM( " +
                "CASE " +
                "WHEN [SupplyInvoiceBillOfLadingService].[AccountingValue] IS NULL " +
                "THEN 0 " +
                "ELSE dbo.GetGovExchangedToEuroValue([SupplyInvoiceBillOfLadingService].[AccountingValue], [AgreementBillOfLadingService].[CurrencyID], " +
                "CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [SupplyInvoice].[Created] " +
                "END) " +
                "END) + SUM( " +
                "CASE " +
                "WHEN [SupplyInvoiceMergedService].[AccountingValue] IS NULL " +
                "THEN 0 " +
                "ELSE dbo.GetGovExchangedToEuroValue([SupplyInvoiceMergedService].[AccountingValue], [AgreementMergedService].[CurrencyID], " +
                "CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [SupplyInvoice].[Created] " +
                "END) " +
                "END) AS [AccountingTotalSpending] " +
                "FROM [SupplyInvoice] " +
                "LEFT JOIN [SupplyInvoiceBillOfLadingService] " +
                "ON [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] =  [SupplyInvoice].[ID] " +
                "AND [SupplyInvoiceBillOfLadingService].[Deleted] = 0 " +
                "LEFT JOIN [BillOfLadingService] " +
                "ON [BillOfLadingService].[ID] = [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] " +
                "AND [BillOfLadingService].[DeliveryProductProtocolID] = @Id " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [AgreementBillOfLadingService] " +
                "ON [AgreementBillOfLadingService].[ID] = [BillOfLadingService].[SupplyOrganizationAgreementID] " +
                "LEFT JOIN [SupplyInvoiceMergedService] " +
                "ON [SupplyInvoiceMergedService].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                "LEFT JOIN [MergedService] " +
                "ON [MergedService].[ID] = [SupplyInvoiceMergedService].[MergedServiceID] " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [AgreementMergedService] " +
                "ON [AgreementMergedService].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
                "AND [MergedService].[DeliveryProductProtocolID] = @Id " +
                "WHERE [SupplyInvoice].[DeliveryProductProtocolID] = @Id " +
                "AND [SupplyInvoice].[Deleted] = 0 " +
                "GROUP BY [SupplyInvoice].[ID] " +
                ") " +
                "SELECT " +
                "[SupplyInvoice].* " +
                ",[TOTAL_VALUE_CTE].[TotalSpending] " +
                ",[TOTAL_VALUE_CTE].[AccountingTotalSpending] " +
                ",[PackingList].* " +
                ",[PackingListPackageOrderItem].* " +
                ",[SupplyOrder].* " +
                ",[Client].* " +
                ",[ClientAgreement].* " +
                ",[Agreement].* " +
                ",[Currency].* " +
                ",[Organization].* " +
                ",[SupplyInvoiceDeliveryDocument].* " +
                ",[SupplyDeliveryDocument].* " +
                "FROM [SupplyInvoice] " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                "AND [PackingList].[Deleted] = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                "LEFT JOIN [Client] " +
                "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                "LEFT JOIN [SupplyInvoiceDeliveryDocument] " +
                "ON [SupplyInvoiceDeliveryDocument].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                "AND [SupplyInvoiceDeliveryDocument].[Deleted] = 0 " +
                "LEFT JOIN [SupplyDeliveryDocument] " +
                "ON [SupplyDeliveryDocument].[ID] = [SupplyInvoiceDeliveryDocument].[SupplyDeliveryDocumentID] " +
                "LEFT JOIN [TOTAL_VALUE_CTE] " +
                "ON [TOTAL_VALUE_CTE].[ID] = [SupplyInvoice].[ID] " +
                "WHERE [SupplyInvoice].[ID] IN (SELECT [TOTAL_VALUE_CTE].[ID] FROM [TOTAL_VALUE_CTE])",
                supplyInvoicesType, supplyInvoicesMapper,
                new {
                    toReturn.Id
                });

            foreach (SupplyInvoice supplyInvoice in toReturn.SupplyInvoices) {
                supplyInvoice.MergedSupplyInvoices =
                    _connection.Query<SupplyInvoice>(
                        "SELECT * " +
                        "FROM [SupplyInvoice] " +
                        "WHERE [SupplyInvoice].RootSupplyInvoiceId = @Id",
                        new { supplyInvoice.Id }).ToList();

                foreach (PackingList packingList in supplyInvoice.PackingLists)
                    packingList.MergedPackingLists =
                        _connection.Query<PackingList>(
                            "SELECT * " +
                            "FROM [PackingList] " +
                            "WHERE [PackingList].RootPackingListId = @Id",
                            new { packingList.Id }).ToList();
            }
        }

        bool hasMergedService = _connection.Query<bool>(
            "SELECT IIF(COUNT([MergedService].[ID]) > 0, 1,0) " +
            "FROM [MergedService] " +
            "WHERE [MergedService].[DeliveryProductProtocolID] = @Id " +
            "AND [MergedService].[Deleted] = 0",
            new { toReturn.Id }).Single();

        if (hasMergedService) {
            Type[] mergedServiceTypes = {
                typeof(MergedService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyInvoiceMergedService),
                typeof(SupplyInvoice),
                typeof(PackingList),
                typeof(PackingListPackageOrderItem),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(SupplyOrderNumber),
                typeof(Organization),
                typeof(SupplyProForm),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyPaymentTaskDocument),
                typeof(SupplyPaymentTaskDocument),
                typeof(SupplyInformationTask),
                typeof(User),
                typeof(User),
                typeof(ActProvidingServiceDocument),
                typeof(SupplyServiceAccountDocument),
                typeof(ConsumableProduct),
                typeof(ActProvidingService),
                typeof(ActProvidingService)
            };

            Func<object[], MergedService> mergedServiceMapper = objects => {
                MergedService service = (MergedService)objects[0];
                SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Currency currencySupplyOrganizationAgreement = (Currency)objects[3];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[4];
                User userSupplyPaymentTask = (User)objects[5];
                SupplyPaymentTask accountingPaymentTask = (SupplyPaymentTask)objects[6];
                User userAccountingPaymentTask = (User)objects[7];
                SupplyInvoiceMergedService supplyInvoiceMergedService = (SupplyInvoiceMergedService)objects[8];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
                PackingList packingList = (PackingList)objects[10];
                PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[11];
                SupplyOrder supplyOrder = (SupplyOrder)objects[12];
                Client client = (Client)objects[13];
                ClientAgreement clientAgreement = (ClientAgreement)objects[14];
                Agreement agreement = (Agreement)objects[15];
                Currency currency = (Currency)objects[16];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[17];
                Organization organization = (Organization)objects[18];
                SupplyProForm supplyProForm = (SupplyProForm)objects[19];
                InvoiceDocument document = (InvoiceDocument)objects[20];
                User serviceUser = (User)objects[21];
                SupplyPaymentTaskDocument supplyPaymentTaskDocument = (SupplyPaymentTaskDocument)objects[22];
                SupplyPaymentTaskDocument accountingPaymentTaskDocument = (SupplyPaymentTaskDocument)objects[23];
                SupplyInformationTask supplyInformationTask = (SupplyInformationTask)objects[24];
                User userSupplyInformationTask = (User)objects[25];
                User updatedSupplyInformationTask = (User)objects[26];
                ActProvidingServiceDocument actProvidingServiceDocument = (ActProvidingServiceDocument)objects[27];
                SupplyServiceAccountDocument supplyServiceAccountDocument = (SupplyServiceAccountDocument)objects[28];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[29];
                ActProvidingService actProvidingService = (ActProvidingService)objects[30];
                ActProvidingService accountingActProvidingService = (ActProvidingService)objects[31];

                if (!toReturn.MergedServices.Any(x => x.Id.Equals(service.Id)))
                    toReturn.MergedServices.Add(service);
                else
                    service = toReturn.MergedServices.First(x => x.Id.Equals(service.Id));

                if (supplyInformationTask != null) {
                    supplyInformationTask.UpdatedBy = updatedSupplyInformationTask;
                    supplyInformationTask.User = userSupplyInformationTask;
                    service.SupplyInformationTask = supplyInformationTask;
                }

                service.ActProvidingService = actProvidingService;
                service.AccountingActProvidingService = accountingActProvidingService;
                service.ConsumableProduct = consumableProduct;
                service.SupplyOrganization = supplyOrganization;
                supplyOrganizationAgreement.Currency = currencySupplyOrganizationAgreement;
                service.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                service.ActProvidingServiceDocument = actProvidingServiceDocument;
                service.SupplyServiceAccountDocument = supplyServiceAccountDocument;
                service.User = serviceUser;

                if (service.SupplyPaymentTask == null)
                    service.SupplyPaymentTask = supplyPaymentTask;
                else
                    supplyPaymentTask = service.SupplyPaymentTask;

                if (supplyPaymentTask != null) {
                    supplyPaymentTask.User = userSupplyPaymentTask;

                    service.SupplyPaymentTask = supplyPaymentTask;

                    if (supplyPaymentTaskDocument != null)
                        if (!supplyPaymentTask.SupplyPaymentTaskDocuments.Any(x => x.Id.Equals(supplyPaymentTaskDocument.Id)))
                            supplyPaymentTask.SupplyPaymentTaskDocuments.Add(supplyPaymentTaskDocument);
                }

                if (service.AccountingPaymentTask == null)
                    service.AccountingPaymentTask = accountingPaymentTask;
                else
                    accountingPaymentTask = service.AccountingPaymentTask;

                if (accountingPaymentTask != null) {
                    accountingPaymentTask.User = userAccountingPaymentTask;

                    service.AccountingPaymentTask = accountingPaymentTask;

                    if (accountingPaymentTaskDocument != null)
                        if (!accountingPaymentTask.SupplyPaymentTaskDocuments.Any(x => x.Id.Equals(accountingPaymentTaskDocument.Id)))
                            accountingPaymentTask.SupplyPaymentTaskDocuments.Add(accountingPaymentTaskDocument);
                }

                if (supplyPaymentTask != null)
                    supplyPaymentTask.User = userSupplyPaymentTask;

                service.SupplyPaymentTask = supplyPaymentTask;

                if (accountingPaymentTask != null)
                    accountingPaymentTask.User = userAccountingPaymentTask;

                service.AccountingPaymentTask = accountingPaymentTask;

                if (document != null) {
                    if (!service.InvoiceDocuments.Any(x => x.Id.Equals(document.Id)))
                        service.InvoiceDocuments.Add(document);
                    else
                        document = service.InvoiceDocuments.FirstOrDefault(x => x.Id.Equals(document.Id));
                }

                if (supplyInvoiceMergedService == null || supplyInvoice == null) return service;

                if (!service.SupplyInvoiceMergedServices.Any(x => x.Id.Equals(supplyInvoiceMergedService.Id)))
                    service.SupplyInvoiceMergedServices.Add(supplyInvoiceMergedService);
                else
                    supplyInvoiceMergedService = service.SupplyInvoiceMergedServices.First(x => x.Id.Equals(supplyInvoiceMergedService.Id));

                supplyInvoiceMergedService.SupplyInvoice = supplyInvoice;
                supplyInvoice.SupplyOrder = supplyOrder;
                supplyOrder.Client = client;
                agreement.Currency = currency;
                clientAgreement.Agreement = agreement;
                supplyOrder.ClientAgreement = clientAgreement;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyProForm = supplyProForm;

                if (packingList == null) return service;

                if (!supplyInvoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                    supplyInvoice.PackingLists.Add(packingList);
                else
                    packingList = supplyInvoice.PackingLists.First(x => x.Id.Equals(packingList.Id));

                packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);

                return service;
            };

            _connection.Query(
                "DECLARE @UAH_ID bigint = ( " +
                "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
                "WHERE [Currency].[Deleted] = 0 " +
                "AND [Currency].[Code] = 'UAH' " +
                "); " +
                "SELECT [MergedService].* " +
                ", [SupplyOrganization].* " +
                ", [SupplyOrganizationAgreement].* " +
                ", [CurrencySupplyOrganizationAgreement].* " +
                ", [SupplyPaymentTask].* " +
                ", [UserSupplyPayment].* " +
                ", [AccountingPaymentTask].* " +
                ", [UserAccountingPaymentTask].* " +
                ", [SupplyInvoiceMergedService].* " +
                ", [SupplyInvoice].* " +
                ", ( " +
                "SELECT " +
                "CASE " +
                "WHEN( " +
                "SELECT TOP 1 ( " +
                "CASE " +
                "WHEN [GovExchangeRateHistory].Amount IS NULL " +
                "THEN [GovExchangeRate].[Amount] " +
                "ELSE [GovExchangeRateHistory].Amount " +
                "END " +
                ") AS [Amount] " +
                "FROM [GovExchangeRateHistory] " +
                "LEFT JOIN [GovExchangeRate] " +
                "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
                "WHERE [GovExchangeRate].[Code] = [CurrencySupplyOrganizationAgreement].[Code] " +
                "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
                "AND CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [MergedService].[Created] " +
                "END > [GovExchangeRateHistory].[Created] " +
                "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
                ") IS NOT NULL " +
                "THEN ( " +
                "SELECT TOP 1 ( " +
                "CASE " +
                "WHEN [GovExchangeRateHistory].Amount IS NULL " +
                "THEN [GovExchangeRate].[Amount] " +
                "ELSE [GovExchangeRateHistory].Amount " +
                "END " +
                ") AS [Amount] " +
                "FROM [GovExchangeRateHistory] " +
                "LEFT JOIN [GovExchangeRate] " +
                "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
                "WHERE [GovExchangeRate].[Code] = [CurrencySupplyOrganizationAgreement].[Code] " +
                "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
                "AND CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [MergedService].[Created] " +
                "END > [GovExchangeRateHistory].[Created] " +
                "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
                ") " +
                "ELSE ( " +
                "SELECT TOP 1 [GovExchangeRate].[Amount] " +
                "FROM [GovExchangeRate] " +
                "WHERE [GovExchangeRate].[Code] = [CurrencySupplyOrganizationAgreement].[Code] " +
                "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
                ") " +
                "END " +
                ") AS [ExchangeRate] " +
                ", ( " +
                "SELECT " +
                "CASE " +
                "WHEN( " +
                "SELECT TOP 1 ( " +
                "CASE " +
                "WHEN [GovExchangeRateHistory].Amount IS NULL " +
                "THEN [GovExchangeRate].[Amount] " +
                "ELSE [GovExchangeRateHistory].Amount " +
                "END " +
                ") AS [Amount] " +
                "FROM [GovExchangeRateHistory] " +
                "LEFT JOIN [GovExchangeRate] " +
                "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
                "WHERE [GovExchangeRate].[Code] = 'EUR' " +
                "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
                "AND CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [MergedService].[Created] " +
                "END > [GovExchangeRateHistory].[Created] " +
                "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
                ") IS NOT NULL " +
                "THEN ( " +
                "SELECT TOP 1 ( " +
                "CASE " +
                "WHEN [GovExchangeRateHistory].Amount IS NULL " +
                "THEN [GovExchangeRate].[Amount] " +
                "ELSE [GovExchangeRateHistory].Amount " +
                "END " +
                ") AS [Amount] " +
                "FROM [GovExchangeRateHistory] " +
                "LEFT JOIN [GovExchangeRate] " +
                "ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID] " +
                "WHERE [GovExchangeRate].[Code] = 'EUR' " +
                "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
                "AND CASE " +
                "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                "THEN  [SupplyInvoice].[DateCustomDeclaration] " +
                "ELSE [MergedService].[Created] " +
                "END > [GovExchangeRateHistory].[Created] " +
                "ORDER BY [GovExchangeRateHistory].[Created] DESC " +
                ") " +
                "ELSE ( " +
                "SELECT TOP 1 [GovExchangeRate].[Amount] " +
                "FROM [GovExchangeRate] " +
                "WHERE [GovExchangeRate].[Code] = 'EUR' " +
                "AND [GovExchangeRate].[CurrencyID] = @UAH_ID " +
                ") " +
                "END " +
                ") AS [ExchangeRateEurToUah] " +
                ", [PackingList].* " +
                ", [PackingListPackageOrderItem].* " +
                ", [SupplyOrder].* " +
                ", [Client].* " +
                ", [ClientAgreement].* " +
                ", [Agreement].* " +
                ", [Currency].* " +
                ", [SupplyOrderNumber].* " +
                ", [Organization].* " +
                ", [SupplyProForm].* " +
                ", [InvoiceDocument].* " +
                ", [User].* " +
                ", [SupplyPaymentTaskDocument].* " +
                ", [AccountingPaymentTaskDocument].* " +
                ", [SupplyInformationTask].* " +
                ", [UserSupplyInformationTask].* " +
                ", [UpdatedSupplyInformationTask].* " +
                ", [ActProvidingServiceDocument].* " +
                ", [SupplyServiceAccountDocument].* " +
                ", [ConsumableProduct].* " +
                ", [ActProvidingService].* " +
                ", [AccountingActProvidingService].* " +
                "FROM [MergedService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].[ID] = [MergedService].[SupplyOrganizationID] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
                "LEFT JOIN [Currency] AS [CurrencySupplyOrganizationAgreement] " +
                "ON [CurrencySupplyOrganizationAgreement].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].[ID] = [MergedService].[SupplyPaymentTaskID] " +
                "LEFT JOIN [User] AS [UserSupplyPayment] " +
                "ON [UserSupplyPayment].[ID] = [SupplyPaymentTask].[UserID] " +
                "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                "ON [AccountingPaymentTask].[ID] = [MergedService].[AccountingPaymentTaskID] " +
                "LEFT JOIN [User] AS [UserAccountingPaymentTask] " +
                "ON [UserAccountingPaymentTask].[ID] = [AccountingPaymentTask].[UserID] " +
                "LEFT JOIN [SupplyInvoiceMergedService] " +
                "ON [SupplyInvoiceMergedService].[MergedServiceID] = [MergedService].[ID] " +
                "AND [SupplyInvoiceMergedService].[Deleted] = 0 " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [SupplyInvoiceMergedService].[SupplyInvoiceID] " +
                "AND [SupplyInvoice].[Deleted] = 0 " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                "AND [PackingList].[Deleted] = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                "LEFT JOIN [Client] " +
                "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].[ID] = [SupplyOrder].[SupplyProFormID] " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].[MergedServiceID] = [MergedService].[ID] " +
                "AND [InvoiceDocument].[Deleted] = 0 " +
                "LEFT JOIN [User] " +
                "ON [User].[ID] = [MergedService].[UserID] " +
                "LEFT JOIN [SupplyPaymentTaskDocument] " +
                "ON [SupplyPaymentTaskDocument].[SupplyPaymentTaskID] = [SupplyPaymentTask].[ID] " +
                "AND [SupplyPaymentTaskDocument].[Deleted] = 0 " +
                "LEFT JOIN [SupplyPaymentTaskDocument] AS [AccountingPaymentTaskDocument] " +
                "ON [AccountingPaymentTaskDocument].[SupplyPaymentTaskID] = [AccountingPaymentTask].[ID] " +
                "AND [AccountingPaymentTaskDocument].[Deleted] = 0 " +
                "LEFT JOIN [SupplyInformationTask] " +
                "ON [SupplyInformationTask].[ID] = [MergedService].[SupplyInformationTaskID] " +
                "AND [SupplyInformationTask].[Deleted] = 0 " +
                "LEFT JOIN [User] AS [UserSupplyInformationTask] " +
                "ON [UserSupplyInformationTask].[ID] = [SupplyInformationTask].[UserID] " +
                "LEFT JOIN [User] AS [UpdatedSupplyInformationTask] " +
                "ON [UpdatedSupplyInformationTask].[ID] = [SupplyInformationTask].[UpdatedByID] " +
                "LEFT JOIN [ActProvidingServiceDocument] " +
                "ON [ActProvidingServiceDocument].[ID] = [MergedService].[ActProvidingServiceDocumentID] " +
                "LEFT JOIN [SupplyServiceAccountDocument] " +
                "ON [SupplyServiceAccountDocument].[ID] = [MergedService].[SupplyServiceAccountDocumentID] " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                "LEFT JOIN [ActProvidingService] AS [AccountingActProvidingService] " +
                "ON [AccountingActProvidingService].[ID] = [MergedService].[AccountingActProvidingServiceID] " +
                "WHERE [MergedService].[DeliveryProductProtocolID] = @Id " +
                "AND [MergedService].[Deleted] = 0; ",
                mergedServiceTypes, mergedServiceMapper, new { toReturn.Id }, commandTimeout: 3600);
        }

        return toReturn;
    }

    public long GetIdByNetId(Guid netId) {
        return _connection.Query<long>(
            "SELECT [DeliveryProductProtocol].[Id] " +
            "FROM [DeliveryProductProtocol] " +
            "WHERE [DeliveryProductProtocol].[NetUID] = @NetId; ",
            new { NetId = netId }).Single();
    }

    public GetAllFilteredWithTotalsModel AllFiltered(DateTime from, DateTime to, string organizationName, string supplierName, int limit, int offset) {
        GetAllFilteredWithTotalsModel toReturn = new();

        List<DeliveryProductProtocol> protocols = new();

        toReturn.TotalQty =
            _connection.Query<double>(
                "SELECT  COUNT(1) FROM [DeliveryProductProtocol] " +
                "WHERE [DeliveryProductProtocol].[Deleted] = 0 " +
                "AND [DeliveryProductProtocol].[Created] >= @From " +
                "AND [DeliveryProductProtocol].[Created] <= @To ",
                new { From = from, To = to }).FirstOrDefault();

        Type[] types = {
            typeof(DeliveryProductProtocol),
            typeof(User),
            typeof(Organization),
            typeof(DeliveryProductProtocolNumber),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client)
        };

        Func<object[], DeliveryProductProtocol> mapper = objects => {
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[0];
            User user = (User)objects[1];
            Organization organization = (Organization)objects[2];
            DeliveryProductProtocolNumber deliveryProductProtocolNumber = (DeliveryProductProtocolNumber)objects[3];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
            SupplyOrder supplyOrder = (SupplyOrder)objects[5];
            Client client = (Client)objects[6];

            if (!protocols.Any(x => x.Id.Equals(protocol.Id)))
                protocols.Add(protocol);
            else
                protocol = protocols.First(x => x.Id.Equals(protocol.Id));

            protocol.User = user;

            protocol.Organization = organization;

            protocol.DeliveryProductProtocolNumber = deliveryProductProtocolNumber;

            if (supplyInvoice == null) return protocol;

            if (!protocol.SupplyInvoices.Any(x => x.SupplyOrder.Client.Id.Equals(client.Id))) {
                supplyOrder.Client = client;
                supplyInvoice.SupplyOrder = supplyOrder;
                protocol.SupplyInvoices.Add(supplyInvoice);
            }

            return protocol;
        };

        string sqlQuery = ";WITH [FILTERED_CTE] AS ( " +
                          "SELECT ROW_NUMBER() OVER (ORDER BY [DeliveryProductProtocol].[ID] DESC) AS [RowNumber] " +
                          ",[DeliveryProductProtocol].[ID] " +
                          ",COUNT([SupplyInvoice].ID) AS QtyInvoices " +
                          ",ROUND(SUM([SupplyInvoice].[NetPrice]), 2) AS TotalValue " +
                          "FROM [DeliveryProductProtocol] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[DeliveryProductProtocolID] = [DeliveryProductProtocol].[ID] " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [DeliveryProductProtocol].[OrganizationID] " +
                          "WHERE [DeliveryProductProtocol].[Deleted] = 0 " +
                          "AND [DeliveryProductProtocol].[FromDate] >= @From " +
                          "AND [DeliveryProductProtocol].[FromDate] <= @To " +
                          "AND [Organization].[Name] LIKE N'%' + @OrganizationName + N'%' " +
                          "AND [Client].FullName LIKE N'%' + @Name + N'%' " +
                          "GROUP BY [DeliveryProductProtocol].[ID] " +
                          ") " +
                          "SELECT [DeliveryProductProtocol].* " +
                          ",[FILTERED_CTE].QtyInvoices " +
                          ",[FILTERED_CTE].TotalValue " +
                          ",[User].* " +
                          ",[Organization].* " +
                          ",[DeliveryProductProtocolNumber].* " +
                          ",[SupplyInvoice].* " +
                          ",[SupplyOrder].* " +
                          ",[Client].* " +
                          "FROM [DeliveryProductProtocol] " +
                          "LEFT JOIN [User] " +
                          "ON [User].[ID] = [DeliveryProductProtocol].[UserID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [DeliveryProductProtocol].[OrganizationID] " +
                          "LEFT JOIN [DeliveryProductProtocolNumber] " +
                          "ON [DeliveryProductProtocolNumber].[ID] = [DeliveryProductProtocol].[DeliveryProductProtocolNumberID] " +
                          "LEFT JOIN [FILTERED_CTE] " +
                          "ON [FILTERED_CTE].[ID] = [DeliveryProductProtocol].[ID] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[DeliveryProductProtocolID] = [DeliveryProductProtocol].[ID] " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "WHERE [DeliveryProductProtocol].[ID] IN ( " +
                          "SELECT [FILTERED_CTE].[ID] " +
                          "FROM [FILTERED_CTE] " +
                          "WHERE [FILTERED_CTE].[RowNumber] > @Offset " +
                          "AND [FILTERED_CTE].[RowNumber] <= @Limit + @Offset) " +
                          "ORDER BY [DeliveryProductProtocol].[Created] DESC; ";

        _connection.Query(sqlQuery, types, mapper,
            new { From = from, To = to, OrganizationName = organizationName, Name = supplierName, Limit = limit, Offset = offset });

        toReturn.DeliveryProductProtocols = protocols;

        return toReturn;
    }

    public List<DeliveryProductProtocol> AllFiltered(DateTime from, DateTime to) {
        List<DeliveryProductProtocol> protocols = new();

        Type[] types = {
            typeof(DeliveryProductProtocol),
            typeof(User),
            typeof(Organization),
            typeof(DeliveryProductProtocolNumber),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement)
        };

        Func<object[], DeliveryProductProtocol> mapper = objects => {
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[0];
            User user = (User)objects[1];
            Organization organization = (Organization)objects[2];
            DeliveryProductProtocolNumber deliveryProductProtocolNumber = (DeliveryProductProtocolNumber)objects[3];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
            SupplyOrder supplyOrder = (SupplyOrder)objects[5];
            Client client = (Client)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Agreement agreement = (Agreement)objects[8];

            if (!protocols.Any(x => x.Id.Equals(protocol.Id)))
                protocols.Add(protocol);
            else
                protocol = protocols.First(x => x.Id.Equals(protocol.Id));

            protocol.User = user;

            protocol.Organization = organization;

            protocol.DeliveryProductProtocolNumber = deliveryProductProtocolNumber;

            if (supplyInvoice == null) return protocol;

            if (!protocol.SupplyInvoices.Any(x => x.SupplyOrder.Client.Id.Equals(client.Id))) {
                supplyOrder.Client = client;
                clientAgreement.Agreement = agreement;
                supplyOrder.ClientAgreement = clientAgreement;
                supplyInvoice.SupplyOrder = supplyOrder;
                protocol.SupplyInvoices.Add(supplyInvoice);
            }

            return protocol;
        };

        string sqlQuery = "SELECT [DeliveryProductProtocol].* " +
                          ",[User].* " +
                          ",[Organization].* " +
                          ",[DeliveryProductProtocolNumber].* " +
                          ",[SupplyInvoice].* " +
                          ",[SupplyOrder].* " +
                          ",[Client].* " +
                          ",[ClientAgreement].* " +
                          ",[Agreement].* " +
                          "FROM [DeliveryProductProtocol] " +
                          "LEFT JOIN [User] " +
                          "ON [User].[ID] = [DeliveryProductProtocol].[UserID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [DeliveryProductProtocol].[OrganizationID] " +
                          "LEFT JOIN [DeliveryProductProtocolNumber] " +
                          "ON [DeliveryProductProtocolNumber].[ID] = [DeliveryProductProtocol].[DeliveryProductProtocolNumberID] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[DeliveryProductProtocolID] = [DeliveryProductProtocol].[ID] " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "WHERE [DeliveryProductProtocol].[Deleted] = 0 " +
                          "AND [DeliveryProductProtocol].[FromDate] >= @From " +
                          "AND [DeliveryProductProtocol].[FromDate] <= @To ";

        _connection.Query(sqlQuery, types, mapper,
            new { From = from, To = to });

        return protocols;
    }

    public void RemoveById(long id) {
        _connection.Query(
            "UPDATE [DeliveryProductProtocol] " +
            "SET [Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [DeliveryProductProtocol].[ID] = @Id; ",
            new { Id = id });
    }

    public SupplyTransportationType GetTransportationTypeById(long id) {
        return _connection.Query<SupplyTransportationType>(
            "SELECT [DeliveryProductProtocol].[TransportationType] " +
            "FROM [DeliveryProductProtocol] " +
            "WHERE [DeliveryProductProtocol].[ID] = @Id; ",
            new { Id = id }).Single();
    }

    public void SetFullyAndPartialPlacedPlaced(DeliveryProductProtocol protocol) {
        _connection.Execute(
            "UPDATE [DeliveryProductProtocol] " +
            "SET [IsPartiallyPlaced] = @IsPartiallyPlaced " +
            ",[IsPlaced] = @IsPlaced " +
            "WHERE [DeliveryProductProtocol].[ID] = @Id; ",
            protocol);
    }

    public Guid GetNetIdById(long id) {
        return _connection.Query<Guid>(
            "SELECT [DeliveryProductProtocol].[NetUID] " +
            "FROM [DeliveryProductProtocol] " +
            "WHERE [DeliveryProductProtocol].[ID] = @Id; ",
            new { Id = id }).Single();
    }

    public void UpdateIsShippedByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [DeliveryProductProtocol] " +
            "SET [IsShipped] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [DeliveryProductProtocol].[NetUID] = @NetId; ",
            new { NetId = netId });
    }

    public void UpdateIsCompletedByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [DeliveryProductProtocol] " +
            "SET [IsCompleted] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [DeliveryProductProtocol].[NetUID] = @NetId; ",
            new { NetId = netId });
    }

    public DeliveryProductProtocol GetWithoutIncludesByNetId(Guid netId) {
        return _connection.Query<DeliveryProductProtocol>(
            "SELECT * FROM [DeliveryProductProtocol] " +
            "WHERE [DeliveryProductProtocol].[NetUID] = @NetId; ",
            new { NetId = netId }).FirstOrDefault();
    }

    public object GetAllFilteredForPrinting(DateTime from, DateTime to) {
        List<DeliveryProductProtocol> toReturn = new();

        Type[] types = {
            typeof(DeliveryProductProtocol),
            typeof(User),
            typeof(Organization),
            typeof(DeliveryProductProtocolNumber),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client)
        };

        Func<object[], DeliveryProductProtocol> mapper = objects => {
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[0];
            User user = (User)objects[1];
            Organization organization = (Organization)objects[2];
            DeliveryProductProtocolNumber deliveryProductProtocolNumber = (DeliveryProductProtocolNumber)objects[3];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
            SupplyOrder supplyOrder = (SupplyOrder)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(x => x.Id.Equals(protocol.Id)))
                toReturn.Add(protocol);
            else
                protocol = toReturn.First(x => x.Id.Equals(protocol.Id));

            protocol.User = user;

            protocol.Organization = organization;

            protocol.DeliveryProductProtocolNumber = deliveryProductProtocolNumber;

            if (supplyInvoice == null) return protocol;

            if (!protocol.SupplyInvoices.Any(x => x.SupplyOrder.Client.Id.Equals(client.Id))) {
                supplyOrder.Client = client;
                supplyInvoice.SupplyOrder = supplyOrder;
                protocol.SupplyInvoices.Add(supplyInvoice);
            }

            return protocol;
        };

        _connection.Query(";WITH [FILTERED_CTE] AS ( " +
                          "SELECT ROW_NUMBER() OVER (ORDER BY [DeliveryProductProtocol].[ID] DESC) AS [RowNumber] " +
                          ",[DeliveryProductProtocol].[ID] " +
                          ",COUNT([SupplyInvoice].ID) AS QtyInvoices " +
                          ",ROUND(SUM([SupplyInvoice].[NetPrice]), 2) AS TotalValue " +
                          "FROM [DeliveryProductProtocol] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[DeliveryProductProtocolID] = [DeliveryProductProtocol].[ID] " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "WHERE [DeliveryProductProtocol].[Deleted] = 0 " +
                          "AND [DeliveryProductProtocol].[Created] >= @From " +
                          "AND [DeliveryProductProtocol].[Created] <= @To " +
                          "GROUP BY [DeliveryProductProtocol].[ID] " +
                          ") " +
                          "SELECT [DeliveryProductProtocol].* " +
                          ",[FILTERED_CTE].QtyInvoices " +
                          ",[FILTERED_CTE].TotalValue " +
                          ",[User].* " +
                          ",[Organization].* " +
                          ",[DeliveryProductProtocolNumber].* " +
                          ",[SupplyInvoice].* " +
                          ",[SupplyOrder].* " +
                          ",[Client].* " +
                          "FROM [DeliveryProductProtocol] " +
                          "LEFT JOIN [User] " +
                          "ON [User].[ID] = [DeliveryProductProtocol].[UserID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [DeliveryProductProtocol].[OrganizationID] " +
                          "LEFT JOIN [DeliveryProductProtocolNumber] " +
                          "ON [DeliveryProductProtocolNumber].[ID] = [DeliveryProductProtocol].[DeliveryProductProtocolNumberID] " +
                          "LEFT JOIN [FILTERED_CTE] " +
                          "ON [FILTERED_CTE].[ID] = [DeliveryProductProtocol].[ID] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[DeliveryProductProtocolID] = [DeliveryProductProtocol].[ID] " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "WHERE [DeliveryProductProtocol].[ID] IN ( " +
                          "SELECT [FILTERED_CTE].[ID] " +
                          "FROM [FILTERED_CTE] " +
                          "WHERE [FILTERED_CTE].[RowNumber] > @Offset " +
                          "AND [FILTERED_CTE].[RowNumber] <= @Limit + @Offset) " +
                          "ORDER BY [DeliveryProductProtocol].[Created] DESC; ", types, mapper,
            new { From = from, To = to, Limit = int.MaxValue, Offset = 0 });

        return toReturn;
    }
}