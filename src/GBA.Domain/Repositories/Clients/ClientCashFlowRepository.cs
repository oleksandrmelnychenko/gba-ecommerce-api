using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientCashFlowRepository : IClientCashFlowRepository {
    private readonly IDbConnection _connection;

    public ClientCashFlowRepository(IDbConnection connection) {
        _connection = connection;
    }

    public AccountingCashFlow GetRangedBySupplier(Client client, DateTime from, DateTime to) {
        AccountingCashFlow accountingCashFlow = new(client);

        accountingCashFlow.BeforeRangeInAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT 0 - ISNULL(SUM(dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice, [Agreement].CurrencyID, [SupplyInvoice].DateFrom)), 0) " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrder].ClientID = @Id " +
                "AND [SupplyInvoice].DateFrom < @From " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                ") " +
                "+ " +
                "(" +
                "SELECT 0 - ISNULL(SUM(dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice, [Agreement].CurrencyID, [SupplyOrderUkraine].FromDate)), 0) " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrderUkraine].SupplierID = @Id " +
                "AND [SupplyOrderUkraine].FromDate < @From " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "AND [SupplyOrderUkraine].[IsPartialPlaced] = 1 " +
                ")" +
                ", 2) AS [BeforeRangeInAmount]",
                new { client.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeOutAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT " +
                "ISNULL( " +
                "SUM([OutcomePaymentOrder].AfterExchangeAmount) " +
                ", 0) " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [ClientAgreement].ClientID = @Id " +
                "AND [OutcomePaymentOrder].FromDate < @From " +
                ") " +
                ", 2) AS [BeforeRangeOutAmount] ",
                new { client.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        IEnumerable<JoinService> joinServices =
            _connection.Query<JoinService, decimal, JoinService>(
                ";WITH [AccountingCashFlow_CTE] " +
                "AS " +
                "( " +
                "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                ", 11 AS [Type] " +
                ", [OutcomePaymentOrder].FromDate AS [FromDate] " +
                ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "WHERE [ClientAgreement].ClientID = @Id " +
                "AND [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].FromDate >= @From " +
                "AND [OutcomePaymentOrder].FromDate <= @To " +
                "UNION " +
                "SELECT [SupplyOrderPaymentDeliveryProtocol].ID AS [ID] " +
                ", 0 AS [Type] " +
                ", [SupplyInvoice].DateFrom AS [FromDate] " +
                ", dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice, [Agreement].CurrencyID, [SupplyInvoice].DateFrom) AS [GrossPrice] " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrder].ClientID = @Id " +
                "AND [SupplyInvoice].DateFrom >= @From " +
                "AND [SupplyInvoice].DateFrom <= @To " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "UNION " +
                "SELECT [SupplyOrderPaymentDeliveryProtocol].ID AS [ID] " +
                ", 0 AS [Type] " +
                ", ISNULL([SupplyProForm].DateFrom, [SupplyProForm].Created) AS [FromDate] " +
                ", dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice,[Agreement].CurrencyID,ISNULL([SupplyProForm].DateFrom,[SupplyProForm].Created)) AS [GrossPrice] " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrder].ClientID = @Id " +
                "AND ISNULL([SupplyProForm].DateFrom, [SupplyProForm].Created) >= @From " +
                "AND ISNULL([SupplyProForm].DateFrom, [SupplyProForm].Created) <= @To " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "UNION " +
                "SELECT [SupplyOrderUkrainePaymentDeliveryProtocol].ID AS [ID] " +
                ", 18 AS [Type] " +
                ", [SupplyOrderUkraine].FromDate AS [FromDate] " +
                ", dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice, [Agreement].CurrencyID, [SupplyOrderUkraine].FromDate) AS [GrossPrice] " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrderUkraine].SupplierID = @Id " +
                "AND [SupplyOrderUkraine].FromDate >= @From " +
                "AND [SupplyOrderUkraine].FromDate <= @To " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "AND [SupplyOrderUkraine].[IsPartialPlaced] = 1 " +
                ") " +
                "SELECT * " +
                "FROM [AccountingCashFlow_CTE] " +
                "ORDER BY [AccountingCashFlow_CTE].FromDate",
                (service, grossPrice) => {
                    switch (service.Type) {
                        case JoinServiceType.OutcomePaymentOrder:
                            currentStepBalance = Math.Round(currentStepBalance - grossPrice, 2);

                            accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol:
                        case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                            currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                            accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.ContainerService:
                        case JoinServiceType.CustomService:
                        case JoinServiceType.PortWorkService:
                        case JoinServiceType.TransportationService:
                        case JoinServiceType.PortCustomAgencyService:
                        case JoinServiceType.CustomAgencyService:
                        case JoinServiceType.PlaneDeliveryService:
                        case JoinServiceType.VehicleDeliveryService:
                        case JoinServiceType.ConsumablesOrder:
                        case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                        default:
                            break;
                    }

                    return service;
                },
                new { client.Id, From = from, To = to },
                splitOn: "ID,GrossPrice"
            );

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderPaymentDeliveryProtocol),
                typeof(SupplyOrderPaymentDeliveryProtocolKey),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyProForm),
                typeof(ProFormDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization)
            };

            Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask) objects[2];
                User user = (User)objects[3];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                Client invoiceClient = (Client)objects[6];
                Region invoiceClientRegion = (Region)objects[7];
                RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                Country invoiceClientCountry = (Country)objects[9];
                ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                Agreement invoiceClientAgreement = (Agreement)objects[19];
                ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                Pricing invoiceClientPricing = (Pricing)objects[22];
                PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                Organization invoiceOrganization = (Organization)objects[26];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                Client proFormClient = (Client)objects[35];
                Region proFormClientRegion = (Region)objects[36];
                RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                Country proFormClientCountry = (Country)objects[38];
                ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                Agreement proFormClientAgreement = (Agreement)objects[48];
                ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                Pricing proFormClientPricing = (Pricing)objects[51];
                PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                Organization proFormClientAgreementOrganization = (Organization)objects[53];
                Currency proFormClientAgreementCurrency = (Currency)objects[54];
                Organization proFormOrganization = (Organization)objects[55];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol) && i.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                if (itemFromList.SupplyOrderPaymentDeliveryProtocol != null) {
                    if (supplyInvoice != null) {
                        if (invoiceDocument != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }
                    }

                    if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                    if (proFormDocument != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormSupplyOrder == null ||
                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                        return supplyOrderPaymentDeliveryProtocol;

                    proFormSupplyOrder.Client = proFormClient;
                    proFormSupplyOrder.Organization = proFormOrganization;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                } else {
                    if (supplyInvoice != null) {
                        if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }

                        if (invoiceClient != null) {
                            if (invoiceClientBankDetails != null) {
                                if (invoiceClientBankDetailAccountNumber != null) {
                                    invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                    invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                }

                                if (invoiceClientClientBankDetailIbanNo != null) {
                                    invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                    invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                }
                            }

                            if (invoiceClientClientAgreement != null) {
                                if (invoiceClientProviderPricing != null) {
                                    if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                    invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                    invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                }

                                invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                            }

                            invoiceClient.Region = invoiceClientRegion;
                            invoiceClient.RegionCode = invoiceClientRegionCode;
                            invoiceClient.Country = invoiceClientCountry;
                            invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                            invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                            invoiceClient.PackingMarking = invoiceClientPackingMarking;
                            invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                        }

                        invoiceSupplyOrder.Client = invoiceClient;
                        invoiceSupplyOrder.Organization = invoiceOrganization;

                        supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                    }

                    if (supplyProForm != null) {
                        if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder != null) {
                            if (proFormClient != null) {
                                if (proFormClientBankDetails != null) {
                                    if (proFormClientBankDetailAccountNumber != null) {
                                        proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                        proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                    }

                                    if (proFormClientClientBankDetailIbanNo != null) {
                                        proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                        proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                    }
                                }

                                if (proFormClientClientAgreement != null) {
                                    if (proFormClientProviderPricing != null) {
                                        if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                        proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                        proFormClientProviderPricing.Pricing = proFormClientPricing;
                                    }

                                    proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                    proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                    proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                    proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                    proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                }

                                proFormClient.Region = proFormClientRegion;
                                proFormClient.RegionCode = proFormClientRegionCode;
                                proFormClient.Country = proFormClientCountry;
                                proFormClient.ClientBankDetails = proFormClientBankDetails;
                                proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                proFormClient.PackingMarking = proFormClientPackingMarking;
                                proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                            }

                            proFormSupplyOrder.Client = proFormClient;
                            proFormSupplyOrder.Organization = proFormOrganization;

                            supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                        }
                    }

                    supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                    supplyOrderPaymentDeliveryProtocol.User = user;
                    supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                    supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol = supplyOrderPaymentDeliveryProtocol;
                }

                return supplyOrderPaymentDeliveryProtocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrderOrganization].OrganizationID " +
                "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                "LEFT JOIN [ProFormDocument] " +
                "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                "WHERE [SupplyOrderPaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            List<long> taskIds = new();

            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[ExchangeRate]" +
                ",[OutcomePaymentOrder].[IsAccounting]" +
                ",[OutcomePaymentOrder].[IsManagementAccounting]" +
                ",[OutcomePaymentOrder].[VAT] " +
                ",[OutcomePaymentOrder].[VatPercent] " +
                ",[dbo].[GetGovExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "[OutcomePaymentOrder].[FromDate] " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;

                    taskIds.Add(supplyPaymentTask.Id);
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);

                    taskIds.Add(supplyPaymentTask.Id);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);

            if (taskIds.Any()) {
                Type[] includesTypes = {
                    typeof(SupplyOrderPaymentDeliveryProtocol),
                    typeof(SupplyOrderPaymentDeliveryProtocolKey),
                    typeof(SupplyPaymentTask),
                    typeof(User),
                    typeof(SupplyInvoice),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization),
                    typeof(InvoiceDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyProForm),
                    typeof(ProFormDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization)
                };

                Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                    SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                    SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                    SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                    User user = (User)objects[3];
                    SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                    SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                    Client invoiceClient = (Client)objects[6];
                    Region invoiceClientRegion = (Region)objects[7];
                    RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                    Country invoiceClientCountry = (Country)objects[9];
                    ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                    ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                    Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                    ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                    Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                    TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                    PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                    PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                    ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                    Agreement invoiceClientAgreement = (Agreement)objects[19];
                    ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                    Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                    Pricing invoiceClientPricing = (Pricing)objects[22];
                    PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                    Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                    Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                    Organization invoiceOrganization = (Organization)objects[26];
                    InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                    SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                    SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                    SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                    ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                    SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                    SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                    SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                    Client proFormClient = (Client)objects[35];
                    Region proFormClientRegion = (Region)objects[36];
                    RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                    Country proFormClientCountry = (Country)objects[38];
                    ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                    ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                    Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                    ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                    Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                    TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                    PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                    PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                    ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                    Agreement proFormClientAgreement = (Agreement)objects[48];
                    ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                    Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                    Pricing proFormClientPricing = (Pricing)objects[51];
                    PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                    Organization proFormClientAgreementOrganization = (Organization)objects[53];
                    Currency proFormClientAgreementCurrency = (Currency)objects[54];
                    Organization proFormOrganization = (Organization)objects[55];

                    AccountingCashFlowHeadItem itemFromList =
                        accountingCashFlow.AccountingCashFlowHeadItems
                            .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder)
                                        && i.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id)));

                    itemFromList.OrganizationName = invoiceClientAgreementOrganization?.Name
                                                    ?? invoiceOrganization?.Name
                                                    ?? proFormClientAgreementOrganization?.Name
                                                    ?? proFormOrganization?.Name
                                                    ?? "";

                    OutcomePaymentOrderSupplyPaymentTask junctionFromList =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.First(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id));

                    if (junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Any(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id))) {
                        SupplyOrderPaymentDeliveryProtocol protocol =
                            junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.First(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                        if (invoiceSupplyOrder != null)
                            itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();
                        else if (proFormSupplyOrder != null)
                            itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                        if (supplyInvoice != null) {
                            if (invoiceDocument != null && !protocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                                protocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null &&
                                !protocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                protocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }
                        }

                        if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                        if (proFormDocument != null && !protocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                            protocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null &&
                            !protocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            protocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder == null || protocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                            return supplyOrderPaymentDeliveryProtocol;

                        proFormSupplyOrder.Client = proFormClient;
                        proFormSupplyOrder.Organization = proFormOrganization;

                        protocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                    } else {
                        if (invoiceSupplyOrder != null)
                            itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();
                        else if (proFormSupplyOrder != null)
                            itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                        if (supplyInvoice != null) {
                            if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }

                            if (invoiceClient != null) {
                                if (invoiceClientBankDetails != null) {
                                    if (invoiceClientBankDetailAccountNumber != null) {
                                        invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                        invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                    }

                                    if (invoiceClientClientBankDetailIbanNo != null) {
                                        invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                        invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                    }
                                }

                                if (invoiceClientClientAgreement != null) {
                                    if (invoiceClientProviderPricing != null) {
                                        if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                        invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                        invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                    }

                                    invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                    invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                    invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                    invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                    invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                                }

                                invoiceClient.Region = invoiceClientRegion;
                                invoiceClient.RegionCode = invoiceClientRegionCode;
                                invoiceClient.Country = invoiceClientCountry;
                                invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                                invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                                invoiceClient.PackingMarking = invoiceClientPackingMarking;
                                invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                            }

                            invoiceSupplyOrder.Client = invoiceClient;
                            invoiceSupplyOrder.Organization = invoiceOrganization;

                            supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                        }

                        if (supplyProForm != null) {
                            if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                            if (proFormInformationProtocol != null) {
                                proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                                supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                            }

                            if (proFormSupplyOrder != null) {
                                if (proFormClient != null) {
                                    if (proFormClientBankDetails != null) {
                                        if (proFormClientBankDetailAccountNumber != null) {
                                            proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                            proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                        }

                                        if (proFormClientClientBankDetailIbanNo != null) {
                                            proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                            proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                        }
                                    }

                                    if (proFormClientClientAgreement != null) {
                                        if (proFormClientProviderPricing != null) {
                                            if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                            proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                            proFormClientProviderPricing.Pricing = proFormClientPricing;
                                        }

                                        proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                        proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                        proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                        proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                        proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                    }

                                    proFormClient.Region = proFormClientRegion;
                                    proFormClient.RegionCode = proFormClientRegionCode;
                                    proFormClient.Country = proFormClientCountry;
                                    proFormClient.ClientBankDetails = proFormClientBankDetails;
                                    proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                    proFormClient.PackingMarking = proFormClientPackingMarking;
                                    proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                                }

                                proFormSupplyOrder.Client = proFormClient;
                                proFormSupplyOrder.Organization = proFormOrganization;

                                supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                            }
                        }

                        supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                        supplyOrderPaymentDeliveryProtocol.User = user;
                        supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                        supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                        junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Add(supplyOrderPaymentDeliveryProtocol);
                    }

                    return supplyOrderPaymentDeliveryProtocol;
                };

                _connection.Query(
                    "SELECT * " +
                    "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                    "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                    "LEFT JOIN [SupplyPaymentTask] " +
                    "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                    "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                    "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                    "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                    "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                    "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                    "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                    "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                    "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                    "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                    "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                    "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                    "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                    "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                    "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                    "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrderOrganization].OrganizationID " +
                    "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [InvoiceDocument] " +
                    "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyProForm] " +
                    "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                    "LEFT JOIN [ProFormDocument] " +
                    "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                    "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                    "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                    "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                    "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                    "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                    "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                    "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                    "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                    "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                    "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                    "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                    "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                    "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                    "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                    "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                    "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                    "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                    "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                    "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                    "WHERE [SupplyPaymentTask].ID IN @Ids",
                    includesTypes,
                    includesMapper,
                    new {
                        Ids = taskIds,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                    }
                );
            }
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol))) {
            Type[] includesTypes = {
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

            Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> includesMapper = objects => {
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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol) && i.Id.Equals(protocol.Id));

                supplyPaymentTask.User = supplyPaymentTaskUser;

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

                itemFromList.SupplyOrderUkrainePaymentDeliveryProtocol = protocol;

                itemFromList.Comment = supplyOrderUkraine.Comment;

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
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return accountingCashFlow;
    }

    public AccountingCashFlow GetRangedBySupplierV2(Client client, DateTime from, DateTime to) {
        AccountingCashFlow accountingCashFlow = new(client);

        accountingCashFlow.BeforeRangeInAmount =
            _connection.Query<decimal>(
                "DECLARE @UahId int = ( " +
                "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
                "WHERE [Code] = 'UAH' " +
                "AND [Deleted] = 0 " +
                "); " +
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT 0 - ISNULL(SUM(" +
                "[dbo].GetExchangedToEuroValue(" +
                "[PackingListPackageOrderItem].UnitPrice * [ProductIncomeItem].Qty + " +
                "[PackingListPackageOrderItem].[VatAmount] " +
                ", [Currency].[ID] " +
                ", GETUTCDATE() " +
                ")" +
                "), 0) " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND [ProductIncome].ProductIncomeType = 0 " +
                "AND [ProductIncome].FromDate < @From " +
                "AND [SupplyOrder].ClientID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT 0 - " +
                "ISNULL(SUM([dbo].GetExchangedToEuroValue( " +
                "[SupplyOrderUkraineItem].UnitPriceLocal * [ProductIncomeItem].Qty + [SupplyOrderUkraineItem].[VatAmountLocal]" +
                ", [Currency].[ID]" +
                ", GETUTCDATE())), 0) " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID]" +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND [ProductIncome].ProductIncomeType = 1 " +
                "AND [ProductIncome].FromDate < @From " +
                "AND [SupplyOrderUkraine].SupplierID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount), 0)  " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].ClientID = @Id " +
                "AND [IncomePaymentOrder].ClientAgreementID IS NOT NULL " +
                "AND [IncomePaymentOrder].FromDate < @From " +
                "AND [IncomePaymentOrder].Deleted = 0 " +
                "AND [IncomePaymentOrder].IsCanceled = 0 " +
                ") " +
                ", 2) AS [BeforeRangeInAmount] ",
                new { client.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeOutAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT " +
                "ISNULL( " +
                "SUM([dbo].GetExchangedToEuroValue( " +
                "[OutcomePaymentOrder].[Amount]" +
                ", [Currency].ID " +
                ", GETUTCDATE() " +
                ")) " +
                ", 0) " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                "AND [ClientAgreement].ClientID = @Id " +
                "AND [OutcomePaymentOrder].FromDate < @From " +
                ") " +
                ", 2) AS [BeforeRangeOutAmount] ",
                new { client.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        IEnumerable<JoinService> joinServices =
            _connection.Query<JoinService, decimal, JoinService>(
                "DECLARE @UahId int = ( " +
                "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
                "WHERE [Code] = 'UAH' " +
                "AND [Deleted] = 0 " +
                "); " +
                ";WITH [AccountingCashFlow_CTE] " +
                "AS " +
                "( " +
                "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                ", 11 AS [Type] " +
                ", [OutcomePaymentOrder].FromDate AS [FromDate] " +
                ",[dbo].[GetExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[Amount], " +
                "[Currency].ID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
                "WHERE [ClientAgreement].ClientID = @Id " +
                "AND [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                "AND [OutcomePaymentOrder].FromDate >= @From " +
                "AND [OutcomePaymentOrder].FromDate <= @To " +
                "UNION " +
                "SELECT [ProductIncome].ID AS [ID] " +
                ", 19 AS [Type] " +
                ", [ProductIncome].FromDate AS [FromDate] " +
                ", ROUND(( " +
                "SELECT ISNULL(SUM( " +
                "[dbo].GetExchangedToEuroValue( " +
                "[PackingListPackageOrderItem].UnitPrice * " +
                "CONVERT(money, [ProductIncomeItem].Qty) " +
                "+ " +
                "[PackingListPackageOrderItem].[VatAmount] " +
                ", [Currency].[ID] " +
                ", GETUTCDATE()) " +
                "), 0) " +
                "FROM [ProductIncome] AS [CalcProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [CalcProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "WHERE [CalcProductIncome].ID = [ProductIncome].ID " +
                "AND [CalcProductIncome].[IsHide] = 0 ) " +
                ", 2) AS [GrossPrice] " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND ([ProductIncome].ProductIncomeType = 0 " +
                "OR [ProductIncome].ProductIncomeType = 1) " +
                "AND [ProductIncome].FromDate >= @From " +
                "AND [ProductIncome].FromDate <= @To " +
                "AND [SupplyOrder].ClientID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                "UNION " +
                "SELECT [IncomePaymentOrder].ID " +
                ", 12 AS [Type] " +
                ", [IncomePaymentOrder].FromDate " +
                ", [IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].ClientID = @Id " +
                "AND [IncomePaymentOrder].ClientAgreementID IS NOT NULL " +
                "AND [IncomePaymentOrder].Deleted = 0 " +
                "AND [IncomePaymentOrder].IsCanceled = 0 " +
                "AND [IncomePaymentOrder].FromDate >= @From " +
                "AND [IncomePaymentOrder].FromDate <= @To " +
                "UNION " +
                "SELECT [ProductIncome].ID AS [ID] " +
                ", 20 AS [Type] " +
                ", [ProductIncome].FromDate AS [FromDate] " +
                ", ROUND( " +
                "[dbo].GetExchangedToEuroValue(" +
                "( " +
                "SELECT ISNULL(SUM([SupplyOrderUkraineItem].UnitPriceLocal* CONVERT(money, [ProductIncomeItem].Qty) " +
                "+ [SupplyOrderUkraineItem].[VatAmountLocal]), 0) " +
                "FROM [ProductIncome] AS [CalcProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [CalcProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "WHERE [CalcProductIncome].ID = [ProductIncome].ID " +
                ") " +
                ", [Currency].[ID] " +
                ", GETUTCDATE()) " +
                ", 2) AS [GrossPrice] " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID]" +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND [ProductIncome].ProductIncomeType = 1 " +
                "AND [ProductIncome].FromDate >= @From " +
                "AND [ProductIncome].FromDate <= @To " +
                "AND [SupplyOrderUkraine].SupplierID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                ") " +
                "SELECT * " +
                "FROM [AccountingCashFlow_CTE] " +
                "ORDER BY [AccountingCashFlow_CTE].FromDate",
                (service, grossPrice) => {
                    switch (service.Type) {
                        case JoinServiceType.OutcomePaymentOrder:
                            currentStepBalance = Math.Round(currentStepBalance - grossPrice, 2);

                            accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    IsCreditValue = true,
                                    CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol:
                        case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                        case JoinServiceType.ProductIncomePL:
                        case JoinServiceType.ProductIncomeUK:
                            currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                            accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    IsCreditValue = false,
                                    CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.IncomePaymentOrder:
                            currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                            accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 14);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.ContainerService:
                        case JoinServiceType.CustomService:
                        case JoinServiceType.PortWorkService:
                        case JoinServiceType.TransportationService:
                        case JoinServiceType.PortCustomAgencyService:
                        case JoinServiceType.CustomAgencyService:
                        case JoinServiceType.PlaneDeliveryService:
                        case JoinServiceType.VehicleDeliveryService:
                        case JoinServiceType.ConsumablesOrder:
                        case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                        default:
                            break;
                    }

                    return service;
                },
                new { client.Id, From = from, To = to },
                splitOn:
                "ID,GrossPrice"
            );

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder))) {
            string sqlExpression =
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentMovementOperation].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [PaymentMovementOperation].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [IncomePaymentOrder].UserID " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "WHERE [IncomePaymentOrder].ID IN @Ids";

            Type[] types = {
                typeof(IncomePaymentOrder),
                typeof(Organization),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(PaymentCurrencyRegister),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], IncomePaymentOrder> mapper = objects => {
                IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                Currency currency = (Currency)objects[2];
                PaymentRegister paymentRegister = (PaymentRegister)objects[3];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User user = (User)objects[7];
                SupplyOrganization incomeClient = (SupplyOrganization)objects[8];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[9];
                Currency agreementCurrency = (Currency)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.IncomePaymentOrder) && i.Id.Equals(incomePaymentOrder.Id));

                itemFromList.IsAccounting = incomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = incomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.IncomePaymentOrder != null) {
                    itemFromList.Number = itemFromList.IncomePaymentOrder.Number;
                } else {
                    if (!string.IsNullOrEmpty(incomePaymentOrder.Number))
                        itemFromList.Number = incomePaymentOrder.Number;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = agreementCurrency;

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);

                    incomePaymentOrder.Organization = organization;
                    incomePaymentOrder.User = user;
                    incomePaymentOrder.Currency = currency;
                    incomePaymentOrder.PaymentRegister = paymentRegister;
                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    incomePaymentOrder.SupplyOrganization = incomeClient;
                    incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    itemFromList.IncomePaymentOrder = incomePaymentOrder;
                }

                return incomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                commandTimeout: 3600
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderPaymentDeliveryProtocol),
                typeof(SupplyOrderPaymentDeliveryProtocolKey),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyProForm),
                typeof(ProFormDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization)
            };

            Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask) objects[2];
                User user = (User)objects[3];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                Client invoiceClient = (Client)objects[6];
                Region invoiceClientRegion = (Region)objects[7];
                RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                Country invoiceClientCountry = (Country)objects[9];
                ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                Agreement invoiceClientAgreement = (Agreement)objects[19];
                ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                Pricing invoiceClientPricing = (Pricing)objects[22];
                PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                Organization invoiceOrganization = (Organization)objects[26];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                Client proFormClient = (Client)objects[35];
                Region proFormClientRegion = (Region)objects[36];
                RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                Country proFormClientCountry = (Country)objects[38];
                ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                Agreement proFormClientAgreement = (Agreement)objects[48];
                ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                Pricing proFormClientPricing = (Pricing)objects[51];
                PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                Organization proFormClientAgreementOrganization = (Organization)objects[53];
                Currency proFormClientAgreementCurrency = (Currency)objects[54];
                Organization proFormOrganization = (Organization)objects[55];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)
                                    && i.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                itemFromList.OrganizationName =
                    invoiceClientAgreementOrganization?.Name
                    ?? invoiceOrganization?.Name
                    ?? proFormClientAgreementOrganization?.Name
                    ?? proFormOrganization?.Name
                    ?? "";

                if (itemFromList.SupplyOrderPaymentDeliveryProtocol != null) {
                    if (supplyInvoice != null) {
                        itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();

                        if (invoiceDocument != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null &&
                            !itemFromList
                                .SupplyOrderPaymentDeliveryProtocol
                                .SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }
                    }

                    if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                    itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                    if (proFormDocument != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormSupplyOrder == null
                        || itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                        return supplyOrderPaymentDeliveryProtocol;

                    proFormSupplyOrder.Client = proFormClient;
                    proFormSupplyOrder.Organization = proFormOrganization;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                } else {
                    if (supplyInvoice != null) {
                        itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();

                        if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }

                        if (invoiceClient != null) {
                            if (invoiceClientBankDetails != null) {
                                if (invoiceClientBankDetailAccountNumber != null) {
                                    invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                    invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                }

                                if (invoiceClientClientBankDetailIbanNo != null) {
                                    invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                    invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                }
                            }

                            if (invoiceClientClientAgreement != null) {
                                if (invoiceClientProviderPricing != null) {
                                    if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                    invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                    invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                }

                                invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                            }

                            invoiceClient.Region = invoiceClientRegion;
                            invoiceClient.RegionCode = invoiceClientRegionCode;
                            invoiceClient.Country = invoiceClientCountry;
                            invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                            invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                            invoiceClient.PackingMarking = invoiceClientPackingMarking;
                            invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                        }

                        invoiceSupplyOrder.Client = invoiceClient;
                        invoiceSupplyOrder.Organization = invoiceOrganization;

                        supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                    }

                    if (supplyProForm != null) {
                        itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                        if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder != null) {
                            if (proFormClient != null) {
                                if (proFormClientBankDetails != null) {
                                    if (proFormClientBankDetailAccountNumber != null) {
                                        proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                        proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                    }

                                    if (proFormClientClientBankDetailIbanNo != null) {
                                        proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                        proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                    }
                                }

                                if (proFormClientClientAgreement != null) {
                                    if (proFormClientProviderPricing != null) {
                                        if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                        proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                        proFormClientProviderPricing.Pricing = proFormClientPricing;
                                    }

                                    proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                    proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                    proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                    proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                    proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                }

                                proFormClient.Region = proFormClientRegion;
                                proFormClient.RegionCode = proFormClientRegionCode;
                                proFormClient.Country = proFormClientCountry;
                                proFormClient.ClientBankDetails = proFormClientBankDetails;
                                proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                proFormClient.PackingMarking = proFormClientPackingMarking;
                                proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                            }

                            proFormSupplyOrder.Client = proFormClient;
                            proFormSupplyOrder.Organization = proFormOrganization;

                            supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                        }
                    }

                    supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                    supplyOrderPaymentDeliveryProtocol.User = user;
                    supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                    supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol = supplyOrderPaymentDeliveryProtocol;
                }

                return supplyOrderPaymentDeliveryProtocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrderOrganization].OrganizationID " +
                "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                "LEFT JOIN [ProFormDocument] " +
                "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                "WHERE [SupplyOrderPaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            List<long> taskIds = new();

            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[dbo].[GetExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[Amount], " +
                "[Agreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[IsAccounting] " +
                ",[OutcomePaymentOrder].[IsManagementAccounting] " +
                ",[OutcomePaymentOrder].[ExchangeRate] " +
                ",[OutcomePaymentOrder].[VAT]" +
                ",[OutcomePaymentOrder].[VatPercent] " +
                ",[dbo].[GetExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [OutcomePaymentOrder].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.IsAccounting = outcomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = outcomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;

                    if (supplyPaymentTask != null)
                        taskIds.Add(supplyPaymentTask.Id);
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);

                    if (supplyPaymentTask != null)
                        taskIds.Add(supplyPaymentTask.Id);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);

            if (taskIds.Any()) {
                Type[] includesTypes = {
                    typeof(SupplyOrderPaymentDeliveryProtocol),
                    typeof(SupplyOrderPaymentDeliveryProtocolKey),
                    typeof(SupplyPaymentTask),
                    typeof(User),
                    typeof(SupplyInvoice),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization),
                    typeof(InvoiceDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyProForm),
                    typeof(ProFormDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization)
                };

                Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                    SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                    SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                    SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                    User user = (User)objects[3];
                    SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                    SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                    Client invoiceClient = (Client)objects[6];
                    Region invoiceClientRegion = (Region)objects[7];
                    RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                    Country invoiceClientCountry = (Country)objects[9];
                    ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                    ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                    Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                    ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                    Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                    TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                    PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                    PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                    ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                    Agreement invoiceClientAgreement = (Agreement)objects[19];
                    ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                    Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                    Pricing invoiceClientPricing = (Pricing)objects[22];
                    PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                    Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                    Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                    Organization invoiceOrganization = (Organization)objects[26];
                    InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                    SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                    SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                    SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                    ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                    SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                    SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                    SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                    Client proFormClient = (Client)objects[35];
                    Region proFormClientRegion = (Region)objects[36];
                    RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                    Country proFormClientCountry = (Country)objects[38];
                    ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                    ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                    Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                    ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                    Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                    TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                    PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                    PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                    ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                    Agreement proFormClientAgreement = (Agreement)objects[48];
                    ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                    Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                    Pricing proFormClientPricing = (Pricing)objects[51];
                    PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                    Organization proFormClientAgreementOrganization = (Organization)objects[53];
                    Currency proFormClientAgreementCurrency = (Currency)objects[54];
                    Organization proFormOrganization = (Organization)objects[55];

                    AccountingCashFlowHeadItem itemFromList =
                        accountingCashFlow.AccountingCashFlowHeadItems
                            .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder)
                                        && i.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id)));

                    itemFromList.OrganizationName = invoiceClientAgreementOrganization?.Name
                                                    ?? invoiceOrganization?.Name
                                                    ?? proFormClientAgreementOrganization?.Name
                                                    ?? proFormOrganization?.Name
                                                    ?? "";

                    OutcomePaymentOrderSupplyPaymentTask junctionFromList =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.First(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id));

                    if (junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Any(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id))) {
                        SupplyOrderPaymentDeliveryProtocol protocol =
                            junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.First(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                        if (supplyInvoice != null) {
                            if (invoiceDocument != null && !protocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                                protocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null &&
                                !protocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                protocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }
                        }

                        if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                        if (proFormDocument != null && !protocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                            protocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null &&
                            !protocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            protocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder == null || protocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                            return supplyOrderPaymentDeliveryProtocol;

                        proFormSupplyOrder.Client = proFormClient;
                        proFormSupplyOrder.Organization = proFormOrganization;

                        protocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                    } else {
                        if (supplyInvoice != null) {
                            if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }

                            if (invoiceClient != null) {
                                if (invoiceClientBankDetails != null) {
                                    if (invoiceClientBankDetailAccountNumber != null) {
                                        invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                        invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                    }

                                    if (invoiceClientClientBankDetailIbanNo != null) {
                                        invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                        invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                    }
                                }

                                if (invoiceClientClientAgreement != null) {
                                    if (invoiceClientProviderPricing != null) {
                                        if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                        invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                        invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                    }

                                    invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                    invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                    invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                    invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                    invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                                }

                                invoiceClient.Region = invoiceClientRegion;
                                invoiceClient.RegionCode = invoiceClientRegionCode;
                                invoiceClient.Country = invoiceClientCountry;
                                invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                                invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                                invoiceClient.PackingMarking = invoiceClientPackingMarking;
                                invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                            }

                            invoiceSupplyOrder.Client = invoiceClient;
                            invoiceSupplyOrder.Organization = invoiceOrganization;

                            supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                        }

                        if (supplyProForm != null) {
                            if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                            if (proFormInformationProtocol != null) {
                                proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                                supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                            }

                            if (proFormSupplyOrder != null) {
                                if (proFormClient != null) {
                                    if (proFormClientBankDetails != null) {
                                        if (proFormClientBankDetailAccountNumber != null) {
                                            proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                            proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                        }

                                        if (proFormClientClientBankDetailIbanNo != null) {
                                            proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                            proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                        }
                                    }

                                    if (proFormClientClientAgreement != null) {
                                        if (proFormClientProviderPricing != null) {
                                            if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                            proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                            proFormClientProviderPricing.Pricing = proFormClientPricing;
                                        }

                                        proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                        proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                        proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                        proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                        proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                    }

                                    proFormClient.Region = proFormClientRegion;
                                    proFormClient.RegionCode = proFormClientRegionCode;
                                    proFormClient.Country = proFormClientCountry;
                                    proFormClient.ClientBankDetails = proFormClientBankDetails;
                                    proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                    proFormClient.PackingMarking = proFormClientPackingMarking;
                                    proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                                }

                                proFormSupplyOrder.Client = proFormClient;
                                proFormSupplyOrder.Organization = proFormOrganization;

                                supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                            }
                        }

                        supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                        supplyOrderPaymentDeliveryProtocol.User = user;
                        supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                        supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                        junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Add(supplyOrderPaymentDeliveryProtocol);
                    }

                    return supplyOrderPaymentDeliveryProtocol;
                };

                _connection.Query(
                    "SELECT * " +
                    "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                    "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                    "LEFT JOIN [SupplyPaymentTask] " +
                    "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                    "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                    "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                    "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                    "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                    "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                    "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                    "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                    "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                    "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                    "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                    "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                    "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                    "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                    "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                    "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrder].OrganizationID " +
                    "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [InvoiceDocument] " +
                    "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyProForm] " +
                    "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                    "LEFT JOIN [ProFormDocument] " +
                    "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                    "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                    "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                    "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                    "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                    "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                    "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                    "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                    "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                    "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                    "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                    "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                    "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                    "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                    "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                    "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                    "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                    "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                    "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                    "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                    "WHERE [SupplyPaymentTask].ID IN @Ids",
                    includesTypes,
                    includesMapper,
                    new {
                        Ids = taskIds,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                    }
                );
            }
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol))) {
            Type[] includesTypes = {
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

            Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> includesMapper = objects => {
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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol) && i.Id.Equals(protocol.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                supplyPaymentTask.User = supplyPaymentTaskUser;

                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                supplyOrderUkraine.Supplier = supplier;
                supplyOrderUkraine.Responsible = responsible;
                supplyOrderUkraine.Organization = organization;
                supplyOrderUkraine.ClientAgreement = clientAgreement;

                itemFromList.Number = supplyOrderUkraine.Number;

                protocol.User = protocolUser;
                protocol.SupplyPaymentTask = supplyPaymentTask;
                protocol.SupplyOrderUkraine = supplyOrderUkraine;
                protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = protocolKey;

                itemFromList.SupplyOrderUkrainePaymentDeliveryProtocol = protocol;

                itemFromList.Comment = supplyOrderUkraine.Comment;

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
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ProductIncomePL))) {
            Type[] includesTypes = {
                typeof(ProductIncome),
                typeof(User),
                typeof(Storage),
                typeof(ProductIncomeItem),
                typeof(PackingListPackageOrderItem),
                typeof(SupplyInvoiceOrderItem),
                typeof(SupplyOrderItem),
                typeof(Product),
                typeof(PackingList),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Organization)
            };

            Func<object[], ProductIncome> includesMapper = objects => {
                ProductIncome productIncome = (ProductIncome)objects[0];
                User user = (User)objects[1];
                Storage storage = (Storage)objects[2];
                ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
                PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[4];
                SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
                SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
                Product product = (Product)objects[7];
                PackingList packingList = (PackingList)objects[8];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
                Organization organization = (Organization)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ProductIncomePL) && i.Id.Equals(productIncome.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ProductIncome != null) {
                    if (!string.IsNullOrEmpty(itemFromList.ProductIncome.Number))
                        itemFromList.Number = itemFromList.ProductIncome.Number;

                    if (productIncomeItem == null) return productIncome;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;

                    supplyInvoiceOrderItem.Product = product;

                    packingList.SupplyInvoice = supplyInvoice;

                    itemFromList.Comment = supplyInvoice.Comment;

                    packingListPackageOrderItem.PackingList = packingList;
                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    decimal totalVat;

                    if (!packingListPackageOrderItem.ExchangeRateAmount.Equals(0))
                        totalVat = packingListPackageOrderItem.ExchangeRateAmount > 0
                            ? packingListPackageOrderItem.VatAmount / packingListPackageOrderItem.ExchangeRateAmount
                            : Math.Abs(packingListPackageOrderItem.VatAmount * packingListPackageOrderItem.ExchangeRateAmount);
                    else
                        totalVat = packingListPackageOrderItem.VatAmount;

                    packingListPackageOrderItem.TotalNetPrice =
                        decimal.Round(
                            packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(productIncomeItem.Qty) + totalVat,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                    itemFromList.ProductIncome.TotalNetPrice =
                        decimal.Round(
                            itemFromList.ProductIncome.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    itemFromList.ProductIncome.ProductIncomeItems.Add(productIncomeItem);
                } else {
                    if (productIncome != null && !string.IsNullOrEmpty(productIncome.Number)) {
                        itemFromList.Number = productIncome.Number;

                        if (productIncomeItem != null) {
                            if (supplyOrderItem != null)
                                supplyOrderItem.Product = product;

                            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                            supplyInvoiceOrderItem.Product = product;

                            packingList.SupplyInvoice = supplyInvoice;

                            itemFromList.Comment = supplyInvoice.Comment;

                            packingListPackageOrderItem.PackingList = packingList;
                            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                            decimal totalVat;

                            if (!packingListPackageOrderItem.ExchangeRateAmount.Equals(0))
                                totalVat = packingListPackageOrderItem.ExchangeRateAmount > 0
                                    ? packingListPackageOrderItem.VatAmount / packingListPackageOrderItem.ExchangeRateAmount
                                    : Math.Abs(packingListPackageOrderItem.VatAmount * packingListPackageOrderItem.ExchangeRateAmount);
                            else
                                totalVat = packingListPackageOrderItem.VatAmount;

                            packingListPackageOrderItem.TotalNetPrice =
                                decimal.Round(
                                    packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(productIncomeItem.Qty) + totalVat,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                            productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                            productIncome.TotalNetPrice =
                                decimal.Round(
                                    productIncome.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                            productIncome.ProductIncomeItems.Add(productIncomeItem);
                        }

                        productIncome.User = user;
                        productIncome.Storage = storage;
                    }

                    itemFromList.ProductIncome = productIncome;
                }

                return productIncome;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductIncome].UserID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].[ID] = [SupplyOrderItem].[SupplyOrderID] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductIncome].ID IN @Ids " +
                "AND [ProductIncome].[IsHide] = 0 ",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ProductIncomePL)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ProductIncomeUK))) {
            Type[] includesTypes = {
                typeof(ProductIncome),
                typeof(User),
                typeof(Storage),
                typeof(ProductIncomeItem),
                typeof(SupplyOrderUkraineItem),
                typeof(Product),
                typeof(SupplyOrderUkraine),
                typeof(Organization)
            };

            Func<object[], ProductIncome> includesMapper = objects => {
                ProductIncome productIncome = (ProductIncome)objects[0];
                User user = (User)objects[1];
                Storage storage = (Storage)objects[2];
                ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
                SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[4];
                Product product = (Product)objects[5];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
                Organization organization = (Organization)objects[7];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ProductIncomeUK) && i.Id.Equals(productIncome.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ProductIncome != null) {
                    if (productIncomeItem == null) return productIncome;

                    supplyOrderUkraineItem.Product = product;
                    supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

                    itemFromList.Comment = supplyOrderUkraine.Comment;

                    supplyOrderUkraineItem.NetPrice =
                        decimal.Round(
                            supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty) + supplyOrderUkraineItem.VatAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

                    itemFromList.ProductIncome.TotalNetPrice =
                        decimal.Round(
                            itemFromList.ProductIncome.TotalNetPrice + supplyOrderUkraineItem.NetPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    itemFromList.ProductIncome.ProductIncomeItems.Add(productIncomeItem);
                } else {
                    itemFromList.Number = productIncome.Number;

                    if (productIncomeItem != null) {
                        supplyOrderUkraineItem.Product = product;
                        supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

                        itemFromList.Comment = supplyOrderUkraine.Comment;

                        supplyOrderUkraineItem.NetPrice =
                            decimal.Round(
                                supplyOrderUkraineItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty) + supplyOrderUkraineItem.VatAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

                        productIncome.TotalNetPrice =
                            decimal.Round(
                                productIncome.TotalNetPrice + supplyOrderUkraineItem.NetPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        productIncome.ProductIncomeItems.Add(productIncomeItem);
                    }

                    productIncome.User = user;
                    productIncome.Storage = storage;

                    itemFromList.ProductIncome = productIncome;
                }

                return productIncome;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductIncome].UserID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductIncome].ID IN @Ids " +
                "AND [ProductIncome].[IsHide] = 0 ",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ProductIncomeUK)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return accountingCashFlow;
    }

    public AccountingCashFlow GetRangedBySupplierClientAgreement(ClientAgreement preDefinedClientAgreement, DateTime from, DateTime to) {
        AccountingCashFlow accountingCashFlow = new(preDefinedClientAgreement);

        accountingCashFlow.BeforeRangeInAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT 0 - ISNULL(SUM(dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice, [Agreement].CurrencyID, [SupplyInvoice].DateFrom)), 0) " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrder].ClientAgreementID = @Id " +
                "AND [SupplyInvoice].DateFrom < @From " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                ") " +
                "+ " +
                "(" +
                "SELECT 0 - ISNULL(SUM(dbo.GetGovExchangedToEuroValue([SupplyPaymentTask].GrossPrice, [Agreement].CurrencyID, [SupplyOrderUkraine].FromDate)), 0) " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [SupplyOrderUkraine].ClientAgreementID = @Id " +
                "AND [SupplyOrderUkraine].FromDate < @From " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "AND [SupplyOrderUkraine].[IsPartialPlaced] = 1 " +
                ")" +
                ", 2) AS [BeforeRangeInAmount]",
                new { preDefinedClientAgreement.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeOutAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT " +
                "ISNULL( " +
                "SUM([OutcomePaymentOrder].AfterExchangeAmount) " +
                ", 0) " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                "AND [ClientAgreement].ID = @Id " +
                "AND [OutcomePaymentOrder].FromDate < @From " +
                ") " +
                ", 2) AS [BeforeRangeOutAmount] ",
                new { preDefinedClientAgreement.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        IEnumerable<JoinService> joinServices =
            _connection.Query<JoinService, decimal, JoinService>(
                ";WITH [AccountingCashFlow_CTE] " +
                "AS " +
                "( " +
                "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                ", 11 AS [Type] " +
                ", [OutcomePaymentOrder].FromDate AS [FromDate] " +
                ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "WHERE [ClientAgreement].ID = @Id " +
                "AND [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                "AND [OutcomePaymentOrder].FromDate >= @From " +
                "AND [OutcomePaymentOrder].FromDate <= @To " +
                "UNION " +
                "SELECT [SupplyOrderPaymentDeliveryProtocol].ID AS [ID] " +
                ", 0 AS [Type] " +
                ", [SupplyInvoice].DateFrom AS [FromDate] " +
                ", [SupplyPaymentTask].GrossPrice AS [GrossPrice] " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [ClientAgreement].ID = @Id " +
                "AND [SupplyInvoice].DateFrom >= @From " +
                "AND [SupplyInvoice].DateFrom <= @To " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "UNION " +
                "SELECT [SupplyOrderUkrainePaymentDeliveryProtocol].ID AS [ID] " +
                ", 18 AS [Type] " +
                ", [SupplyOrderUkraine].FromDate AS [FromDate] " +
                ", [SupplyPaymentTask].GrossPrice AS [GrossPrice] " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [ClientAgreement].ID = @Id " +
                "AND [SupplyOrderUkraine].FromDate >= @From " +
                "AND [SupplyOrderUkraine].FromDate <= @To " +
                "AND [SupplyPaymentTask].Deleted = 0 " +
                "AND [SupplyOrderUkraine].[IsPartialPlaced] = 1 " +
                ") " +
                "SELECT * " +
                "FROM [AccountingCashFlow_CTE] " +
                "ORDER BY [AccountingCashFlow_CTE].FromDate",
                (service, grossPrice) => {
                    switch (service.Type) {
                        case JoinServiceType.OutcomePaymentOrder:
                            currentStepBalance = Math.Round(currentStepBalance - grossPrice, 2);

                            accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol:
                        case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                            currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                            accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.ContainerService:
                        case JoinServiceType.CustomService:
                        case JoinServiceType.PortWorkService:
                        case JoinServiceType.TransportationService:
                        case JoinServiceType.PortCustomAgencyService:
                        case JoinServiceType.CustomAgencyService:
                        case JoinServiceType.PlaneDeliveryService:
                        case JoinServiceType.VehicleDeliveryService:
                        case JoinServiceType.ConsumablesOrder:
                        case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                        default:
                            break;
                    }

                    return service;
                },
                new { preDefinedClientAgreement.Id, From = from, To = to },
                splitOn: "ID,GrossPrice"
            );

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderPaymentDeliveryProtocol),
                typeof(SupplyOrderPaymentDeliveryProtocolKey),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyProForm),
                typeof(ProFormDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization)
            };

            Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask) objects[2];
                User user = (User)objects[3];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                Client invoiceClient = (Client)objects[6];
                Region invoiceClientRegion = (Region)objects[7];
                RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                Country invoiceClientCountry = (Country)objects[9];
                ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                Agreement invoiceClientAgreement = (Agreement)objects[19];
                ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                Pricing invoiceClientPricing = (Pricing)objects[22];
                PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                Organization invoiceOrganization = (Organization)objects[26];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                Client proFormClient = (Client)objects[35];
                Region proFormClientRegion = (Region)objects[36];
                RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                Country proFormClientCountry = (Country)objects[38];
                ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                Agreement proFormClientAgreement = (Agreement)objects[48];
                ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                Pricing proFormClientPricing = (Pricing)objects[51];
                PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                Organization proFormClientAgreementOrganization = (Organization)objects[53];
                Currency proFormClientAgreementCurrency = (Currency)objects[54];
                Organization proFormOrganization = (Organization)objects[55];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol) && i.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                if (itemFromList.SupplyOrderPaymentDeliveryProtocol != null) {
                    if (supplyInvoice != null) {
                        if (invoiceDocument != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }
                    }

                    if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                    if (proFormDocument != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormSupplyOrder == null ||
                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                        return supplyOrderPaymentDeliveryProtocol;

                    proFormSupplyOrder.Client = proFormClient;
                    proFormSupplyOrder.Organization = proFormOrganization;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                } else {
                    if (supplyInvoice != null) {
                        if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }

                        if (invoiceClient != null) {
                            if (invoiceClientBankDetails != null) {
                                if (invoiceClientBankDetailAccountNumber != null) {
                                    invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                    invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                }

                                if (invoiceClientClientBankDetailIbanNo != null) {
                                    invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                    invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                }
                            }

                            if (invoiceClientClientAgreement != null) {
                                if (invoiceClientProviderPricing != null) {
                                    if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                    invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                    invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                }

                                invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                            }

                            invoiceClient.Region = invoiceClientRegion;
                            invoiceClient.RegionCode = invoiceClientRegionCode;
                            invoiceClient.Country = invoiceClientCountry;
                            invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                            invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                            invoiceClient.PackingMarking = invoiceClientPackingMarking;
                            invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                        }

                        invoiceSupplyOrder.Client = invoiceClient;
                        invoiceSupplyOrder.Organization = invoiceOrganization;

                        supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                    }

                    if (supplyProForm != null) {
                        if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder != null) {
                            if (proFormClient != null) {
                                if (proFormClientBankDetails != null) {
                                    if (proFormClientBankDetailAccountNumber != null) {
                                        proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                        proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                    }

                                    if (proFormClientClientBankDetailIbanNo != null) {
                                        proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                        proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                    }
                                }

                                if (proFormClientClientAgreement != null) {
                                    if (proFormClientProviderPricing != null) {
                                        if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                        proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                        proFormClientProviderPricing.Pricing = proFormClientPricing;
                                    }

                                    proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                    proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                    proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                    proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                    proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                }

                                proFormClient.Region = proFormClientRegion;
                                proFormClient.RegionCode = proFormClientRegionCode;
                                proFormClient.Country = proFormClientCountry;
                                proFormClient.ClientBankDetails = proFormClientBankDetails;
                                proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                proFormClient.PackingMarking = proFormClientPackingMarking;
                                proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                            }

                            proFormSupplyOrder.Client = proFormClient;
                            proFormSupplyOrder.Organization = proFormOrganization;

                            supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                        }
                    }

                    supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                    supplyOrderPaymentDeliveryProtocol.User = user;
                    supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                    supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol = supplyOrderPaymentDeliveryProtocol;
                }

                return supplyOrderPaymentDeliveryProtocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrderOrganization].OrganizationID " +
                "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                "LEFT JOIN [ProFormDocument] " +
                "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                "WHERE [SupplyOrderPaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            List<long> taskIds = new();

            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[ExchangeRate] " +
                ",[dbo].[GetGovExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "[OutcomePaymentOrder].[FromDate] " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;

                    taskIds.Add(supplyPaymentTask.Id);
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);

                    taskIds.Add(supplyPaymentTask.Id);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);

            if (taskIds.Any()) {
                Type[] includesTypes = {
                    typeof(SupplyOrderPaymentDeliveryProtocol),
                    typeof(SupplyOrderPaymentDeliveryProtocolKey),
                    typeof(SupplyPaymentTask),
                    typeof(User),
                    typeof(SupplyInvoice),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization),
                    typeof(InvoiceDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyProForm),
                    typeof(ProFormDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization)
                };

                Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                    SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                    SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                    SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                    User user = (User)objects[3];
                    SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                    SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                    Client invoiceClient = (Client)objects[6];
                    Region invoiceClientRegion = (Region)objects[7];
                    RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                    Country invoiceClientCountry = (Country)objects[9];
                    ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                    ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                    Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                    ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                    Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                    TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                    PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                    PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                    ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                    Agreement invoiceClientAgreement = (Agreement)objects[19];
                    ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                    Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                    Pricing invoiceClientPricing = (Pricing)objects[22];
                    PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                    Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                    Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                    Organization invoiceOrganization = (Organization)objects[26];
                    InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                    SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                    SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                    SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                    ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                    SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                    SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                    SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                    Client proFormClient = (Client)objects[35];
                    Region proFormClientRegion = (Region)objects[36];
                    RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                    Country proFormClientCountry = (Country)objects[38];
                    ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                    ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                    Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                    ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                    Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                    TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                    PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                    PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                    ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                    Agreement proFormClientAgreement = (Agreement)objects[48];
                    ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                    Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                    Pricing proFormClientPricing = (Pricing)objects[51];
                    PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                    Organization proFormClientAgreementOrganization = (Organization)objects[53];
                    Currency proFormClientAgreementCurrency = (Currency)objects[54];
                    Organization proFormOrganization = (Organization)objects[55];

                    AccountingCashFlowHeadItem itemFromList =
                        accountingCashFlow.AccountingCashFlowHeadItems
                            .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder)
                                        && i.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id)));

                    itemFromList.OrganizationName = invoiceClientAgreementOrganization?.Name
                                                    ?? invoiceOrganization?.Name
                                                    ?? proFormClientAgreementOrganization?.Name
                                                    ?? proFormOrganization?.Name
                                                    ?? "";

                    OutcomePaymentOrderSupplyPaymentTask junctionFromList =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.First(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id));

                    if (junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Any(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id))) {
                        SupplyOrderPaymentDeliveryProtocol protocol =
                            junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.First(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                        if (invoiceSupplyOrder != null)
                            itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();
                        else if (proFormSupplyOrder != null)
                            itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                        if (supplyInvoice != null) {
                            if (invoiceDocument != null && !protocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                                protocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null &&
                                !protocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                protocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }
                        }

                        if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                        if (proFormDocument != null && !protocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                            protocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null &&
                            !protocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            protocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder == null || protocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                            return supplyOrderPaymentDeliveryProtocol;

                        proFormSupplyOrder.Client = proFormClient;
                        proFormSupplyOrder.Organization = proFormOrganization;

                        protocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                    } else {
                        if (invoiceSupplyOrder != null)
                            itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();
                        else if (proFormSupplyOrder != null)
                            itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                        if (supplyInvoice != null) {
                            if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }

                            if (invoiceClient != null) {
                                if (invoiceClientBankDetails != null) {
                                    if (invoiceClientBankDetailAccountNumber != null) {
                                        invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                        invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                    }

                                    if (invoiceClientClientBankDetailIbanNo != null) {
                                        invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                        invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                    }
                                }

                                if (invoiceClientClientAgreement != null) {
                                    if (invoiceClientProviderPricing != null) {
                                        if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                        invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                        invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                    }

                                    invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                    invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                    invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                    invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                    invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                                }

                                invoiceClient.Region = invoiceClientRegion;
                                invoiceClient.RegionCode = invoiceClientRegionCode;
                                invoiceClient.Country = invoiceClientCountry;
                                invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                                invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                                invoiceClient.PackingMarking = invoiceClientPackingMarking;
                                invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                            }

                            invoiceSupplyOrder.Client = invoiceClient;
                            invoiceSupplyOrder.Organization = invoiceOrganization;

                            supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                        }

                        if (supplyProForm != null) {
                            if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                            if (proFormInformationProtocol != null) {
                                proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                                supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                            }

                            if (proFormSupplyOrder != null) {
                                if (proFormClient != null) {
                                    if (proFormClientBankDetails != null) {
                                        if (proFormClientBankDetailAccountNumber != null) {
                                            proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                            proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                        }

                                        if (proFormClientClientBankDetailIbanNo != null) {
                                            proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                            proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                        }
                                    }

                                    if (proFormClientClientAgreement != null) {
                                        if (proFormClientProviderPricing != null) {
                                            if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                            proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                            proFormClientProviderPricing.Pricing = proFormClientPricing;
                                        }

                                        proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                        proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                        proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                        proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                        proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                    }

                                    proFormClient.Region = proFormClientRegion;
                                    proFormClient.RegionCode = proFormClientRegionCode;
                                    proFormClient.Country = proFormClientCountry;
                                    proFormClient.ClientBankDetails = proFormClientBankDetails;
                                    proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                    proFormClient.PackingMarking = proFormClientPackingMarking;
                                    proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                                }

                                proFormSupplyOrder.Client = proFormClient;
                                proFormSupplyOrder.Organization = proFormOrganization;

                                supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                            }
                        }

                        supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                        supplyOrderPaymentDeliveryProtocol.User = user;
                        supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                        supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                        junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Add(supplyOrderPaymentDeliveryProtocol);
                    }

                    return supplyOrderPaymentDeliveryProtocol;
                };

                _connection.Query(
                    "SELECT * " +
                    "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                    "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                    "LEFT JOIN [SupplyPaymentTask] " +
                    "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                    "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                    "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                    "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                    "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                    "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                    "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                    "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                    "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                    "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                    "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                    "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                    "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                    "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                    "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                    "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrderOrganization].OrganizationID " +
                    "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [InvoiceDocument] " +
                    "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyProForm] " +
                    "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                    "LEFT JOIN [ProFormDocument] " +
                    "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                    "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                    "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                    "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                    "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                    "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                    "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                    "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                    "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                    "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                    "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                    "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                    "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                    "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                    "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                    "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                    "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                    "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                    "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                    "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                    "WHERE [SupplyPaymentTask].ID IN @Ids",
                    includesTypes,
                    includesMapper,
                    new {
                        Ids = taskIds,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                    }
                );
            }
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol))) {
            Type[] includesTypes = {
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

            Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> includesMapper = objects => {
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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol) && i.Id.Equals(protocol.Id));

                supplyPaymentTask.User = supplyPaymentTaskUser;

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

                itemFromList.SupplyOrderUkrainePaymentDeliveryProtocol = protocol;

                itemFromList.Comment = supplyOrderUkraine.Comment;

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
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return accountingCashFlow;
    }

    public AccountingCashFlow GetRangedBySupplierClientAgreementV2(ClientAgreement preDefinedClientAgreement, DateTime from, DateTime to) {
        AccountingCashFlow accountingCashFlow = new(preDefinedClientAgreement);

        accountingCashFlow.BeforeRangeInAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT 0 - ISNULL(SUM([PackingListPackageOrderItem].UnitPrice * [ProductIncomeItem].Qty + " +
                "CASE " +
                "WHEN [PackingListPackageOrderItem].[ExchangeRateAmount] != 0 " +
                "THEN " +
                "CASE " +
                "WHEN [PackingListPackageOrderItem].[ExchangeRateAmount] > 0 " +
                "THEN [PackingListPackageOrderItem].[VatAmount] / [PackingListPackageOrderItem].[ExchangeRateAmount] " +
                "ELSE ABS([PackingListPackageOrderItem].[VatAmount] * [PackingListPackageOrderItem].[ExchangeRateAmount]) " +
                "END " +
                "ELSE [PackingListPackageOrderItem].[VatAmount] " +
                "END " +
                "), 0) " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND [ProductIncome].ProductIncomeType = 0 " +
                "AND [ProductIncome].FromDate < @From " +
                "AND [SupplyOrder].ClientAgreementID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT 0 - ISNULL(SUM([SupplyOrderUkraineItem].UnitPriceLocal * " +
                "[ProductIncomeItem].Qty + [SupplyOrderUkraineItem].[VatAmountLocal]), 0) " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND [ProductIncome].ProductIncomeType = 1 " +
                "AND [ProductIncome].FromDate < @From " +
                "AND [SupplyOrderUkraine].ClientAgreementID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT 0 - ISNULL(SUM(ROUND([IncomePaymentOrder].Amount * [IncomePaymentOrder].ExchangeRate, 4)), 0)  " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                "AND [IncomePaymentOrder].FromDate < @From " +
                "AND [IncomePaymentOrder].Deleted = 0 " +
                "AND [IncomePaymentOrder].IsCanceled = 0 " +
                ") " +
                ", 2) AS [BeforeRangeInAmount] ",
                new { preDefinedClientAgreement.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeOutAmount =
            _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "SELECT " +
                "ISNULL( " +
                "SUM([OutcomePaymentOrder].AfterExchangeAmount) " +
                ", 0) " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                "AND [ClientAgreement].ID = @Id " +
                "AND [OutcomePaymentOrder].FromDate < @From " +
                ") " +
                ", 2) AS [BeforeRangeOutAmount] ",
                new { preDefinedClientAgreement.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        IEnumerable<JoinService> joinServices =
            _connection.Query<JoinService, decimal, JoinService>(
                ";WITH [AccountingCashFlow_CTE] " +
                "AS " +
                "( " +
                "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                ", 11 AS [Type] " +
                ", [OutcomePaymentOrder].FromDate AS [FromDate] " +
                ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                "WHERE [ClientAgreement].ID = @Id " +
                "AND [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                "AND [OutcomePaymentOrder].FromDate >= @From " +
                "AND [OutcomePaymentOrder].FromDate <= @To " +
                "UNION " +
                "SELECT [IncomePaymentOrder].ID " +
                ", 12 AS [Type] " +
                ", [IncomePaymentOrder].FromDate " +
                ", [IncomePaymentOrder].AgreementExchangedAmount AS [GrossPrice] " +
                //", ISNULL((ROUND([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 4)), 0) AS [GrossPrice] " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].ClientAgreementID = @Id " +
                "AND [IncomePaymentOrder].Deleted = 0 " +
                "AND [IncomePaymentOrder].IsCanceled = 0 " +
                "AND [IncomePaymentOrder].FromDate >= @From " +
                "AND [IncomePaymentOrder].FromDate <= @To " +
                "UNION " +
                "SELECT [ProductIncome].ID AS [ID] " +
                ", 19 AS [Type] " +
                ", [ProductIncome].FromDate AS [FromDate] " +
                ", ROUND( " +
                "( " +
                "SELECT ISNULL(SUM([PackingListPackageOrderItem].UnitPrice * CONVERT(money, [ProductIncomeItem].Qty) + " +
                "CASE " +
                "WHEN [PackingListPackageOrderItem].[ExchangeRateAmount] != 0 " +
                "THEN " +
                "CASE " +
                "WHEN [PackingListPackageOrderItem].[ExchangeRateAmount] > 0 " +
                "THEN [PackingListPackageOrderItem].[VatAmount] / [PackingListPackageOrderItem].[ExchangeRateAmount] " +
                "ELSE ABS([PackingListPackageOrderItem].[VatAmount] * [PackingListPackageOrderItem].[ExchangeRateAmount]) " +
                "END " +
                "ELSE [PackingListPackageOrderItem].[VatAmount] " +
                "END " +
                "), 0) " +
                "FROM [ProductIncome] AS [CalcProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [CalcProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "WHERE [CalcProductIncome].ID = [ProductIncome].ID " +
                ") " +
                ", 14) AS [GrossPrice] " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND ([ProductIncome].ProductIncomeType = 0 " +
                "OR [ProductIncome].ProductIncomeType = 1) " +
                "AND [ProductIncome].FromDate >= @From " +
                "AND [ProductIncome].FromDate <= @To " +
                "AND [SupplyOrder].ClientAgreementID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                "UNION " +
                "SELECT [ProductIncome].ID AS [ID] " +
                ", 20 AS [Type] " +
                ", [ProductIncome].FromDate AS [FromDate] " +
                ", ROUND( " +
                "( " +
                "SELECT ISNULL(SUM([SupplyOrderUkraineItem].UnitPriceLocal * CONVERT(money, [ProductIncomeItem].Qty) " +
                "+ [SupplyOrderUkraineItem].[VatAmountLocal] " +
                "), 0) " +
                "FROM [ProductIncome] AS [CalcProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [CalcProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "WHERE [CalcProductIncome].ID = [ProductIncome].ID " +
                "AND [CalcProductIncome].[IsHide] = 0 " +
                ") " +
                ", 14) AS [GrossPrice] " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "AND [ProductIncomeItem].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "WHERE [ProductIncome].Deleted = 0 " +
                "AND [ProductIncome].ProductIncomeType = 1 " +
                "AND [ProductIncome].FromDate >= @From " +
                "AND [ProductIncome].FromDate <= @To " +
                "AND [SupplyOrderUkraine].ClientAgreementID = @Id " +
                "AND [ProductIncome].[IsHide] = 0 " +
                ") " +
                "SELECT * " +
                "FROM [AccountingCashFlow_CTE] " +
                "ORDER BY [AccountingCashFlow_CTE].FromDate",
                (service, grossPrice) => {
                    switch (service.Type) {
                        case JoinServiceType.OutcomePaymentOrder:
                            currentStepBalance = Math.Round(currentStepBalance - grossPrice, 2);

                            accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    IsCreditValue = true,
                                    CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                    Type = service.Type,
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol:
                        case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                        case JoinServiceType.ProductIncomePL:
                        case JoinServiceType.ProductIncomeUK:
                            currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                            accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    IsCreditValue = false,
                                    CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                    Type = service.Type,
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.IncomePaymentOrder:
                            currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                            accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                            accountingCashFlow.AccountingCashFlowHeadItems.Add(
                                new AccountingCashFlowHeadItem {
                                    CurrentBalance = currentStepBalance,
                                    FromDate = service.FromDate,
                                    Type = service.Type,
                                    CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                    Id = service.Id
                                }
                            );
                            break;
                        case JoinServiceType.ContainerService:
                        case JoinServiceType.CustomService:
                        case JoinServiceType.PortWorkService:
                        case JoinServiceType.TransportationService:
                        case JoinServiceType.PortCustomAgencyService:
                        case JoinServiceType.CustomAgencyService:
                        case JoinServiceType.PlaneDeliveryService:
                        case JoinServiceType.VehicleDeliveryService:
                        case JoinServiceType.ConsumablesOrder:
                        case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                        default:
                            break;
                    }

                    return service;
                },
                new { preDefinedClientAgreement.Id, From = from, To = to },
                splitOn: "ID,GrossPrice"
            );

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder))) {
            string sqlExpression =
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentMovementOperation].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [PaymentMovementOperation].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [IncomePaymentOrder].UserID " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "WHERE [IncomePaymentOrder].ID IN @Ids";

            Type[] types = {
                typeof(IncomePaymentOrder),
                typeof(Organization),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(PaymentCurrencyRegister),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], IncomePaymentOrder> mapper = objects => {
                IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                Currency currency = (Currency)objects[2];
                PaymentRegister paymentRegister = (PaymentRegister)objects[3];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User user = (User)objects[7];
                SupplyOrganization incomeClient = (SupplyOrganization)objects[8];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[9];
                Currency agreementCurrency = (Currency)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.IncomePaymentOrder) && i.Id.Equals(incomePaymentOrder.Id));

                itemFromList.IsAccounting = incomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = incomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.IncomePaymentOrder != null) {
                    itemFromList.Number = itemFromList.IncomePaymentOrder.Number;
                } else {
                    if (!string.IsNullOrEmpty(incomePaymentOrder.Number))
                        itemFromList.Number = incomePaymentOrder.Number;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = agreementCurrency;

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);

                    incomePaymentOrder.Organization = organization;
                    incomePaymentOrder.User = user;
                    incomePaymentOrder.Currency = currency;
                    incomePaymentOrder.PaymentRegister = paymentRegister;
                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    incomePaymentOrder.SupplyOrganization = incomeClient;
                    incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    itemFromList.IncomePaymentOrder = incomePaymentOrder;
                }

                return incomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                commandTimeout: 3600
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderPaymentDeliveryProtocol),
                typeof(SupplyOrderPaymentDeliveryProtocolKey),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyProForm),
                typeof(ProFormDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization)
            };

            Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask) objects[2];
                User user = (User)objects[3];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                Client invoiceClient = (Client)objects[6];
                Region invoiceClientRegion = (Region)objects[7];
                RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                Country invoiceClientCountry = (Country)objects[9];
                ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                Agreement invoiceClientAgreement = (Agreement)objects[19];
                ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                Pricing invoiceClientPricing = (Pricing)objects[22];
                PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                Organization invoiceOrganization = (Organization)objects[26];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                Client proFormClient = (Client)objects[35];
                Region proFormClientRegion = (Region)objects[36];
                RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                Country proFormClientCountry = (Country)objects[38];
                ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                Agreement proFormClientAgreement = (Agreement)objects[48];
                ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                Pricing proFormClientPricing = (Pricing)objects[51];
                PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                Organization proFormClientAgreementOrganization = (Organization)objects[53];
                Currency proFormClientAgreementCurrency = (Currency)objects[54];
                Organization proFormOrganization = (Organization)objects[55];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)
                                    && i.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));

                itemFromList.OrganizationName =
                    invoiceClientAgreementOrganization?.Name
                    ?? invoiceOrganization?.Name
                    ?? proFormClientAgreementOrganization?.Name
                    ?? proFormOrganization?.Name
                    ?? "";

                if (itemFromList.SupplyOrderPaymentDeliveryProtocol != null) {
                    if (invoiceSupplyOrder != null)
                        itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();
                    else if (proFormSupplyOrder != null)
                        itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                    if (supplyInvoice != null) {
                        if (invoiceDocument != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null &&
                            !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }
                    }

                    if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                    if (proFormDocument != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null &&
                        !itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                        itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormSupplyOrder == null || itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                        return supplyOrderPaymentDeliveryProtocol;

                    proFormSupplyOrder.Client = proFormClient;
                    proFormSupplyOrder.Organization = proFormOrganization;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                } else {
                    if (supplyInvoice != null) {
                        itemFromList.Number = invoiceSupplyOrder.SupplyOrderNumberId.ToString();

                        if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }

                        if (invoiceClient != null) {
                            if (invoiceClientBankDetails != null) {
                                if (invoiceClientBankDetailAccountNumber != null) {
                                    invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                    invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                }

                                if (invoiceClientClientBankDetailIbanNo != null) {
                                    invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                    invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                }
                            }

                            if (invoiceClientClientAgreement != null) {
                                if (invoiceClientProviderPricing != null) {
                                    if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                    invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                    invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                }

                                invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                            }

                            invoiceClient.Region = invoiceClientRegion;
                            invoiceClient.RegionCode = invoiceClientRegionCode;
                            invoiceClient.Country = invoiceClientCountry;
                            invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                            invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                            invoiceClient.PackingMarking = invoiceClientPackingMarking;
                            invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                        }

                        invoiceSupplyOrder.Client = invoiceClient;
                        invoiceSupplyOrder.Organization = invoiceOrganization;

                        supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                    }

                    if (supplyProForm != null) {
                        itemFromList.Number = proFormSupplyOrder.SupplyOrderNumberId.ToString();

                        if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder != null) {
                            if (proFormClient != null) {
                                if (proFormClientBankDetails != null) {
                                    if (proFormClientBankDetailAccountNumber != null) {
                                        proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                        proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                    }

                                    if (proFormClientClientBankDetailIbanNo != null) {
                                        proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                        proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                    }
                                }

                                if (proFormClientClientAgreement != null) {
                                    if (proFormClientProviderPricing != null) {
                                        if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                        proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                        proFormClientProviderPricing.Pricing = proFormClientPricing;
                                    }

                                    proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                    proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                    proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                    proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                    proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                }

                                proFormClient.Region = proFormClientRegion;
                                proFormClient.RegionCode = proFormClientRegionCode;
                                proFormClient.Country = proFormClientCountry;
                                proFormClient.ClientBankDetails = proFormClientBankDetails;
                                proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                proFormClient.PackingMarking = proFormClientPackingMarking;
                                proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                            }

                            proFormSupplyOrder.Client = proFormClient;
                            proFormSupplyOrder.Organization = proFormOrganization;

                            supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                        }
                    }

                    supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                    supplyOrderPaymentDeliveryProtocol.User = user;
                    supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                    supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                    itemFromList.SupplyOrderPaymentDeliveryProtocol = supplyOrderPaymentDeliveryProtocol;
                }

                return supplyOrderPaymentDeliveryProtocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrderOrganization].OrganizationID " +
                "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                "LEFT JOIN [ProFormDocument] " +
                "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                "WHERE [SupplyOrderPaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            List<long> taskIds = new();

            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[IsAccounting] " +
                ",[OutcomePaymentOrder].[IsManagementAccounting] " +
                ",[OutcomePaymentOrder].[ExchangeRate]" +
                ",[OutcomePaymentOrder].[VAT]" +
                ",[OutcomePaymentOrder].[VatPercent] " +
                ",[dbo].[GetGovExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "[OutcomePaymentOrder].[FromDate] " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.IsAccounting = outcomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = outcomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;

                    if (supplyPaymentTask is not null)
                        taskIds.Add(supplyPaymentTask.Id);
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);

                    if (supplyPaymentTask is not null)
                        taskIds.Add(supplyPaymentTask.Id);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);

            if (taskIds.Any()) {
                Type[] includesTypes = {
                    typeof(SupplyOrderPaymentDeliveryProtocol),
                    typeof(SupplyOrderPaymentDeliveryProtocolKey),
                    typeof(SupplyPaymentTask),
                    typeof(User),
                    typeof(SupplyInvoice),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization),
                    typeof(InvoiceDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyProForm),
                    typeof(ProFormDocument),
                    typeof(SupplyInformationDeliveryProtocol),
                    typeof(SupplyInformationDeliveryProtocolKey),
                    typeof(SupplyOrder),
                    typeof(Client),
                    typeof(Region),
                    typeof(RegionCode),
                    typeof(Country),
                    typeof(ClientBankDetails),
                    typeof(ClientBankDetailAccountNumber),
                    typeof(Currency),
                    typeof(ClientBankDetailIbanNo),
                    typeof(Currency),
                    typeof(TermsOfDelivery),
                    typeof(PackingMarking),
                    typeof(PackingMarkingPayment),
                    typeof(ClientAgreement),
                    typeof(Agreement),
                    typeof(ProviderPricing),
                    typeof(Currency),
                    typeof(Pricing),
                    typeof(PriceType),
                    typeof(Organization),
                    typeof(Currency),
                    typeof(Organization)
                };

                Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                    SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                    SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                    SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                    User user = (User)objects[3];
                    SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                    SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                    Client invoiceClient = (Client)objects[6];
                    Region invoiceClientRegion = (Region)objects[7];
                    RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                    Country invoiceClientCountry = (Country)objects[9];
                    ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                    ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                    Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                    ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                    Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                    TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                    PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                    PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                    ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                    Agreement invoiceClientAgreement = (Agreement)objects[19];
                    ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                    Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                    Pricing invoiceClientPricing = (Pricing)objects[22];
                    PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                    Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                    Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                    Organization invoiceOrganization = (Organization)objects[26];
                    InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                    SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                    SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                    SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                    ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                    SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                    SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                    SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                    Client proFormClient = (Client)objects[35];
                    Region proFormClientRegion = (Region)objects[36];
                    RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                    Country proFormClientCountry = (Country)objects[38];
                    ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                    ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                    Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                    ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                    Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                    TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                    PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                    PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                    ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                    Agreement proFormClientAgreement = (Agreement)objects[48];
                    ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                    Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                    Pricing proFormClientPricing = (Pricing)objects[51];
                    PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                    Organization proFormClientAgreementOrganization = (Organization)objects[53];
                    Currency proFormClientAgreementCurrency = (Currency)objects[54];
                    Organization proFormOrganization = (Organization)objects[55];

                    AccountingCashFlowHeadItem itemFromList =
                        accountingCashFlow.AccountingCashFlowHeadItems
                            .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder)
                                        && i.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id)));

                    itemFromList.OrganizationName = invoiceClientAgreementOrganization?.Name
                                                    ?? invoiceOrganization?.Name
                                                    ?? proFormClientAgreementOrganization?.Name
                                                    ?? proFormOrganization?.Name
                                                    ?? "";

                    OutcomePaymentOrderSupplyPaymentTask junctionFromList =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.First(j => j.SupplyPaymentTaskId.Equals(supplyPaymentTask.Id));

                    if (junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Any(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id))) {
                        SupplyOrderPaymentDeliveryProtocol protocol =
                            junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.First(p => p.Id.Equals(supplyOrderPaymentDeliveryProtocol.Id));
                        if (supplyInvoice != null) {
                            if (invoiceDocument != null && !protocol.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                                protocol.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null &&
                                !protocol.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                protocol.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }
                        }

                        if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                        if (proFormDocument != null && !protocol.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                            protocol.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null &&
                            !protocol.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            protocol.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder == null || protocol.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                            return supplyOrderPaymentDeliveryProtocol;

                        proFormSupplyOrder.Client = proFormClient;
                        proFormSupplyOrder.Organization = proFormOrganization;

                        protocol.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                    } else {
                        if (supplyInvoice != null) {
                            if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                            if (invoiceInformationProtocol != null) {
                                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                                supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                            }

                            if (invoiceClient != null) {
                                if (invoiceClientBankDetails != null) {
                                    if (invoiceClientBankDetailAccountNumber != null) {
                                        invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                        invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                    }

                                    if (invoiceClientClientBankDetailIbanNo != null) {
                                        invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                        invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                    }
                                }

                                if (invoiceClientClientAgreement != null) {
                                    if (invoiceClientProviderPricing != null) {
                                        if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                        invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                        invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                    }

                                    invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                    invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                    invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                    invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                    invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                                }

                                invoiceClient.Region = invoiceClientRegion;
                                invoiceClient.RegionCode = invoiceClientRegionCode;
                                invoiceClient.Country = invoiceClientCountry;
                                invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                                invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                                invoiceClient.PackingMarking = invoiceClientPackingMarking;
                                invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                            }

                            invoiceSupplyOrder.Client = invoiceClient;
                            invoiceSupplyOrder.Organization = invoiceOrganization;

                            supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                        }

                        if (supplyProForm != null) {
                            if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                            if (proFormInformationProtocol != null) {
                                proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                                supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                            }

                            if (proFormSupplyOrder != null) {
                                if (proFormClient != null) {
                                    if (proFormClientBankDetails != null) {
                                        if (proFormClientBankDetailAccountNumber != null) {
                                            proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                            proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                        }

                                        if (proFormClientClientBankDetailIbanNo != null) {
                                            proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                            proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                        }
                                    }

                                    if (proFormClientClientAgreement != null) {
                                        if (proFormClientProviderPricing != null) {
                                            if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                            proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                            proFormClientProviderPricing.Pricing = proFormClientPricing;
                                        }

                                        proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                        proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                        proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                        proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                        proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                    }

                                    proFormClient.Region = proFormClientRegion;
                                    proFormClient.RegionCode = proFormClientRegionCode;
                                    proFormClient.Country = proFormClientCountry;
                                    proFormClient.ClientBankDetails = proFormClientBankDetails;
                                    proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                    proFormClient.PackingMarking = proFormClientPackingMarking;
                                    proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                                }

                                proFormSupplyOrder.Client = proFormClient;
                                proFormSupplyOrder.Organization = proFormOrganization;

                                supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                            }
                        }

                        supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                        supplyOrderPaymentDeliveryProtocol.User = user;
                        supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                        supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                        junctionFromList.SupplyPaymentTask.PaymentDeliveryProtocols.Add(supplyOrderPaymentDeliveryProtocol);
                    }

                    return supplyOrderPaymentDeliveryProtocol;
                };

                _connection.Query(
                    "SELECT * " +
                    "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                    "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                    "LEFT JOIN [SupplyPaymentTask] " +
                    "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                    "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                    "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                    "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                    "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                    "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                    "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                    "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                    "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                    "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                    "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                    "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                    "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                    "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                    "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                    "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                    "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                    "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                    "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                    "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                    "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                    "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrder].OrganizationID " +
                    "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [InvoiceDocument] " +
                    "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                    "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyProForm] " +
                    "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                    "LEFT JOIN [ProFormDocument] " +
                    "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormDocument].Deleted = 0 " +
                    "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                    "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                    "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                    "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                    "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                    "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                    "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                    "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                    "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                    "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                    "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                    "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                    "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                    "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                    "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                    "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                    "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                    "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                    "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                    "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                    "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                    "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                    "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                    "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                    "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                    "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                    "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                    "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                    "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                    "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                    "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                    "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                    "LEFT JOIN ( " +
                    "SELECT [PriceType].ID " +
                    ",[PriceType].Created " +
                    ",[PriceType].Deleted " +
                    ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                    ",[PriceType].NetUID " +
                    ",[PriceType].Updated " +
                    "FROM [PriceType] " +
                    "LEFT JOIN [PriceTypeTranslation] " +
                    "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                    "AND [PriceTypeTranslation].CultureCode = @Culture " +
                    "AND [PriceTypeTranslation].Deleted = 0 " +
                    ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                    "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                    "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                    "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                    "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                    "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                    "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                    "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                    "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                    "WHERE [SupplyPaymentTask].ID IN @Ids",
                    includesTypes,
                    includesMapper,
                    new {
                        Ids = taskIds,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                    }
                );
            }
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol))) {
            Type[] includesTypes = {
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

            Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> includesMapper = objects => {
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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol) && i.Id.Equals(protocol.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                supplyPaymentTask.User = supplyPaymentTaskUser;

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

                if (!string.IsNullOrEmpty(supplyOrderUkraine.Number))
                    itemFromList.Number = supplyOrderUkraine.Number;

                itemFromList.SupplyOrderUkrainePaymentDeliveryProtocol = protocol;

                itemFromList.Comment = supplyOrderUkraine.Comment;

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
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ProductIncomePL))) {
            Type[] includesTypes = {
                typeof(ProductIncome),
                typeof(User),
                typeof(Storage),
                typeof(ProductIncomeItem),
                typeof(PackingListPackageOrderItem),
                typeof(SupplyInvoiceOrderItem),
                typeof(SupplyOrderItem),
                typeof(Product),
                typeof(PackingList),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Organization)
            };

            Func<object[], ProductIncome> includesMapper = objects => {
                ProductIncome productIncome = (ProductIncome)objects[0];
                User user = (User)objects[1];
                Storage storage = (Storage)objects[2];
                ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
                PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[4];
                SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[5];
                SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[6];
                Product product = (Product)objects[7];
                PackingList packingList = (PackingList)objects[8];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
                Organization organization = (Organization)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ProductIncomePL) && i.Id.Equals(productIncome.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ProductIncome != null) {
                    if (!string.IsNullOrEmpty(itemFromList.ProductIncome.Number))
                        itemFromList.Number = itemFromList.ProductIncome.Number;

                    if (productIncomeItem == null) return productIncome;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    packingList.SupplyInvoice = supplyInvoice;

                    itemFromList.Comment = supplyInvoice.Comment;

                    packingListPackageOrderItem.PackingList = packingList;
                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingListPackageOrderItem.TotalNetPrice =
                        decimal.Round(
                            packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(productIncomeItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                    itemFromList.ProductIncome.TotalNetPrice =
                        decimal.Round(
                            itemFromList.ProductIncome.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    itemFromList.ProductIncome.ProductIncomeItems.Add(productIncomeItem);
                } else {
                    if (productIncomeItem != null) {
                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                        supplyInvoiceOrderItem.Product = product;

                        packingList.SupplyInvoice = supplyInvoice;

                        itemFromList.Comment = supplyInvoice.Comment;

                        packingListPackageOrderItem.PackingList = packingList;
                        packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                        packingListPackageOrderItem.TotalNetPrice =
                            decimal.Round(
                                packingListPackageOrderItem.UnitPriceEur * Convert.ToDecimal(productIncomeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                        productIncome.TotalNetPrice =
                            decimal.Round(
                                productIncome.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        productIncome.ProductIncomeItems.Add(productIncomeItem);
                    }

                    productIncome.User = user;
                    productIncome.Storage = storage;

                    if (!string.IsNullOrEmpty(productIncome.Number))
                        itemFromList.Number = productIncome.Number;

                    itemFromList.ProductIncome = productIncome;
                }

                return productIncome;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductIncome].UserID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductIncome].ID IN @Ids " +
                "AND [ProductIncome].[IsHide] = 0 ",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ProductIncomePL)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ProductIncomeUK))) {
            Type[] includesTypes = {
                typeof(ProductIncome),
                typeof(User),
                typeof(Storage),
                typeof(ProductIncomeItem),
                typeof(SupplyOrderUkraineItem),
                typeof(Product),
                typeof(SupplyOrderUkraine),
                typeof(Organization)
            };

            Func<object[], ProductIncome> includesMapper = objects => {
                ProductIncome productIncome = (ProductIncome)objects[0];
                User user = (User)objects[1];
                Storage storage = (Storage)objects[2];
                ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
                SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[4];
                Product product = (Product)objects[5];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
                Organization organization = (Organization)objects[7];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ProductIncomeUK) && i.Id.Equals(productIncome.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ProductIncome != null) {
                    if (!string.IsNullOrEmpty(itemFromList.ProductIncome.Number))
                        itemFromList.Number = itemFromList.ProductIncome.Number;

                    if (productIncomeItem == null) return productIncome;

                    supplyOrderUkraineItem.Product = product;
                    supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

                    itemFromList.Comment = supplyOrderUkraine.Comment;

                    supplyOrderUkraineItem.NetPrice =
                        decimal.Round(
                            supplyOrderUkraineItem.UnitPriceLocal * Convert.ToDecimal(productIncomeItem.Qty),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

                    itemFromList.ProductIncome.TotalNetPrice =
                        decimal.Round(
                            itemFromList.ProductIncome.TotalNetPrice + supplyOrderUkraineItem.NetPrice,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    itemFromList.ProductIncome.ProductIncomeItems.Add(productIncomeItem);
                } else {
                    if (productIncomeItem != null) {
                        supplyOrderUkraineItem.Product = product;
                        supplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;

                        itemFromList.Comment = supplyOrderUkraine.Comment;

                        supplyOrderUkraineItem.NetPrice =
                            decimal.Round(
                                supplyOrderUkraineItem.UnitPriceLocal * Convert.ToDecimal(productIncomeItem.Qty),
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;

                        productIncome.TotalNetPrice =
                            decimal.Round(
                                productIncome.TotalNetPrice + supplyOrderUkraineItem.NetPriceLocal,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                        productIncome.ProductIncomeItems.Add(productIncomeItem);
                    }

                    productIncome.User = user;
                    productIncome.Storage = storage;

                    itemFromList.Number = productIncome.Number;

                    itemFromList.ProductIncome = productIncome;
                }

                return productIncome;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductIncome] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ProductIncome].UserID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductIncome].StorageID " +
                "LEFT JOIN [ProductIncomeItem] " +
                "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
                "LEFT JOIN [SupplyOrderUkraineItem] " +
                "ON [ProductIncomeItem].SupplyOrderUkraineItemID = [SupplyOrderUkraineItem].ID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductIncome].ID IN @Ids " +
                "AND [ProductIncome].[IsHide] = 0 ",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ProductIncomeUK)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return accountingCashFlow;
    }

    public AccountingCashFlow GetRangedByClient(Client client, DateTime from, DateTime to, bool isFromEcommerce = false) {
        AccountingCashFlow accountingCashFlow = new(client);

        string beforeRangeInQuery =
            "SELECT " +
            "ROUND( " +
            "( " +
            "SELECT " +
            "ISNULL( " +
            "SUM( " +
            "[dbo].GetExchangedToEuroValue( " +
            "[IncomePaymentOrder].EuroAmount " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ")" +
            ") " +
            ", 0) " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [IncomePaymentOrder].[CurrencyID] " +
            "WHERE [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "AND [IncomePaymentOrder].ClientID = @Id " +
            "AND [IncomePaymentOrder].ClientAgreementID IS NOT NULL " +
            "AND [IncomePaymentOrder].FromDate < @From ";
        beforeRangeInQuery +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        beforeRangeInQuery +=
            ") + " +
            "( " +
            "SELECT " +
            "ISNULL( " +
            "SUM([SaleReturnItem].Amount) " +
            ", 0) " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = SaleReturn.ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "On [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [SaleReturn].ClientID = @Id " +
            "AND [SaleReturn].FromDate < @From ";

        beforeRangeInQuery +=
            isFromEcommerce
                ? "AND [Agreement].ForReSale = 0 ) "
                : ") ";

        beforeRangeInQuery +=
            !isFromEcommerce
                ? "- " +
                  "( " +
                  "SELECT " +
                  "ISNULL( " +
                  "SUM( " +
                  "([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                  "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, [Currency].Code, @From), 1) " +
                  ") " +
                  ", 0) " +
                  "FROM [ReSale] " +
                  "LEFT JOIN [ReSaleItem] " +
                  "ON [ReSaleItem].ReSaleID = [ReSale].ID " +
                  "AND [ReSaleItem].[Deleted] = 0 " +
                  "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                  "LEFT JOIN [Agreement] " +
                  "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                  "LEFT JOIN [Currency] " +
                  "ON [Currency].ID = [Agreement].CurrencyID " +
                  "WHERE [ClientAgreement].ClientID = @Id " +
                  "AND [ReSale].ChangedToInvoice < @From " +
                  "AND [ReSale].[IsCompleted] = 1 " +
                  ") "
                : "";
        beforeRangeInQuery += ", 2) AS [BeforeRangeOutAmount]";


        accountingCashFlow.BeforeRangeInAmount =
            _connection.Query<decimal>(
                beforeRangeInQuery,
                new { client.Id, From = from }
            ).Single();

        string beforeRangeOutQuery =
            "SELECT " +
            "ROUND( " +
            "( " +
            "SELECT ISNULL(" +
            "SUM(" +
            "CASE WHEN [Sale].IsImported = 1 THEN " +
            "[dbo].GetExchangedToEuroValue([OrderItem].PricePerItem * ( " +
            "CASE " +
            "WHEN [Currency].[Code] = 'EUR' " +
            "THEN 1 " +
            "ELSE [OrderItem].[ExchangeRateAmount] " +
            "END " +
            ") * CONVERT(money, [OrderItem].Qty), [Currency].[ID], GETUTCDATE()) " +
            "ELSE " +
            "[dbo].GetExchangedToEuroValue([OrderItem].PricePerItem * ( " +
            "CASE " +
            "WHEN [Currency].[Code] = 'EUR' " +
            "THEN 1 " +
            "ELSE [OrderItem].[ExchangeRateAmount] " +
            "END " +
            ") * CONVERT(money, [OrderItem].Qty), [Currency].[ID], GETUTCDATE()) " +
            "END" +
            ")" +
            ", 0) " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [Sale].ChangedToInvoice < @From " +
            "AND [ClientAgreement].ClientID = @Id " +
            ") " +
            "+ " +
            "( " +
            "SELECT " +
            "ISNULL( " +
            "SUM( " +
            "[dbo].GetExchangedToEuroValue( " +
            "[OutcomePaymentOrder].[AfterExchangeAmount] " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ") " +
            ") " +
            ", 0) " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND [ClientAgreement].ClientID = @Id " +
            "AND [OutcomePaymentOrder].FromDate < @From " +
            ") " +
            "+ " +
            "( " +
            "SELECT ISNULL(" +
            "SUM(" +
            "[dbo].GetExchangedToEuroValue( " +
            "[SaleInvoiceDocument].ShippingAmount " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ") " +
            ")" +
            ", 0) " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [Sale].ChangedToInvoice < @From " +
            "AND [ClientAgreement].ClientID = @Id ";

        beforeRangeOutQuery +=
            isFromEcommerce
                ? "AND [Agreement].ForReSale = 0 ) "
                : ") ";

        beforeRangeOutQuery +=
            !isFromEcommerce
                ? "+ " +
                  "( " +
                  "SELECT " +
                  "ISNULL( " +
                  "SUM( " +
                  "([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                  "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, [Currency].Code, @From), 1) " +
                  ") " +
                  ", 0) " +
                  "FROM [ReSale] " +
                  "LEFT JOIN [ReSaleItem] " +
                  "ON [ReSaleItem].ReSaleID = [ReSale].ID " +
                  "AND [ReSaleItem].[Deleted] = 0 " +
                  "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                  "LEFT JOIN [Agreement] " +
                  "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                  "LEFT JOIN [Currency] " +
                  "ON [Currency].ID = [Agreement].CurrencyID " +
                  "WHERE [ClientAgreement].ClientID = @Id " +
                  "AND [ReSale].ChangedToInvoice < @From " +
                  "AND [ReSale].[IsCompleted] = 1 " +
                  ") "
                : "";

        beforeRangeOutQuery += ", 2) AS [BeforeRangeInAmount]";

        accountingCashFlow.BeforeRangeOutAmount =
            _connection.Query<decimal>(
                beforeRangeOutQuery,
                new { client.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        List<JoinService> joinServices = new();
        List<JoinServiceToAdd> joinServicesToAdd = new();

        string accountingCashFlowQuery =
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT [OutcomePaymentOrder].ID AS [ID] " +
            ", 11 AS [Type] " +
            ", [OutcomePaymentOrder].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", [dbo].GetExchangedToEuroValue( " +
            "[OutcomePaymentOrder].[Amount] " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ") AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "WHERE [ClientAgreement].ClientID = @Id " +
            "AND [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND [OutcomePaymentOrder].FromDate >= @From " +
            "AND [OutcomePaymentOrder].FromDate <= @To " +
            "AND [ClientAgreement].[Deleted] = 0 " +
            "UNION " +
            "SELECT [IncomePaymentOrder].ID AS [ID] " +
            ", 12 AS [Type] " +
            ", [IncomePaymentOrder].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", [dbo].GetExchangedToEuroValue( " +
            "[IncomePaymentOrder].[Amount] " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ") AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [IncomePaymentOrder].[CurrencyID] " +
            "WHERE [IncomePaymentOrder].ClientID = @Id " +
            "AND [IncomePaymentOrder].FromDate >= @From " +
            "AND [IncomePaymentOrder].FromDate <= @To " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "AND [IncomePaymentOrder].ClientAgreementID IS NOT NULL ";
        accountingCashFlowQuery +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        accountingCashFlowQuery +=
            "UNION " +
            "SELECT [Sale].ID AS [ID] " +
            ", 13 AS [Type] " +
            ", [Sale].ChangedToInvoice AS [FromDate] " +
            ", (CASE " +
            "WHEN [Workplace].ID IS NULL " +
            "THEN NULL " +
            "ELSE CONCAT([Workplace].FirstName, ' ', [Workplace].LastName) " +
            "END) AS [ResponsibleName] " +
            ",(( " +
            "SELECT ISNULL( " +
            "SUM( " +
            "CASE WHEN [ForCalculationSale].IsImported = 1 " +
            "THEN " +
            "[dbo].GetExchangedToEuroValue([OrderItem].PricePerItem * ( " +
            "CASE " +
            "WHEN [Currency].[Code] = 'EUR' " +
            "THEN 1 " +
            "ELSE [OrderItem].[ExchangeRateAmount] " +
            "END " +
            ") * [OrderItem].Qty, [Currency].[ID], GETUTCDATE()) " +
            "ELSE " +
            "[dbo].GetExchangedToEuroValue([OrderItem].PricePerItem * ( " +
            "CASE " +
            "WHEN [Currency].[Code] = 'EUR' " +
            "THEN 1 " +
            "ELSE [OrderItem].[ExchangeRateAmount] " +
            "END " +
            ") * [OrderItem].Qty, [Currency].[ID], GETUTCDATE()) " +
            "END " +
            ") " +
            ", 0) " +
            "FROM [Sale] AS [ForCalculationSale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [ForCalculationSale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ForCalculationSale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ForCalculationSale].ID = [Sale].ID " +
            "AND [OrderItem].Qty > 0 " +
            ") " +
            "+ " +
            "( " +
            "SELECT ISNULL( " +
            "SUM( " +
            "[dbo].GetExchangedToEuroValue( " +
            "[SaleInvoiceDocument].ShippingAmount " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ") " +
            ") " +
            ", 0) " +
            "FROM [Sale] AS [ForCalculationSale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [ForCalculationSale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ForCalculationSale].ClientAgreementID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ForCalculationSale].ID = [Sale].ID " +
            ")) AS [GrossPrice] " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "LEFT JOIN [Workplace] " +
            "ON [Workplace].ID = [Sale].WorkplaceID " +
            "WHERE [ClientAgreement].ClientID = @Id " +
            "AND [Sale].ChangedToInvoice >= @From " +
            "AND [Sale].ChangedToInvoice <= @To ";
        accountingCashFlowQuery +=
            isFromEcommerce
                ? "AND [Agreement].ForReSale = 0"
                : "";
        accountingCashFlowQuery +=
            "AND ( " +
            "[SaleBaseShiftStatus].ShiftStatus IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1 " +
            ") " +
            "AND [ClientAgreement].[Deleted] = 0 " +
            "UNION " +
            "SELECT [SaleReturn].ID " +
            ", 15 AS [Type] " +
            ", [SaleReturn].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", ROUND( " +
            "( " +
            "SELECT ISNULL(SUM([SaleReturnItem].Amount), 0) " +
            "FROM [SaleReturn] AS [CalcReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [CalcReturn].ID = [SaleReturnItem].SaleReturnID " +
            "WHERE [CalcReturn].ID = [SaleReturn].ID " +
            ") " +
            ", 2) AS [GrossPrice] " +
            "FROM [SaleReturn] " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To " +
            "AND [SaleReturn].ClientID = @Id ";

        accountingCashFlowQuery +=
            !isFromEcommerce
                ? "UNION " +
                  "SELECT [ReSale].ID " +
                  ", 37 AS [Type] " +
                  ", [ReSale].ChangedToInvoice AS FromDate " +
                  ", NULL AS [ResponsibleName] " +
                  ", ROUND( " +
                  "( " +
                  "SELECT ISNULL( " +
                  "( " +
                  "SUM(([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                  "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, 'EUR', @From), 1) " +
                  ") " +
                  "), 0 " +
                  ") " +
                  "FROM [ReSale] AS [CalcReturn] " +
                  "LEFT JOIN [ReSaleItem] " +
                  "ON [ReSaleItem].ReSaleID = [CalcReturn].ID " +
                  "AND [ReSaleItem].[Deleted] = 0 " +
                  "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [CalcReturn].ClientAgreementID " +
                  "LEFT JOIN [Agreement] " +
                  "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                  "LEFT JOIN [Currency] " +
                  "ON [Currency].ID = [Agreement].CurrencyID " +
                  "WHERE [CalcReturn].ID = [ReSale].ID " +
                  ") " +
                  ", 2) AS [GrossPrice] " +
                  "FROM [ReSale] " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                  "WHERE [ReSale].Deleted = 0 " +
                  "AND [ReSale].ChangedToInvoice >= @From " +
                  "AND [ReSale].ChangedToInvoice <= @To " +
                  "AND [ClientAgreement].ClientID = @Id " +
                  "AND [ReSale].[IsCompleted] = 1 "
                : "";

        accountingCashFlowQuery +=
            ") SELECT * " +
            "FROM [AccountingCashFlow_CTE] " +
            "ORDER BY [AccountingCashFlow_CTE].FromDate ";

        _connection.Query<JoinService, decimal, JoinService>(
            accountingCashFlowQuery,
            (service, grossPrice) => {
                switch (service.Type) {
                    case JoinServiceType.SaleReturn:
                    case JoinServiceType.Sale:
                        service.FromDate = service.FromDate;

                        break;
                }

                joinServicesToAdd.Add(new JoinServiceToAdd(service, grossPrice));

                return service;
            },
            new {
                client.Id,
                From = from,
                To = to
            },
            splitOn: "ID,GrossPrice"
        );

        foreach (JoinServiceToAdd service in joinServicesToAdd.OrderBy(s => s.JoinService.FromDate)) {
            switch (service.JoinService.Type) {
                case JoinServiceType.OutcomePaymentOrder:
                    currentStepBalance = Math.Round(currentStepBalance - service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            Type = service.JoinService.Type,
                            IsCreditValue = true,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.Sale:
                    currentStepBalance = Math.Round(currentStepBalance - service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            IsCreditValue = true,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            Sale = new Sale {
                                TotalAmount = service.GrossPrice
                            },
                            ResponsibleName = service.JoinService.ResponsibleName,
                            Type = service.JoinService.Type,
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.ReSale:
                    currentStepBalance = Math.Round(currentStepBalance - service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            IsCreditValue = true,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            UpdatedReSaleModel = new UpdatedReSaleModel {
                                ReSale = new ReSale { TotalAmount = service.GrossPrice }
                            },
                            Type = service.JoinService.Type,
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.SaleReturn:
                case JoinServiceType.IncomePaymentOrder:
                    currentStepBalance = Math.Round(currentStepBalance + service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            Type = service.JoinService.Type,
                            IsCreditValue = false,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                case JoinServiceType.ContainerService:
                case JoinServiceType.CustomService:
                case JoinServiceType.PortWorkService:
                case JoinServiceType.TransportationService:
                case JoinServiceType.PortCustomAgencyService:
                case JoinServiceType.CustomAgencyService:
                case JoinServiceType.PlaneDeliveryService:
                case JoinServiceType.VehicleDeliveryService:
                case JoinServiceType.ConsumablesOrder:
                default:
                    break;
            }

            joinServices.Add(service.JoinService);
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[ExchangeRate] " +
                ",[OutcomePaymentOrder].[IsAccounting] " +
                ",[OutcomePaymentOrder].[IsManagementAccounting] " +
                ",[OutcomePaymentOrder].[VAT] " +
                ",[OutcomePaymentOrder].[VatPercent]" +
                ",[dbo].[GetExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";
                itemFromList.IsAccounting = outcomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = outcomePaymentOrder.IsManagementAccounting;

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder))) {
            string sqlExpression =
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentMovementOperation].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [PaymentMovementOperation].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [IncomePaymentOrder].UserID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [IncomePaymentOrder].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [IncomePaymentOrderSale] " +
                "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
                "LEFT JOIN [ReSale] " +
                "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "LEFT JOIN [ReSaleItem] " +
                "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
                "AND [ReSaleItem].[Deleted] = 0 " +
                "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                "LEFT JOIN [ReSaleAvailability] " +
                "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
                "LEFT JOIN [ConsignmentItem] " +
                "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
                "LEFT JOIN [Product] " +
                "ON CASE WHEN [OrderItem].[ID] IS NOT NULL THEN [OrderItem].ProductID ELSE [ConsignmentItem].[ProductID] END = [Product].ID " +
                "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[ClientAgreementID] ELSE [ReSale].[ClientAgreementID] END = [SaleClientAgreement].[ID]  " +
                "LEFT JOIN [Agreement] AS [SaleAgreement] " +
                "ON [SaleAgreement].ID = [SaleClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [SaleAgreementCurrency] " +
                "ON [SaleAgreementCurrency].ID = [SaleAgreement].CurrencyID " +
                "AND [SaleAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SaleNumber] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[SaleNumberID] ELSE [ReSale].[SaleNumberID] END = [SaleNumber].ID " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].BaseLifeCycleStatusID ELSE [ReSale].[BaseLifeCycleStatusID] END = [BaseLifeCycleStatus].ID " +
                "LEFT JOIN [BaseSalePaymentStatus] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].BaseSalePaymentStatusID ELSE [ReSale].[BaseSalePaymentStatusID] END = [BaseSalePaymentStatus].ID " +
                "WHERE [IncomePaymentOrder].ID IN @Ids";

            Type[] types = {
                typeof(IncomePaymentOrder),
                typeof(Organization),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(PaymentCurrencyRegister),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(Client),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(IncomePaymentOrderSale),
                typeof(Sale),
                typeof(ReSale),
                typeof(Order),
                typeof(OrderItem),
                typeof(ReSaleItem),
                typeof(ReSaleAvailability),
                typeof(ConsignmentItem),
                typeof(Product),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(SaleNumber),
                typeof(BaseLifeCycleStatus),
                typeof(BaseSalePaymentStatus)
            };

            Func<object[], IncomePaymentOrder> mapper = objects => {
                IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                Currency currency = (Currency)objects[2];
                PaymentRegister paymentRegister = (PaymentRegister)objects[3];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User user = (User)objects[7];
                Client incomeClient = (Client)objects[8];
                ClientAgreement clientAgreement = (ClientAgreement)objects[9];
                Agreement agreement = (Agreement)objects[10];
                Currency agreementCurrency = (Currency)objects[11];
                IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[12];
                Sale sale = (Sale)objects[13];
                ReSale reSale = (ReSale)objects[14];
                Order order = (Order)objects[15];
                OrderItem orderItem = (OrderItem)objects[16];
                ReSaleItem reSaleItem = (ReSaleItem)objects[17];
                ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[18];
                ConsignmentItem consignmentItem = (ConsignmentItem)objects[19];
                Product product = (Product)objects[20];
                ClientAgreement saleClientAgreement = (ClientAgreement)objects[21];
                Agreement saleAgreement = (Agreement)objects[22];
                Currency saleAgreementCurrency = (Currency)objects[23];
                SaleNumber saleNumber = (SaleNumber)objects[24];
                BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[25];
                BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[26];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.IncomePaymentOrder) && i.Id.Equals(incomePaymentOrder.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";
                itemFromList.IsAccounting = incomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = incomePaymentOrder.IsManagementAccounting;

                if (itemFromList.IncomePaymentOrder != null) {
                    itemFromList.Number = itemFromList.IncomePaymentOrder.Number;

                    if (incomePaymentOrderSale == null) return incomePaymentOrder;

                    if (!itemFromList.IncomePaymentOrder.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) {
                        if (orderItem != null) {
                            orderItem.Product = product;

                            order.OrderItems.Add(orderItem);

                            sale.TotalAmount = Math.Round(sale.TotalAmount + orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        }

                        if (reSaleItem != null) {
                            consignmentItem.Product = product;
                            reSaleAvailability.ConsignmentItem = consignmentItem;
                            reSaleItem.ReSaleAvailability = reSaleAvailability;

                            reSale.ReSaleItems.Add(reSaleItem);

                            reSale.TotalAmount = Math.Round(reSale.TotalAmount + reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty), 2);
                        }

                        if (saleClientAgreement != null) {
                            saleAgreement.Currency = saleAgreementCurrency;

                            saleClientAgreement.Agreement = saleAgreement;
                        }

                        if (sale != null) {
                            sale.Order = order;
                            sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            sale.SaleNumber = saleNumber;
                            sale.ClientAgreement = saleClientAgreement;

                            itemFromList.Comment = sale.Comment;
                        }

                        if (reSale != null) {
                            reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            reSale.SaleNumber = saleNumber;
                            reSale.ClientAgreement = saleClientAgreement;

                            itemFromList.Comment = reSale.Comment;
                        }

                        incomePaymentOrderSale.Sale = sale;
                        incomePaymentOrderSale.ReSale = reSale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                    } else {
                        if (orderItem == null && reSaleItem == null) return incomePaymentOrder;

                        IncomePaymentOrderSale fromList = itemFromList.IncomePaymentOrder.IncomePaymentOrderSales.First(s => s.Id.Equals(incomePaymentOrderSale.Id));

                        if (sale != null && orderItem != null) {
                            if (fromList.Sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) return incomePaymentOrder;

                            orderItem.Product = product;

                            fromList.Sale.Order.OrderItems.Add(orderItem);

                            itemFromList.Comment = sale.Comment;

                            fromList.Sale.TotalAmount = Math.Round(fromList.Sale.TotalAmount + orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        }

                        if (reSale == null) return incomePaymentOrder;

                        itemFromList.Comment = reSale.Comment;

                        if (fromList.ReSale.ReSaleItems.Any(i => i.Id.Equals(reSaleItem.Id))) return incomePaymentOrder;

                        consignmentItem.Product = product;

                        reSaleAvailability.ConsignmentItem = consignmentItem;

                        reSaleItem.ReSaleAvailability = reSaleAvailability;

                        fromList.ReSale.ReSaleItems.Add(reSaleItem);

                        fromList.ReSale.TotalAmount = Math.Round(fromList.ReSale.TotalAmount + reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty), 2);
                    }
                } else {
                    if (!string.IsNullOrEmpty(incomePaymentOrder.Number))
                        itemFromList.Number = incomePaymentOrder.Number;

                    if (incomePaymentOrderSale != null) {
                        if (orderItem != null) {
                            orderItem.Product = product;

                            order.OrderItems.Add(orderItem);

                            sale.TotalAmount = Math.Round(sale.TotalAmount + orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        }

                        if (reSaleItem != null) {
                            consignmentItem.Product = product;
                            reSaleAvailability.ConsignmentItem = consignmentItem;
                            reSaleItem.ReSaleAvailability = reSaleAvailability;

                            reSale.TotalAmount = Math.Round(reSale.TotalAmount + reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty), 2);
                        }

                        if (saleClientAgreement != null) {
                            saleAgreement.Currency = saleAgreementCurrency;

                            saleClientAgreement.Agreement = saleAgreement;
                        }

                        if (reSale != null) {
                            reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            reSale.SaleNumber = saleNumber;
                            reSale.ClientAgreement = saleClientAgreement;

                            itemFromList.Comment = reSale.Comment;
                        }

                        if (sale != null) {
                            sale.Order = order;
                            sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            sale.SaleNumber = saleNumber;
                            sale.ClientAgreement = saleClientAgreement;

                            itemFromList.Comment = sale.Comment;
                        }

                        incomePaymentOrderSale.Sale = sale;
                        incomePaymentOrderSale.ReSale = reSale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                    }

                    if (clientAgreement != null) {
                        agreement.Currency = agreementCurrency;

                        clientAgreement.Agreement = agreement;
                    }

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);

                    incomePaymentOrder.Organization = organization;
                    incomePaymentOrder.User = user;
                    incomePaymentOrder.Currency = currency;
                    incomePaymentOrder.PaymentRegister = paymentRegister;
                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    incomePaymentOrder.Client = incomeClient;

                    itemFromList.IncomePaymentOrder = incomePaymentOrder;
                }

                return incomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                commandTimeout: 3600
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.Sale))) {
            string sqlExpression =
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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.Sale) && i.Id.Equals(sale.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.Sale != null && !itemFromList.Sale.IsNew()) {
                    itemFromList.Number = saleNumber.Value;

                    if (orderItem == null || itemFromList.Sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) return sale;

                    orderItem.Product = product;

                    itemFromList.Sale.Order.OrderItems.Add(orderItem);
                } else {
                    itemFromList.Number = saleNumber.Value;

                    if (orderItem != null) {
                        orderItem.Product = product;

                        order.OrderItems.Add(orderItem);
                    }

                    if (clientAgreement != null) {
                        agreement.Currency = currency;

                        clientAgreement.Client = saleClient;
                        clientAgreement.Agreement = agreement;
                    }

                    sale.Order = order;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    sale.ClientAgreement = clientAgreement;
                    sale.SaleNumber = saleNumber;

                    sale.TotalAmount = itemFromList.Sale?.TotalAmount ?? sale.TotalAmount;

                    itemFromList.Sale = sale;

                    itemFromList.Comment = sale.Comment;
                }

                return sale;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new { Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.Sale)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ReSale))) {
            string sqlExpression =
                "SELECT * FROM ReSale " +
                "LEFT JOIN ReSaleItem " +
                "ON ReSaleItem.ReSaleID = ReSale.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.ID = ReSale.ClientAgreementID " +
                "LEFT JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [ReSale].SaleNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN Client " +
                "ON Client.ID = ClientAgreement.ClientID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ReSale].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE ReSale.ID IN @Ids ";

            Type[] types = {
                typeof(ReSale),
                typeof(ReSaleItem),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(SaleNumber),
                typeof(Client),
                typeof(Currency),
                typeof(Organization)
            };

            Func<object[], ReSale> mapper = objects => {
                ReSale reSale = (ReSale)objects[0];
                ReSaleItem reSaleItem = (ReSaleItem)objects[1];
                ClientAgreement clientAgreement = (ClientAgreement)objects[2];
                Agreement agreement = (Agreement)objects[3];
                SaleNumber saleNumber = (SaleNumber)objects[4];
                Client returnClient = (Client)objects[5];
                Currency currency = (Currency)objects[6];
                Organization organization = (Organization)objects[7];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ReSale) && i.Id.Equals(reSale.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.UpdatedReSaleModel.ReSale != null && !itemFromList.UpdatedReSaleModel.ReSale.IsNew()) {
                    itemFromList.Number = saleNumber.Value;

                    if (reSaleItem == null || itemFromList.UpdatedReSaleModel.ReSale.ReSaleItems.Any(i => i.Id.Equals(reSaleItem.Id))) return reSale;

                    itemFromList.UpdatedReSaleModel.ReSale.ReSaleItems.Add(reSaleItem);
                } else {
                    itemFromList.Number = saleNumber.Value;

                    if (reSaleItem != null) reSale.ReSaleItems.Add(reSaleItem);

                    if (clientAgreement != null) {
                        agreement.Currency = currency;

                        clientAgreement.Client = returnClient;
                        clientAgreement.Agreement = agreement;
                    }

                    reSale.ClientAgreement = clientAgreement;
                    reSale.SaleNumber = saleNumber;
                    reSale.Organization = organization;

                    itemFromList.UpdatedReSaleModel.ReSale = reSale;

                    itemFromList.Comment = reSale.Comment;
                }

                return reSale;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ReSale)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SaleReturn))) {
            string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.SaleReturn) && i.Id.Equals(saleReturn.Id));

                decimal vatRatePercent = Convert.ToDecimal(vatRate?.Value ?? 0) / 100;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.SaleReturn != null) {
                    itemFromList.Number = itemFromList.SaleReturn.Number;

                    if (itemFromList.SaleReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) return saleReturn;

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

                    itemFromList.SaleReturn.TotalAmount =
                        decimal.Round(itemFromList.SaleReturn.TotalAmount + saleReturnItem.Amount, 2, MidpointRounding.AwayFromZero);

                    itemFromList.SaleReturn.SaleReturnItems.Add(saleReturnItem);
                } else {
                    itemFromList.Number = saleReturn.Number;

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

                        saleReturn.TotalAmount = saleReturnItem.Amount;

                        saleReturn.SaleReturnItems.Add(saleReturnItem);
                    }

                    returnClient.RegionCode = regionCode;

                    saleReturn.Client = returnClient;
                    saleReturn.CreatedBy = returnCreatedBy;

                    itemFromList.SaleReturn = saleReturn;
                }

                return saleReturn;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new { Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SaleReturn)).Select(s => s.Id), Culture = culture }
            );
        }

        return accountingCashFlow;
    }

    public decimal GetAccountBalanceByClientAgreement(
        long preDefinedClientAgreementId,
        bool isEuroAgreement = false,
        bool isFromEcommerce = false) {
        decimal currentStepBalance = 0;

        List<JoinServiceToAdd> joinServicesToAdd = new();

        string joinServicesSqlExpression =
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT [OutcomePaymentOrder].ID AS [ID] " +
            ", 11 AS [Type] " +
            ", [OutcomePaymentOrder].FromDate " +
            ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "WHERE [OutcomePaymentOrder].ClientAgreementID = @Id " +
            "AND [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND [OutcomePaymentOrder].ClientAgreementID IS NOT NULL " +
            "UNION " +
            "SELECT [IncomePaymentOrder].ID AS [ID] " +
            ", 12 AS [Type] " +
            ", [IncomePaymentOrder].FromDate " +
            ", ( " +
            "SELECT ROUND(SUM(([IncomePaymentOrderSale].Amount + [IncomePaymentOrderSale].OverpaidAmount) * [IncomePaymentOrderSale].ExchangeRate), 2) " +
            "FROM [IncomePaymentOrderSale] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "WHERE [IncomePaymentOrderSale].Deleted = 0 " +
            "AND [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].ClientAgreementID ELSE [ReSale].[ClientAgreementID] END = @Id ";

        joinServicesSqlExpression +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        joinServicesSqlExpression +=
            ") AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "WHERE CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].ClientAgreementID ELSE [ReSale].[ClientAgreementID] END = @Id " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 ";

        joinServicesSqlExpression +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        joinServicesSqlExpression +=
            "UNION " +
            "SELECT [IncomePaymentOrder].ID AS [ID] " +
            ", 12 AS [Type] " +
            ", [IncomePaymentOrder].FromDate " +
            ", ROUND([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 2) AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "WHERE [IncomePaymentOrder].ClientAgreementID = @Id " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "AND [IncomePaymentOrderSale].ID IS NULL " +
            "UNION " +
            "SELECT [Sale].ID AS [ID] " +
            ", 13 AS [Type] " +
            ", [Sale].ChangedToInvoice AS [FromDate] " +
            ", ( " +
            "ROUND(" +
            "(";

        joinServicesSqlExpression +=
            isEuroAgreement
                ? "SELECT ISNULL(" +
                  "SUM(" +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) " +
                  ")" +
                  ", 0) "
                : "SELECT ISNULL(" +
                  "SUM(" +
                  "CASE WHEN [ForCalculationSale].IsImported = 1 " +
                  "THEN " +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) * [OrderItem].ExchangeRateAmount " +
                  "ELSE " +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) * [OrderItem].ExchangeRateAmount " +
                  "END" +
                  "), 0) ";

        joinServicesSqlExpression +=
            "FROM [Sale] AS [ForCalculationSale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [ForCalculationSale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ForCalculationSale].ID = [Sale].ID " +
            "AND [OrderItem].Qty > 0 " +
            ")" +
            "+" +
            "(";

        joinServicesSqlExpression +=
            isEuroAgreement
                ? "SELECT ISNULL(" +
                  "SUM(" +
                  "[SaleInvoiceDocument].ShippingAmountEur " +
                  ")" +
                  ", 0) "
                : "SELECT ISNULL(" +
                  "SUM(" +
                  "[SaleInvoiceDocument].ShippingAmount " +
                  ")" +
                  ", 0) ";

        joinServicesSqlExpression +=
            "FROM [Sale] AS [ForCalculationSale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [ForCalculationSale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ForCalculationSale].ID = [Sale].ID " +
            ")" +
            ", 2)" +
            ") AS [GrossPrice] " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "WHERE [Sale].ClientAgreementID = @Id " +
            "AND ( " +
            "[SaleBaseShiftStatus].ShiftStatus IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1 " +
            ") " +
            "UNION " +
            "SELECT [SaleReturn].ID " +
            ", 15 AS [Type] " +
            ", [SaleReturn].FromDate " +
            ", ( " +
            "SELECT ISNULL(SUM([SaleReturnItem].Amount * [SaleReturnItem].ExchangeRateAmount), 0) " +
            "FROM [SaleReturn] AS [CalcReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [CalcReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "WHERE [CalcReturn].ID = [SaleReturn].ID " +
            "AND [Order].ClientAgreementID = @Id " +
            ") " +
            " AS [GrossPrice] " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [Order].ClientAgreementID = @Id ";

        joinServicesSqlExpression +=
            !isFromEcommerce
                ? "UNION " +
                  "SELECT [ReSale].ID " +
                  ", 37 AS [Type] " +
                  ", [ReSale].ChangedToInvoice AS FromDate " +
                  ", ROUND( " +
                  "( " +
                  "SELECT ISNULL( " +
                  "( " +
                  "SUM(([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                  "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, Currency.Code, @Date), 1) " +
                  ") " +
                  "), 0 " +
                  ") " +
                  "FROM [ReSale] AS [CalcReturn] " +
                  "LEFT JOIN [ReSaleItem] " +
                  "ON [ReSaleItem].ReSaleID = [CalcReturn].ID " +
                  "AND [ReSaleItem].[Deleted] = 0 " +
                  "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [CalcReturn].ClientAgreementID " +
                  "LEFT JOIN [Agreement] " +
                  "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                  "LEFT JOIN [Currency] " +
                  "ON [Currency].ID = [Agreement].CurrencyID " +
                  "WHERE [CalcReturn].ID = [ReSale].ID " +
                  ") " +
                  ", 2) AS [GrossPrice] " +
                  "FROM [ReSale] " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                  "WHERE [ReSale].Deleted = 0 " +
                  "AND [ClientAgreement].Id = @Id " +
                  "AND [ReSale].[IsCompleted] = 1 "
                : "";

        joinServicesSqlExpression +=
            ") SELECT * " +
            "FROM [AccountingCashFlow_CTE] " +
            "ORDER BY [AccountingCashFlow_CTE].FromDate";

        _connection.Query<JoinService, decimal, JoinService>(
            joinServicesSqlExpression,
            (service, grossPrice) => {
                switch (service.Type) {
                    case JoinServiceType.SaleReturn:
                    case JoinServiceType.Sale:
                        service.FromDate = service.FromDate;

                        break;
                }

                joinServicesToAdd.Add(new JoinServiceToAdd(service, decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero)));

                return service;
            },
            new {
                Id = preDefinedClientAgreementId,
                Date = DateTime.Now
            },
            splitOn: "ID,GrossPrice"
        );

        foreach (JoinServiceToAdd service in joinServicesToAdd.OrderBy(s => s.JoinService.FromDate))
            switch (service.JoinService.Type) {
                case JoinServiceType.OutcomePaymentOrder:
                    currentStepBalance = decimal.Round(currentStepBalance - service.GrossPrice, 2, MidpointRounding.AwayFromZero);
                    break;
                case JoinServiceType.Sale:
                    currentStepBalance = decimal.Round(currentStepBalance - service.GrossPrice, 2, MidpointRounding.AwayFromZero);
                    break;
                case JoinServiceType.SaleReturn:
                case JoinServiceType.IncomePaymentOrder:
                    currentStepBalance = decimal.Round(currentStepBalance + service.GrossPrice, 2, MidpointRounding.AwayFromZero);
                    break;
                case JoinServiceType.ReSale:
                    currentStepBalance = decimal.Round(currentStepBalance - service.GrossPrice, 2, MidpointRounding.AwayFromZero);
                    break;
                case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                case JoinServiceType.ContainerService:
                case JoinServiceType.CustomService:
                case JoinServiceType.PortWorkService:
                case JoinServiceType.TransportationService:
                case JoinServiceType.PortCustomAgencyService:
                case JoinServiceType.CustomAgencyService:
                case JoinServiceType.PlaneDeliveryService:
                case JoinServiceType.VehicleDeliveryService:
                case JoinServiceType.ConsumablesOrder:
                default:
                    break;
            }

        return currentStepBalance;
    }

    public AccountingCashFlow GetRangedByClientAgreement(
        ClientAgreement preDefinedClientAgreement,
        DateTime from,
        DateTime to,
        bool isEuroAgreement = false,
        bool isFromEcommerce = false) {
        AccountingCashFlow accountingCashFlow = new(preDefinedClientAgreement);

        string beforeRangeInAmountSqlExpression =
            "SELECT " +
            "ROUND( " +
            "( " +
            "SELECT " +
            "ISNULL( " +
            "SUM(ROUND(([IncomePaymentOrderSale].Amount + [IncomePaymentOrderSale].OverpaidAmount) * [IncomePaymentOrderSale].ExchangeRate, 2)) " +
            ", 0) " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "WHERE [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "AND " +
            "CASE " +
            "WHEN [Sale].[ID] IS NOT NULL " +
            "THEN [Sale].[ClientAgreementID] " +
            "ELSE [ReSale].[ClientAgreementID] " +
            "END = @Id " +
            "AND [IncomePaymentOrder].FromDate < @From ";

        beforeRangeInAmountSqlExpression +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        beforeRangeInAmountSqlExpression +=
            ") " +
            "+ " +
            "( " +
            "SELECT " +
            "ISNULL( " +
            "CASE WHEN SUM([IncomePaymentOrder].AgreementExchangedAmount) <> 0 " +
            "THEN SUM([IncomePaymentOrder].AgreementExchangedAmount) " +
            "ELSE SUM([IncomePaymentOrder].Amount * [IncomePaymentOrder].ExchangeRate) " +
            "END " +
            ", 0) " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale] .IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "WHERE [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "AND [IncomePaymentOrder].ClientAgreementID = @Id " +
            "AND [IncomePaymentOrder].FromDate < @From " +
            "AND [IncomePaymentOrderSale].ID IS NULL ";

        beforeRangeInAmountSqlExpression +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        beforeRangeInAmountSqlExpression +=
            ") " +
            "+ " +
            "( " +
            "SELECT " +
            "ROUND(" +
            "ISNULL( " +
            "SUM(ROUND([SaleReturnItem].Amount * [SaleReturnItem].ExchangeRateAmount, 4)) " +
            ", 0) " +
            ", 2) " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [Order].ClientAgreementID = @Id " +
            "AND [SaleReturn].FromDate < @From " +
            ") ";

        beforeRangeInAmountSqlExpression +=
            !isFromEcommerce
                ? "- " +
                  "( " +
                  "SELECT " +
                  "ISNULL( " +
                  "SUM( " +
                  "([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                  "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, [Currency].Code, @From), 1) " +
                  ") " +
                  ", 0) " +
                  "FROM [ReSale] " +
                  "LEFT JOIN [ReSaleItem] " +
                  "ON [ReSaleItem].ReSaleID = [ReSale].ID " +
                  "AND [ReSaleItem].[Deleted] = 0 " +
                  "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                  "LEFT JOIN [Agreement] " +
                  "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                  "LEFT JOIN [Currency] " +
                  "ON [Currency].ID = [Agreement].CurrencyID " +
                  "WHERE [ClientAgreement].ClientID = @Id " +
                  "AND [ReSale].ChangedToInvoice < @From " +
                  "AND [ReSale].[IsCompleted] = 1 " +
                  ") "
                : "";

        beforeRangeInAmountSqlExpression += ", 2) AS [BeforeRangeInAmount]";

        accountingCashFlow.BeforeRangeInAmount =
            _connection.Query<decimal>(
                beforeRangeInAmountSqlExpression,
                new { preDefinedClientAgreement.Id, From = from }
            ).Single();

        string beforeRangeOutAmountSqlExpression =
            "SELECT " +
            "ROUND( " +
            "( ";

        beforeRangeOutAmountSqlExpression +=
            isEuroAgreement
                ? "SELECT ISNULL(" +
                  "SUM(" +
                  "ROUND(" +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty)" +
                  ", 2)" +
                  ")" +
                  ", 0) "
                : "SELECT ISNULL(" +
                  "SUM( " +
                  "CASE WHEN [Sale].IsImported = 1 " +
                  "THEN " +
                  "ROUND(" +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty)" +
                  ", 4) " +
                  "ELSE " +
                  "ROUND(" +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty)" +
                  ", 2)" +
                  "END" +
                  ") * MAX([OrderItem].ExchangeRateAmount)" +
                  ", 0) ";

        beforeRangeOutAmountSqlExpression +=
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "WHERE [Sale].ChangedToInvoice < @From " +
            "AND [Sale].ClientAgreementID = @Id " +
            ") " +
            "+" +
            "( " +
            "SELECT " +
            "ISNULL( " +
            "SUM([OutcomePaymentOrder].AfterExchangeAmount) " +
            ", 0) " +
            "FROM [OutcomePaymentOrder] " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].ClientAgreementID = @Id " +
            "AND [OutcomePaymentOrder].FromDate < @From " +
            ") " +
            "+ " +
            "( ";

        beforeRangeOutAmountSqlExpression +=
            isEuroAgreement
                ? "SELECT ISNULL(" +
                  "SUM(" +
                  "ROUND(" +
                  "[SaleInvoiceDocument].ShippingAmountEur" +
                  ", 2)" +
                  ")" +
                  ", 0) "
                : "SELECT ISNULL(" +
                  "SUM(" +
                  "ROUND(" +
                  "[SaleInvoiceDocument].ShippingAmount" +
                  ", 2)" +
                  ")" +
                  ", 0) ";

        beforeRangeOutAmountSqlExpression +=
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "WHERE [Sale].ChangedToInvoice < @From " +
            "AND [Sale].ClientAgreementID = @Id " +
            ")" +
            ", 2) AS [BeforeRangeOutAmount]";

        accountingCashFlow.BeforeRangeOutAmount =
            _connection.Query<decimal>(
                beforeRangeOutAmountSqlExpression,
                new { preDefinedClientAgreement.Id, From = from }
            ).Single();

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        List<JoinService> joinServices = new();
        List<JoinServiceToAdd> joinServicesToAdd = new();

        string joinServicesSqlExpression =
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT [OutcomePaymentOrder].ID AS [ID] " +
            ", 11 AS [Type] " +
            ", [OutcomePaymentOrder].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "WHERE [OutcomePaymentOrder].ClientAgreementID = @Id " +
            "AND [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND [OutcomePaymentOrder].FromDate >= @From " +
            "AND [OutcomePaymentOrder].FromDate <= @To " +
            "UNION " +
            "SELECT [IncomePaymentOrder].ID AS [ID] " +
            ", 12 AS [Type] " +
            ", [IncomePaymentOrder].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", ( " +
            "SELECT ROUND(SUM(([IncomePaymentOrderSale].Amount + [IncomePaymentOrderSale].OverpaidAmount) * [IncomePaymentOrderSale].ExchangeRate), 2) " + // TODO this is probably nor correct
            "FROM [IncomePaymentOrderSale] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "WHERE [IncomePaymentOrderSale].Deleted = 0 " +
            "AND [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].ClientAgreementID ELSE [ReSale].[ClientAgreementID] END = @Id ";

        joinServicesSqlExpression +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        joinServicesSqlExpression +=
            ") AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "WHERE CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].ClientAgreementID ELSE [ReSale].[ClientAgreementID] END = @Id " +
            "AND [IncomePaymentOrder].FromDate >= @From " +
            "AND [IncomePaymentOrder].FromDate <= @To " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 ";

        joinServicesSqlExpression +=
            isFromEcommerce
                ? "AND [IncomePaymentOrderSale].ReSaleID IS NULL "
                : "";

        joinServicesSqlExpression +=
            "UNION " +
            "SELECT [IncomePaymentOrder].ID AS [ID] " +
            ", 12 AS [Type] " +
            ", [IncomePaymentOrder].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", CASE WHEN [IncomePaymentOrder].AgreementExchangedAmount <> 0 " +
            "THEN [IncomePaymentOrder].AgreementExchangedAmount " +
            "ELSE ROUND([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 2) " +
            "END AS [GrossPrice] " +
            //", ROUND([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 2) AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "WHERE [IncomePaymentOrder].ClientAgreementID = @Id " +
            "AND [IncomePaymentOrder].FromDate >= @From " +
            "AND [IncomePaymentOrder].FromDate <= @To " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "AND [IncomePaymentOrderSale].ID IS NULL " +
            "UNION " +
            "SELECT [Sale].ID AS [ID] " +
            ", 13 AS [Type] " +
            ", [Sale].ChangedToInvoice AS [FromDate] " +
            ", (CASE " +
            "WHEN [Workplace].ID IS NULL " +
            "THEN NULL " +
            "ELSE CONCAT([Workplace].FirstName, ' ', [Workplace].LastName) " +
            "END) AS [ResponsibleName] " +
            ", ( " +
            "ROUND(" +
            "(";

        joinServicesSqlExpression +=
            isEuroAgreement
                ? "SELECT ISNULL(" +
                  "SUM(" +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) " +
                  ")" +
                  ", 0) "
                : "SELECT ISNULL(" +
                  "SUM(" +
                  "CASE WHEN [ForCalculationSale].IsImported = 1 " +
                  "THEN " +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) * [OrderItem].ExchangeRateAmount " +
                  "ELSE " +
                  "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) * [OrderItem].ExchangeRateAmount " +
                  "END" +
                  "), 0) ";

        joinServicesSqlExpression +=
            "FROM [Sale] AS [ForCalculationSale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [ForCalculationSale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ForCalculationSale].ID = [Sale].ID " +
            "AND [OrderItem].Qty > 0 " +
            ")" +
            "+" +
            "(";

        joinServicesSqlExpression +=
            isEuroAgreement
                ? "SELECT ISNULL(" +
                  "SUM(" +
                  "[SaleInvoiceDocument].ShippingAmountEur " +
                  ")" +
                  ", 0) "
                : "SELECT ISNULL(" +
                  "SUM(" +
                  "[SaleInvoiceDocument].ShippingAmount " +
                  ")" +
                  ", 0) ";

        joinServicesSqlExpression +=
            "FROM [Sale] AS [ForCalculationSale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [ForCalculationSale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ForCalculationSale].ID = [Sale].ID " +
            ")" +
            ", 2)" +
            ") AS [GrossPrice] " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "LEFT JOIN [Workplace] " +
            "ON [Workplace].ID = [Sale].WorkplaceID " +
            "WHERE [Sale].ClientAgreementID = @Id " +
            "AND [Sale].ChangedToInvoice >= @From " +
            "AND [Sale].ChangedToInvoice <= @To " +
            "AND ( " +
            "[SaleBaseShiftStatus].ShiftStatus IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1 " +
            ") " +
            "UNION " +
            "SELECT [SaleReturn].ID " +
            ", 15 AS [Type] " +
            ", [SaleReturn].FromDate " +
            ", NULL AS [ResponsibleName] " +
            ", ( " +
            "SELECT ISNULL(SUM([SaleReturnItem].Amount * [SaleReturnItem].ExchangeRateAmount), 0) " +
            "FROM [SaleReturn] AS [CalcReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [CalcReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "WHERE [CalcReturn].ID = [SaleReturn].ID " +
            "AND [Order].ClientAgreementID = @Id " +
            ") " +
            " AS [GrossPrice] " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To " +
            "AND [Order].ClientAgreementID = @Id ";

        joinServicesSqlExpression +=
            !isFromEcommerce
                ? "UNION " +
                  "SELECT [ReSale].ID " +
                  ", 37 AS [Type] " +
                  ", [ReSale].ChangedToInvoice AS FromDate " +
                  ", NULL AS [ResponsibleName] " +
                  ", ROUND( " +
                  "( " +
                  "SELECT ISNULL( " +
                  "( " +
                  "SUM(([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                  "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, Currency.Code, @From), 1) " +
                  ") " +
                  "), 0 " +
                  ") " +
                  "FROM [ReSale] AS [CalcReturn] " +
                  "LEFT JOIN [ReSaleItem] " +
                  "ON [ReSaleItem].ReSaleID = [CalcReturn].ID " +
                  "AND [ReSaleItem].[Deleted] = 0 " +
                  "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [CalcReturn].ClientAgreementID " +
                  "LEFT JOIN [Agreement] " +
                  "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                  "LEFT JOIN [Currency] " +
                  "ON [Currency].ID = [Agreement].CurrencyID " +
                  "WHERE [CalcReturn].ID = [ReSale].ID " +
                  ") " +
                  ", 2) AS [GrossPrice] " +
                  "FROM [ReSale] " +
                  "LEFT JOIN [ClientAgreement] " +
                  "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                  "WHERE [ReSale].Deleted = 0 " +
                  "AND [ReSale].ChangedToInvoice >= @From " +
                  "AND [ReSale].ChangedToInvoice <= @To " +
                  "AND [ClientAgreement].Id = @Id " +
                  "AND [ReSale].[IsCompleted] = 1 "
                : "";

        joinServicesSqlExpression +=
            ") SELECT * " +
            "FROM [AccountingCashFlow_CTE] " +
            "ORDER BY [AccountingCashFlow_CTE].FromDate";

        _connection.Query<JoinService, decimal, JoinService>(
            joinServicesSqlExpression,
            (service, grossPrice) => {
                switch (service.Type) {
                    case JoinServiceType.SaleReturn:
                    case JoinServiceType.Sale:
                        service.FromDate = service.FromDate;

                        break;
                }

                joinServicesToAdd.Add(new JoinServiceToAdd(service, decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero)));

                return service;
            },
            new {
                preDefinedClientAgreement.Id,
                From = from,
                To = to
            },
            splitOn: "ID,GrossPrice"
        );

        foreach (JoinServiceToAdd service in joinServicesToAdd.OrderBy(s => s.JoinService.FromDate)) {
            switch (service.JoinService.Type) {
                case JoinServiceType.OutcomePaymentOrder:
                    currentStepBalance = decimal.Round(currentStepBalance - service.GrossPrice, 2, MidpointRounding.AwayFromZero);

                    accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            Type = service.JoinService.Type,
                            IsCreditValue = true,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.Sale:
                    currentStepBalance = Math.Round(currentStepBalance - service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            IsCreditValue = true,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            Sale = new Sale {
                                TotalAmount = service.GrossPrice
                            },
                            ResponsibleName = service.JoinService.ResponsibleName,
                            Type = service.JoinService.Type,
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.SaleReturn:
                case JoinServiceType.IncomePaymentOrder:
                    currentStepBalance = Math.Round(currentStepBalance + service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            Type = service.JoinService.Type,
                            IsCreditValue = false,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.ReSale:
                    currentStepBalance = Math.Round(currentStepBalance - service.GrossPrice, 2);

                    accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + service.GrossPrice, 2);

                    accountingCashFlow.AccountingCashFlowHeadItems.Add(
                        new AccountingCashFlowHeadItem {
                            CurrentBalance = currentStepBalance,
                            FromDate = service.JoinService.FromDate,
                            IsCreditValue = true,
                            CurrentValue = decimal.Round(service.GrossPrice, 2, MidpointRounding.AwayFromZero),
                            UpdatedReSaleModel = new UpdatedReSaleModel {
                                ReSale = new ReSale { TotalAmount = service.GrossPrice }
                            },
                            Type = service.JoinService.Type,
                            Id = service.JoinService.Id
                        }
                    );
                    break;
                case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                case JoinServiceType.ContainerService:
                case JoinServiceType.CustomService:
                case JoinServiceType.PortWorkService:
                case JoinServiceType.TransportationService:
                case JoinServiceType.PortCustomAgencyService:
                case JoinServiceType.CustomAgencyService:
                case JoinServiceType.PlaneDeliveryService:
                case JoinServiceType.VehicleDeliveryService:
                case JoinServiceType.ConsumablesOrder:
                default:
                    break;
            }

            joinServices.Add(service.JoinService);
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[IsAccounting] " +
                ",[OutcomePaymentOrder].[IsManagementAccounting] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[ExchangeRate] " +
                ",[OutcomePaymentOrder].[VAT] " +
                ",[OutcomePaymentOrder].[VatPercent]" +
                ",[dbo].[GetGovExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "[OutcomePaymentOrder].[FromDate] " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.IsAccounting = outcomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = outcomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder))) {
            string sqlExpression =
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentMovementOperation].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [PaymentMovementOperation].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [IncomePaymentOrder].UserID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [IncomePaymentOrder].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [IncomePaymentOrderSale] " +
                "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [IncomePaymentOrderSale].Deleted = 0 " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
                "LEFT JOIN [ReSale] " +
                "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
                "LEFT JOIN [ReSaleItem] " +
                "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
                "AND [ReSaleItem].[Deleted] = 0 " +
                "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                "LEFT JOIN [ReSaleAvailability] " +
                "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
                "LEFT JOIN [ConsignmentItem] " +
                "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "AND [OrderItem].Deleted = 0 " +
                "AND [OrderItem].Qty > 0 " +
                "LEFT JOIN [Product] " +
                "ON  CASE WHEN [OrderItem].[ID] IS NOT NULL THEN [OrderItem].ProductID ELSE [ConsignmentItem].[ProductID] END = [Product].ID " +
                "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[ClientAgreementID] ELSE [ReSale].[ClientAgreementID] END = [SaleClientAgreement].ID " +
                "LEFT JOIN [Agreement] AS [SaleAgreement] " +
                "ON [SaleAgreement].ID = [SaleClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [SaleAgreementCurrency] " +
                "ON [SaleAgreementCurrency].ID = [SaleAgreement].CurrencyID " +
                "AND [SaleAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SaleNumber] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].SaleNumberID ELSE [ReSale].[SaleNumberID] END = [SaleNumber].ID " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].BaseLifeCycleStatusID ELSE [ReSale].[BaseLifeCycleStatusID] END = [BaseLifeCycleStatus].ID " +
                "LEFT JOIN [BaseSalePaymentStatus] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].BaseSalePaymentStatusID ELSE [ReSale].[BaseSalePaymentStatusID] END = [BaseSalePaymentStatus].ID " +
                "WHERE [IncomePaymentOrder].ID IN @Ids " +
                "AND ([Sale].ClientAgreementID IS NULL OR [Sale].ClientAgreementID = @Id)";

            Type[] types = {
                typeof(IncomePaymentOrder),
                typeof(Organization),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(PaymentCurrencyRegister),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(Client),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(IncomePaymentOrderSale),
                typeof(Sale),
                typeof(ReSale),
                typeof(ReSaleItem),
                typeof(ReSaleAvailability),
                typeof(ConsignmentItem),
                typeof(Order),
                typeof(OrderItem),
                typeof(Product),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(SaleNumber),
                typeof(BaseLifeCycleStatus),
                typeof(BaseSalePaymentStatus)
            };

            Func<object[], IncomePaymentOrder> mapper = objects => {
                IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                Currency currency = (Currency)objects[2];
                PaymentRegister paymentRegister = (PaymentRegister)objects[3];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User user = (User)objects[7];
                Client incomeClient = (Client)objects[8];
                ClientAgreement clientAgreement = (ClientAgreement)objects[9];
                Agreement agreement = (Agreement)objects[10];
                Currency agreementCurrency = (Currency)objects[11];
                IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[12];
                Sale sale = (Sale)objects[13];
                ReSale reSale = (ReSale)objects[14];
                ReSaleItem reSaleItem = (ReSaleItem)objects[15];
                ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[16];
                ConsignmentItem consignmentItem = (ConsignmentItem)objects[17];
                Order order = (Order)objects[18];
                OrderItem orderItem = (OrderItem)objects[19];
                Product product = (Product)objects[20];
                ClientAgreement saleClientAgreement = (ClientAgreement)objects[21];
                Agreement saleAgreement = (Agreement)objects[22];
                Currency saleAgreementCurrency = (Currency)objects[23];
                SaleNumber saleNumber = (SaleNumber)objects[24];
                BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[25];
                BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[26];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.IncomePaymentOrder) && i.Id.Equals(incomePaymentOrder.Id));

                itemFromList.IsAccounting = incomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = incomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.IncomePaymentOrder != null) {
                    itemFromList.Number = itemFromList.IncomePaymentOrder.Number;

                    if (incomePaymentOrderSale == null) return incomePaymentOrder;

                    if (!itemFromList.IncomePaymentOrder.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) {
                        if (orderItem != null) {
                            orderItem.Product = product;

                            order.OrderItems.Add(orderItem);

                            sale.TotalAmount = Math.Round(sale.TotalAmount + orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        } else if (reSaleItem != null) {
                            consignmentItem.Product = product;
                            reSaleAvailability.ConsignmentItem = consignmentItem;
                            reSaleItem.ReSaleAvailability = reSaleAvailability;
                            reSale.ReSaleItems.Add(reSaleItem);
                            reSale.TotalAmount = Math.Round(reSale.TotalAmount + reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty), 2);
                        }

                        if (saleClientAgreement != null) {
                            saleAgreement.Currency = saleAgreementCurrency;

                            saleClientAgreement.Agreement = saleAgreement;
                        }

                        if (sale != null) {
                            sale.Order = order;
                            sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            sale.SaleNumber = saleNumber;
                            sale.ClientAgreement = saleClientAgreement;
                        } else if (reSale != null) {
                            reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            reSale.SaleNumber = saleNumber;
                            reSale.ClientAgreement = saleClientAgreement;
                        }

                        incomePaymentOrderSale.Sale = sale;
                        incomePaymentOrderSale.ReSale = reSale;

                        itemFromList.IncomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);

                        itemFromList.IncomePaymentOrder.EuroAmount =
                            decimal.Round(
                                itemFromList.IncomePaymentOrder.EuroAmount + (incomePaymentOrderSale.Amount + incomePaymentOrderSale.OverpaidAmount) *
                                incomePaymentOrderSale.ExchangeRate, 2, MidpointRounding.AwayFromZero);
                    } else {
                        if (orderItem != null) {
                            IncomePaymentOrderSale fromList = itemFromList.IncomePaymentOrder.IncomePaymentOrderSales.First(s => s.Id.Equals(incomePaymentOrderSale.Id));

                            if (fromList.Sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) return incomePaymentOrder;

                            orderItem.Product = product;

                            fromList.Sale.Order.OrderItems.Add(orderItem);

                            fromList.Sale.TotalAmount = Math.Round(fromList.Sale.TotalAmount + orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        } else if (reSaleItem != null) {
                            IncomePaymentOrderSale fromList = itemFromList.IncomePaymentOrder.IncomePaymentOrderSales.First(s => s.Id.Equals(incomePaymentOrderSale.Id));

                            if (fromList.ReSale.ReSaleItems.Any(i => i.Id.Equals(reSaleItem.Id))) return incomePaymentOrder;

                            consignmentItem.Product = product;
                            reSaleAvailability.ConsignmentItem = consignmentItem;
                            reSaleItem.ReSaleAvailability = reSaleAvailability;
                            fromList.ReSale.ReSaleItems.Add(reSaleItem);
                            fromList.ReSale.TotalAmount = Math.Round(reSale.TotalAmount + reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty), 2);
                        }
                    }
                } else {
                    if (!string.IsNullOrEmpty(incomePaymentOrder.Number))
                        itemFromList.Number = incomePaymentOrder.Number;

                    if (incomePaymentOrderSale != null) {
                        if (orderItem != null) {
                            orderItem.Product = product;

                            order.OrderItems.Add(orderItem);

                            sale.TotalAmount = Math.Round(sale.TotalAmount + orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                        } else if (reSaleItem != null) {
                            consignmentItem.Product = product;
                            reSaleAvailability.ConsignmentItem = consignmentItem;
                            reSaleItem.ReSaleAvailability = reSaleAvailability;
                            reSale.ReSaleItems.Add(reSaleItem);
                            reSale.TotalAmount = Math.Round(reSale.TotalAmount + reSaleItem.PricePerItem * Convert.ToDecimal(reSaleItem.Qty), 2);
                        }

                        if (saleClientAgreement != null) {
                            saleAgreement.Currency = saleAgreementCurrency;

                            saleClientAgreement.Agreement = saleAgreement;
                        }

                        if (sale != null) {
                            sale.Order = order;
                            sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            sale.SaleNumber = saleNumber;
                            sale.ClientAgreement = saleClientAgreement;
                        } else if (reSale != null) {
                            reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                            reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                            reSale.SaleNumber = saleNumber;
                            reSale.ClientAgreement = saleClientAgreement;
                        }

                        incomePaymentOrderSale.Sale = sale;
                        incomePaymentOrderSale.ReSale = reSale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);

                        incomePaymentOrder.EuroAmount =
                            decimal.Round(
                                (incomePaymentOrderSale.Amount + incomePaymentOrderSale.OverpaidAmount) * incomePaymentOrderSale.ExchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );
                    } else {
                        incomePaymentOrder.EuroAmount =
                            decimal.Round(
                                incomePaymentOrder.EuroAmount * incomePaymentOrder.AgreementEuroExchangeRate,
                                2,
                                MidpointRounding.AwayFromZero
                            );
                    }

                    if (clientAgreement != null) {
                        agreement.Currency = agreementCurrency;

                        clientAgreement.Agreement = agreement;
                    }

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);

                    incomePaymentOrder.Organization = organization;
                    incomePaymentOrder.User = user;
                    incomePaymentOrder.Currency = currency;
                    incomePaymentOrder.PaymentRegister = paymentRegister;
                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    incomePaymentOrder.Client = incomeClient;

                    itemFromList.IncomePaymentOrder = incomePaymentOrder;

                    itemFromList.Comment = incomePaymentOrder.Comment;
                }

                return incomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder)).Select(s => s.Id),
                    preDefinedClientAgreement.Id,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.Sale))) {
            string sqlExpression =
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
                //Client saleClient = (Client) objects[5];
                Agreement agreement = (Agreement)objects[6];
                Currency currency = (Currency)objects[7];
                SaleNumber saleNumber = (SaleNumber)objects[8];
                BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[9];
                BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[10];
                Organization organization = (Organization)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.Sale) && i.Id.Equals(sale.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.Sale != null && !itemFromList.Sale.IsNew()) {
                    if (orderItem == null || itemFromList.Sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) return sale;

                    orderItem.Product = product;

                    itemFromList.Sale.Order.OrderItems.Add(orderItem);
                } else {
                    if (orderItem != null) {
                        orderItem.Product = product;

                        order.OrderItems.Add(orderItem);
                    }

                    if (clientAgreement != null) {
                        agreement.Currency = currency;

                        clientAgreement.Client = preDefinedClientAgreement.Client;
                        clientAgreement.Agreement = agreement;
                    }

                    sale.Order = order;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    sale.ClientAgreement = clientAgreement;
                    sale.SaleNumber = saleNumber;

                    sale.TotalAmount = itemFromList.Sale?.TotalAmount ?? sale.TotalAmount;

                    itemFromList.Sale = sale;

                    itemFromList.Comment = sale.Comment;

                    itemFromList.Number = saleNumber.Value;
                }

                return sale;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.Sale)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SaleReturn))) {
            string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

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
                "WHERE [SaleReturn].ID IN @Ids " +
                "AND [Sale].ClientAgreementID = @Id";

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

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.SaleReturn) && i.Id.Equals(saleReturn.Id));

                decimal vatRatePercent = Convert.ToDecimal((vatRate?.Value ?? 0) / 100);

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.SaleReturn != null) {
                    if (itemFromList.SaleReturn.SaleReturnItems.Any(i => i.Id.Equals(saleReturnItem.Id))) return saleReturn;

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

                    itemFromList.SaleReturn.SaleReturnItems.Add(saleReturnItem);

                    itemFromList.SaleReturn.TotalAmount =
                        decimal.Round(
                            itemFromList.SaleReturn.SaleReturnItems.Sum(i => decimal.Round(i.Amount * i.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero))
                            , 2
                            , MidpointRounding.AwayFromZero
                        );
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

                        saleReturn.TotalAmount = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                        saleReturn.SaleReturnItems.Add(saleReturnItem);
                    }

                    returnClient.RegionCode = regionCode;

                    saleReturn.Client = returnClient;
                    saleReturn.CreatedBy = returnCreatedBy;

                    itemFromList.Number = saleReturn.Number;

                    itemFromList.SaleReturn = saleReturn;
                }

                return saleReturn;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SaleReturn)).Select(s => s.Id),
                    Culture = culture,
                    preDefinedClientAgreement.Id
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ReSale))) {
            string sqlExpression =
                "SELECT * FROM ReSale " +
                "LEFT JOIN ReSaleItem " +
                "ON ReSaleItem.ReSaleID = ReSale.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.ID = ReSale.ClientAgreementID " +
                "LEFT JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [ReSale].SaleNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN Client " +
                "ON Client.ID = ClientAgreement.ClientID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ReSale].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE ReSale.ID IN @Ids ";

            Type[] types = {
                typeof(ReSale),
                typeof(ReSaleItem),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(SaleNumber),
                typeof(Client),
                typeof(Currency),
                typeof(Organization)
            };

            Func<object[], ReSale> mapper = objects => {
                ReSale reSale = (ReSale)objects[0];
                ReSaleItem reSaleItem = (ReSaleItem)objects[1];
                ClientAgreement clientAgreement = (ClientAgreement)objects[2];
                Agreement agreement = (Agreement)objects[3];
                SaleNumber saleNumber = (SaleNumber)objects[4];
                Client returnClient = (Client)objects[5];
                Currency currency = (Currency)objects[6];
                Organization organization = (Organization)objects[7];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ReSale) && i.Id.Equals(reSale.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.UpdatedReSaleModel.ReSale != null && !itemFromList.UpdatedReSaleModel.ReSale.IsNew()) {
                    itemFromList.Number = saleNumber.Value;

                    if (reSaleItem == null || itemFromList.UpdatedReSaleModel.ReSale.ReSaleItems.Any(i => i.Id.Equals(reSaleItem.Id))) return reSale;

                    itemFromList.UpdatedReSaleModel.ReSale.ReSaleItems.Add(reSaleItem);
                } else {
                    itemFromList.Number = saleNumber.Value;

                    if (reSaleItem != null) reSale.ReSaleItems.Add(reSaleItem);

                    if (clientAgreement != null) {
                        agreement.Currency = currency;

                        clientAgreement.Client = returnClient;
                        clientAgreement.Agreement = agreement;
                    }

                    reSale.ClientAgreement = clientAgreement;
                    reSale.SaleNumber = saleNumber;
                    reSale.Organization = organization;

                    itemFromList.UpdatedReSaleModel.ReSale = reSale;

                    itemFromList.Comment = reSale.Comment;
                }

                return reSale;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ReSale)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
        }

        return accountingCashFlow;
    }
}