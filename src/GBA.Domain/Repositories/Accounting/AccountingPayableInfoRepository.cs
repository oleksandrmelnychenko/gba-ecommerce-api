using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.Repositories.Accounting.Contracts;

namespace GBA.Domain.Repositories.Accounting;

public sealed class AccountingPayableInfoRepository : IAccountingPayableInfoRepository {
    private readonly IDbConnection _connection;

    private readonly IDbConnection _currencyExchangeConnection;

    public AccountingPayableInfoRepository(IDbConnection connection, IDbConnection currencyExchangeConnection) {
        _connection = connection;

        _currencyExchangeConnection = currencyExchangeConnection;
    }

    public AccountingPayableInfo GetAllDebitInfo() {
        AccountingPayableInfo payableInfo = new();

        try {
            payableInfo.AccountingPayableInfoItems =
                _connection.Query<AccountingPayableInfoItem>(
                    ";WITH [DebitInfo_CTE] " +
                    "AS " +
                    "( " +
                    "SELECT [SupplyOrganizationAgreement].ID " +
                    ",0 AS [Type] " +
                    ",[SupplyOrganizationAgreement].CurrentAmount AS [Amount] " +
                    "FROM [SupplyOrganizationAgreement] " +
                    "WHERE [SupplyOrganizationAgreement].Deleted = 0 " +
                    "AND [SupplyOrganizationAgreement].CurrentAmount > 0 " +
                    "UNION " +
                    "SELECT [ClientAgreement].ID " +
                    ",1 AS [Type] " +
                    ",[ClientAgreement].CurrentAmount AS [Amount] " +
                    "FROM [ClientAgreement] " +
                    "WHERE [ClientAgreement].Deleted = 0 " +
                    "AND [ClientAgreement].CurrentAmount > 0 " +
                    "UNION " +
                    "SELECT [OutcomePaymentOrder].ID " +
                    ",2 AS [Type] " +
                    ",( " +
                    "0 " +
                    "- " +
                    "(SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    ", 2)) " +
                    ") AS [Amount] " +
                    "FROM [OutcomePaymentOrder] " +
                    "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                    "AND [OutcomePaymentOrder].IsUnderReport = 1 " +
                    "AND ( " +
                    "0 " +
                    "- " +
                    "( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    ") " +
                    ") <> [OutcomePaymentOrder].Amount " +
                    "AND ( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    ") < 0 " +
                    ") " +
                    "SELECT * " +
                    "FROM [DebitInfo_CTE] " +
                    "ORDER BY [DebitInfo_CTE].Amount DESC "
                ).ToList();

            if (payableInfo.AccountingPayableInfoItems.Any(i => i.Type.Equals(AccountingPayableInfoItemType.SupplyOrganizationAgreement)))
                _connection.Query<SupplyOrganizationAgreement, Currency, SupplyOrganization, SupplyOrganizationDocument, Organization, SupplyOrganizationAgreement>(
                    "SELECT * " +
                    "FROM [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "AND [Currency].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyOrganization] " +
                    "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
                    "LEFT JOIN [SupplyOrganizationDocument] " +
                    "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
                    "AND [SupplyOrganizationDocument].Deleted = 0 " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "WHERE [SupplyOrganizationAgreement].ID IN @Ids",
                    (agreement, currency, supplyOrganization, document, organization) => {
                        AccountingPayableInfoItem item =
                            payableInfo
                                .AccountingPayableInfoItems
                                .First(i => i.Type.Equals(AccountingPayableInfoItemType.SupplyOrganizationAgreement) && i.Id.Equals(agreement.Id));

                        if (item.SupplyOrganizationAgreement != null) {
                            if (document != null) item.SupplyOrganizationAgreement.SupplyOrganizationDocuments.Add(document);
                        } else {
                            if (document != null) agreement.SupplyOrganizationDocuments.Add(document);

                            agreement.Organization = organization;

                            agreement.Currency = currency;
                            agreement.SupplyOrganization = supplyOrganization;

                            item.SupplyOrganizationAgreement = agreement;

                            decimal exchangeRateAmount = 1;

                            if (!currency.Code.ToLower().Equals("eur")) exchangeRateAmount = GetExchangeRateToEuroCurrency(currency);

                            if (exchangeRateAmount < decimal.Zero) {
                                exchangeRateAmount = 0 - exchangeRateAmount;

                                item.EuroAmount = Math.Round(item.Amount / exchangeRateAmount, 2);
                            } else {
                                item.EuroAmount = Math.Round(item.Amount * exchangeRateAmount, 2);
                            }

                            payableInfo.TotalEuroAmount = Math.Round(payableInfo.TotalEuroAmount + item.EuroAmount, 2);

                            if (payableInfo.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                                PriceTotal totalFromList = payableInfo.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                                totalFromList.TotalPrice = Math.Round(totalFromList.TotalPrice + item.Amount, 2);
                            } else {
                                payableInfo.PriceTotals.Add(new PriceTotal {
                                    Currency = currency,
                                    TotalPrice = item.Amount
                                });
                            }
                        }

                        return agreement;
                    },
                    new {
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        Ids = payableInfo.AccountingPayableInfoItems.Where(i => i.Type.Equals(AccountingPayableInfoItemType.SupplyOrganizationAgreement)).Select(i => i.Id)
                    }
                );

            if (payableInfo.AccountingPayableInfoItems.Any(i => i.Type.Equals(AccountingPayableInfoItemType.ClientAgreement)))
                _connection.Query<ClientAgreement, Client, Agreement, Currency, ClientAgreement>(
                    "SELECT * " +
                    "FROM [ClientAgreement] " +
                    "LEFT JOIN [Client] " +
                    "ON [Client].ID = [ClientAgreement].ClientID " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                    "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                    "ON [Currency].ID = [Agreement].CurrencyID " +
                    "AND [Currency].CultureCode = @Culture " +
                    "WHERE [ClientAgreement].ID IN @Ids",
                    (clientAgreement, client, agreement, currency) => {
                        AccountingPayableInfoItem item =
                            payableInfo
                                .AccountingPayableInfoItems
                                .First(i => i.Type.Equals(AccountingPayableInfoItemType.ClientAgreement) && i.Id.Equals(clientAgreement.Id));

                        if (item.ClientAgreement == null) {
                            agreement.Currency = currency;

                            clientAgreement.Client = client;
                            clientAgreement.Agreement = agreement;

                            item.ClientAgreement = clientAgreement;

                            decimal exchangeRateAmount = 1;

                            if (!currency.Code.ToLower().Equals("eur")) exchangeRateAmount = GetExchangeRateToEuroCurrency(currency);

                            if (exchangeRateAmount < decimal.Zero) {
                                exchangeRateAmount = 0 - exchangeRateAmount;

                                item.EuroAmount = Math.Round(item.Amount / exchangeRateAmount, 2);
                            } else {
                                item.EuroAmount = Math.Round(item.Amount * exchangeRateAmount, 2);
                            }

                            payableInfo.TotalEuroAmount = Math.Round(payableInfo.TotalEuroAmount + item.EuroAmount, 2);

                            if (payableInfo.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                                PriceTotal totalFromList = payableInfo.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                                totalFromList.TotalPrice = Math.Round(totalFromList.TotalPrice + item.Amount, 2);
                            } else {
                                payableInfo.PriceTotals.Add(new PriceTotal {
                                    Currency = currency,
                                    TotalPrice = item.Amount
                                });
                            }
                        }

                        return clientAgreement;
                    },
                    new {
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        Ids = payableInfo.AccountingPayableInfoItems.Where(i => i.Type.Equals(AccountingPayableInfoItemType.ClientAgreement)).Select(i => i.Id)
                    }
                );

            if (payableInfo.AccountingPayableInfoItems.Any(i => i.Type.Equals(AccountingPayableInfoItemType.OutcomePaymentOrder))) {
                string sqlExpression =
                    "SELECT [OutcomePaymentOrder].* " +
                    ", ( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    "AND [AssignedIncome].IsCanceled = 0" +
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
                    ", [Organization].*" +
                    ", [User].*" +
                    ", [PaymentMovementOperation].*" +
                    ", [PaymentMovement].*" +
                    ", [PaymentCurrencyRegister].*" +
                    ", [Currency].*" +
                    ", [PaymentRegister].*" +
                    ", [PaymentRegisterOrganization].*" +
                    ", [OutcomePaymentOrderConsumablesOrder].*" +
                    ", [ConsumablesOrder].*" +
                    ", [ConsumablesOrderItem].*" +
                    ", [ConsumableProductCategory].*" +
                    ", [ConsumableProduct].*" +
                    ", [ConsumableProductOrganization].*" +
                    ", [ConsumablesOrganizationAgreement].*" +
                    ", [ConsumableProductOrganizationCurrency].*" +
                    ", [Colleague].*" +
                    ", [ConsumablesOrderUser].*" +
                    ", [ConsumablesStorage].*" +
                    ", [PaymentCostMovementOperation].*" +
                    ", [PaymentCostMovement].* " +
                    ", [MeasureUnit].*" +
                    ", [OutcomeConsumableProductOrganization].* " +
                    "FROM [OutcomePaymentOrder] " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [User] " +
                    "ON [User].ID = [OutcomePaymentOrder].UserID " +
                    "LEFT JOIN [PaymentMovementOperation] " +
                    "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                    "LEFT JOIN (" +
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
                    "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                    "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                    "LEFT JOIN [ConsumablesOrder] " +
                    "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                    "LEFT JOIN [ConsumablesOrderItem] " +
                    "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                    "AND [ConsumablesOrderItem].Deleted = 0 " +
                    "LEFT JOIN (" +
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
                    "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
                    ") AS [ConsumableProductCategory] " +
                    "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                    "LEFT JOIN (" +
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
                    "AND [ConsumableProductTranslation].CultureCode = @Culture" +
                    ") AS [ConsumableProduct] " +
                    "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                    "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                    "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                    "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesOrganizationAgreement] " +
                    "ON [ConsumablesOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ConsumableProductOrganizationCurrency] " +
                    "ON [ConsumableProductOrganizationCurrency].ID = [ConsumablesOrganizationAgreement].CurrencyID " +
                    "AND [ConsumableProductOrganizationCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [User] AS [Colleague] " +
                    "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                    "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                    "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                    "LEFT JOIN [ConsumablesStorage] " +
                    "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                    "LEFT JOIN [PaymentCostMovementOperation] " +
                    "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                    "LEFT JOIN (" +
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
                    "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                    "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                    "WHERE [OutcomePaymentOrder].ID IN @Ids";

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
                    typeof(OutcomePaymentOrderConsumablesOrder),
                    typeof(ConsumablesOrder),
                    typeof(ConsumablesOrderItem),
                    typeof(ConsumableProductCategory),
                    typeof(ConsumableProduct),
                    typeof(SupplyOrganization),
                    typeof(SupplyOrganizationAgreement),
                    typeof(Currency),
                    typeof(User),
                    typeof(User),
                    typeof(ConsumablesStorage),
                    typeof(PaymentCostMovementOperation),
                    typeof(PaymentCostMovement),
                    typeof(MeasureUnit),
                    typeof(SupplyOrganization)
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
                    OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[9];
                    ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[10];
                    ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[11];
                    ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[12];
                    ConsumableProduct consumableProduct = (ConsumableProduct)objects[13];
                    SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[14];
                    SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
                    Currency supplyOrganizationAgreementCurrency = (Currency)objects[16];
                    User colleague = (User)objects[17];
                    User consumablesOrderUser = (User)objects[18];
                    ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[19];
                    PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[20];
                    PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[21];
                    MeasureUnit measureUnit = (MeasureUnit)objects[22];
                    SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[23];

                    AccountingPayableInfoItem item =
                        payableInfo
                            .AccountingPayableInfoItems
                            .First(i => i.Type.Equals(AccountingPayableInfoItemType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                    if (item.OutcomePaymentOrder == null) {
                        if (consumablesOrder != null && consumablesOrderItem != null) {
                            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                            consumablesOrder.User = consumablesOrderUser;
                            consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrder.ConsumablesStorage = consumablesStorage;

                            outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                            outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                        }

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;

                        item.OutcomePaymentOrder = outcomePaymentOrder;
                    } else {
                        if (outcomePaymentOrderConsumablesOrder == null) return outcomePaymentOrder;

                        if (item.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                            if (consumablesOrder == null || consumablesOrderItem == null)
                                return outcomePaymentOrder;

                            OutcomePaymentOrderConsumablesOrder orderFromList =
                                item.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        } else {
                            if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                            consumablesOrder.User = consumablesOrderUser;
                            consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrder.ConsumablesStorage = consumablesStorage;

                            outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                            item.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                        }
                    }

                    return outcomePaymentOrder;
                };

                _connection.Query(
                    sqlExpression,
                    types,
                    mapper,
                    new {
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        Ids = payableInfo.AccountingPayableInfoItems.Where(i => i.Type.Equals(AccountingPayableInfoItemType.OutcomePaymentOrder)).Select(i => i.Id)
                    }
                );
            }
        } catch (Exception exc) {
            Console.WriteLine(exc);
        }

        return payableInfo;
    }

    public AccountingPayableInfo GetAllCreditInfo() {
        AccountingPayableInfo payableInfo = new();

        try {
            payableInfo.AccountingPayableInfoItems =
                _connection.Query<AccountingPayableInfoItem>(
                    ";WITH [CreditInfo_CTE] " +
                    "AS " +
                    "( " +
                    "SELECT [SupplyOrganizationAgreement].ID " +
                    ",0 AS [Type] " +
                    ",(0 - [SupplyOrganizationAgreement].CurrentAmount) AS [Amount] " +
                    "FROM [SupplyOrganizationAgreement] " +
                    "WHERE [SupplyOrganizationAgreement].Deleted = 0 " +
                    "AND [SupplyOrganizationAgreement].CurrentAmount < 0 " +
                    "UNION " +
                    "SELECT [ClientAgreement].ID " +
                    ",1 AS [Type] " +
                    ",( " +
                    "SELECT ISNULL(SUM([Debt].Total), 0) " +
                    "FROM [ClientInDebt] " +
                    "LEFT JOIN [Debt] " +
                    "ON [Debt].ID = [ClientInDebt].DebtID " +
                    "WHERE [ClientInDebt].Deleted = 0 " +
                    "AND [Debt].Deleted = 0 " +
                    "AND [Debt].Total > 0 " +
                    "AND [ClientInDebt].AgreementID = [ClientAgreement].AgreementID " +
                    ") AS [Amount] " +
                    "FROM [ClientAgreement] " +
                    "WHERE [ClientAgreement].Deleted = 0 " +
                    "AND [ClientAgreement].CurrentAmount < 0 " +
                    "UNION " +
                    "SELECT [OutcomePaymentOrder].ID " +
                    ",2 AS [Type] " +
                    ",( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    ") AS [Amount] " +
                    "FROM [OutcomePaymentOrder] " +
                    "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                    "AND [OutcomePaymentOrder].IsUnderReport = 1 " +
                    "AND ( " +
                    "0 " +
                    "- " +
                    "( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    ") " +
                    ") <> [OutcomePaymentOrder].Amount " +
                    "AND ( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    ") > 0 " +
                    ") " +
                    "SELECT * " +
                    "FROM [CreditInfo_CTE] " +
                    "ORDER BY [CreditInfo_CTE].Amount DESC "
                ).ToList();

            if (payableInfo.AccountingPayableInfoItems.Any(i => i.Type.Equals(AccountingPayableInfoItemType.SupplyOrganizationAgreement)))
                _connection.Query<SupplyOrganizationAgreement, Currency, SupplyOrganization, SupplyOrganizationDocument, Organization, SupplyOrganizationAgreement>(
                    "SELECT * " +
                    "FROM [SupplyOrganizationAgreement] " +
                    "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "AND [Currency].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyOrganization] " +
                    "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
                    "LEFT JOIN [SupplyOrganizationDocument] " +
                    "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
                    "AND [SupplyOrganizationDocument].Deleted = 0 " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "WHERE [SupplyOrganizationAgreement].ID IN @Ids",
                    (agreement, currency, supplyOrganization, document, organization) => {
                        AccountingPayableInfoItem item =
                            payableInfo
                                .AccountingPayableInfoItems
                                .First(i => i.Type.Equals(AccountingPayableInfoItemType.SupplyOrganizationAgreement) && i.Id.Equals(agreement.Id));

                        if (item.SupplyOrganizationAgreement != null) {
                            if (document != null) item.SupplyOrganizationAgreement.SupplyOrganizationDocuments.Add(document);
                        } else {
                            if (document != null) agreement.SupplyOrganizationDocuments.Add(document);

                            agreement.Organization = organization;

                            agreement.Currency = currency;
                            agreement.SupplyOrganization = supplyOrganization;

                            item.SupplyOrganizationAgreement = agreement;

                            decimal exchangeRateAmount = 1;

                            if (!currency.Code.ToLower().Equals("eur")) exchangeRateAmount = GetExchangeRateToEuroCurrency(currency);

                            if (exchangeRateAmount < decimal.Zero) {
                                exchangeRateAmount = 0 - exchangeRateAmount;

                                item.EuroAmount = Math.Round(item.Amount / exchangeRateAmount, 2);
                            } else {
                                item.EuroAmount = Math.Round(item.Amount * exchangeRateAmount, 2);
                            }

                            payableInfo.TotalEuroAmount = Math.Round(payableInfo.TotalEuroAmount + item.EuroAmount, 2);

                            if (payableInfo.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                                PriceTotal totalFromList = payableInfo.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                                totalFromList.TotalPrice = Math.Round(totalFromList.TotalPrice + item.Amount, 2);
                            } else {
                                payableInfo.PriceTotals.Add(new PriceTotal {
                                    Currency = currency,
                                    TotalPrice = item.Amount
                                });
                            }
                        }

                        return agreement;
                    },
                    new {
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        Ids = payableInfo.AccountingPayableInfoItems.Where(i => i.Type.Equals(AccountingPayableInfoItemType.SupplyOrganizationAgreement)).Select(i => i.Id)
                    }
                );

            if (payableInfo.AccountingPayableInfoItems.Any(i => i.Type.Equals(AccountingPayableInfoItemType.ClientAgreement)))
                _connection.Query<ClientAgreement, Client, Agreement, Currency, ClientAgreement>(
                    "SELECT * " +
                    "FROM [ClientAgreement] " +
                    "LEFT JOIN [Client] " +
                    "ON [Client].ID = [ClientAgreement].ClientID " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                    "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                    "ON [Currency].ID = [Agreement].CurrencyID " +
                    "AND [Currency].CultureCode = @Culture " +
                    "WHERE [ClientAgreement].ID IN @Ids",
                    (clientAgreement, client, agreement, currency) => {
                        AccountingPayableInfoItem item =
                            payableInfo
                                .AccountingPayableInfoItems
                                .First(i => i.Type.Equals(AccountingPayableInfoItemType.ClientAgreement) && i.Id.Equals(clientAgreement.Id));

                        if (item.ClientAgreement == null) {
                            agreement.Currency = currency;

                            clientAgreement.Client = client;
                            clientAgreement.Agreement = agreement;

                            item.ClientAgreement = clientAgreement;

                            decimal exchangeRateAmount = 1;

                            if (!currency.Code.ToLower().Equals("eur")) exchangeRateAmount = GetExchangeRateToEuroCurrency(currency);

                            if (exchangeRateAmount < decimal.Zero) {
                                exchangeRateAmount = 0 - exchangeRateAmount;

                                item.EuroAmount = Math.Round(item.Amount / exchangeRateAmount, 2);
                            } else {
                                item.EuroAmount = Math.Round(item.Amount * exchangeRateAmount, 2);
                            }

                            payableInfo.TotalEuroAmount = Math.Round(payableInfo.TotalEuroAmount + item.EuroAmount, 2);

                            if (payableInfo.PriceTotals.Any(t => t.Currency.Id.Equals(currency.Id))) {
                                PriceTotal totalFromList = payableInfo.PriceTotals.First(t => t.Currency.Id.Equals(currency.Id));

                                totalFromList.TotalPrice = Math.Round(totalFromList.TotalPrice + item.Amount, 2);
                            } else {
                                payableInfo.PriceTotals.Add(new PriceTotal {
                                    Currency = currency,
                                    TotalPrice = item.Amount
                                });
                            }
                        }

                        return clientAgreement;
                    },
                    new {
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        Ids = payableInfo.AccountingPayableInfoItems.Where(i => i.Type.Equals(AccountingPayableInfoItemType.ClientAgreement)).Select(i => i.Id)
                    }
                );

            if (payableInfo.AccountingPayableInfoItems.Any(i => i.Type.Equals(AccountingPayableInfoItemType.OutcomePaymentOrder))) {
                string sqlExpression =
                    "SELECT [OutcomePaymentOrder].* " +
                    ", ( " +
                    "SELECT ROUND( " +
                    "( " +
                    "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                    "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                    "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPrice + [ConsumablesOrderItem].VAT), 0)) " +
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
                    "AND [AssignedIncome].IsCanceled = 0" +
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
                    ", [Organization].*" +
                    ", [User].*" +
                    ", [PaymentMovementOperation].*" +
                    ", [PaymentMovement].*" +
                    ", [PaymentCurrencyRegister].*" +
                    ", [Currency].*" +
                    ", [PaymentRegister].*" +
                    ", [PaymentRegisterOrganization].*" +
                    ", [OutcomePaymentOrderConsumablesOrder].*" +
                    ", [ConsumablesOrder].*" +
                    ", [ConsumablesOrderItem].*" +
                    ", [ConsumableProductCategory].*" +
                    ", [ConsumableProduct].*" +
                    ", [ConsumableProductOrganization].*" +
                    ", [ConsumablesOrganizationAgreement].*" +
                    ", [ConsumableProductOrganizationCurrency].*" +
                    ", [Colleague].*" +
                    ", [ConsumablesOrderUser].*" +
                    ", [ConsumablesStorage].*" +
                    ", [PaymentCostMovementOperation].*" +
                    ", [PaymentCostMovement].* " +
                    ", [MeasureUnit].*" +
                    ", [OutcomeConsumableProductOrganization].* " +
                    "FROM [OutcomePaymentOrder] " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [User] " +
                    "ON [User].ID = [OutcomePaymentOrder].UserID " +
                    "LEFT JOIN [PaymentMovementOperation] " +
                    "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                    "LEFT JOIN (" +
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
                    "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                    "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                    "LEFT JOIN [ConsumablesOrder] " +
                    "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                    "LEFT JOIN [ConsumablesOrderItem] " +
                    "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                    "AND [ConsumablesOrderItem].Deleted = 0 " +
                    "LEFT JOIN (" +
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
                    "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
                    ") AS [ConsumableProductCategory] " +
                    "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                    "LEFT JOIN (" +
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
                    "AND [ConsumableProductTranslation].CultureCode = @Culture" +
                    ") AS [ConsumableProduct] " +
                    "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                    "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                    "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                    "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesOrganizationAgreement] " +
                    "ON [ConsumablesOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [views].[CurrencyView] AS [ConsumableProductOrganizationCurrency] " +
                    "ON [ConsumableProductOrganizationCurrency].ID = [ConsumablesOrganizationAgreement].CurrencyID " +
                    "AND [ConsumableProductOrganizationCurrency].CultureCode = @Culture " +
                    "LEFT JOIN [User] AS [Colleague] " +
                    "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                    "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                    "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                    "LEFT JOIN [ConsumablesStorage] " +
                    "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                    "LEFT JOIN [PaymentCostMovementOperation] " +
                    "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                    "LEFT JOIN (" +
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
                    "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                    "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                    "WHERE [OutcomePaymentOrder].ID IN @Ids";

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
                    typeof(OutcomePaymentOrderConsumablesOrder),
                    typeof(ConsumablesOrder),
                    typeof(ConsumablesOrderItem),
                    typeof(ConsumableProductCategory),
                    typeof(ConsumableProduct),
                    typeof(SupplyOrganization),
                    typeof(SupplyOrganizationAgreement),
                    typeof(Currency),
                    typeof(User),
                    typeof(User),
                    typeof(ConsumablesStorage),
                    typeof(PaymentCostMovementOperation),
                    typeof(PaymentCostMovement),
                    typeof(MeasureUnit),
                    typeof(SupplyOrganization)
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
                    OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[9];
                    ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[10];
                    ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[11];
                    ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[12];
                    ConsumableProduct consumableProduct = (ConsumableProduct)objects[13];
                    SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[14];
                    SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
                    Currency supplyOrganizationAgreementCurrency = (Currency)objects[16];
                    User colleague = (User)objects[17];
                    User consumablesOrderUser = (User)objects[18];
                    ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[19];
                    PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[20];
                    PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[21];
                    MeasureUnit measureUnit = (MeasureUnit)objects[22];
                    SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[23];

                    AccountingPayableInfoItem item =
                        payableInfo
                            .AccountingPayableInfoItems
                            .First(i => i.Type.Equals(AccountingPayableInfoItemType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                    if (item.OutcomePaymentOrder == null) {
                        if (consumablesOrder != null && consumablesOrderItem != null) {
                            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                            consumablesOrder.User = consumablesOrderUser;
                            consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrder.ConsumablesStorage = consumablesStorage;

                            outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                            outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                        }

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;

                        item.OutcomePaymentOrder = outcomePaymentOrder;
                    } else {
                        if (outcomePaymentOrderConsumablesOrder == null) return outcomePaymentOrder;

                        if (item.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                            if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                            OutcomePaymentOrderConsumablesOrder orderFromList =
                                item.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        } else {
                            if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                            consumablesOrder.User = consumablesOrderUser;
                            consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrder.ConsumablesStorage = consumablesStorage;

                            outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                            item.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                        }
                    }

                    return outcomePaymentOrder;
                };

                _connection.Query(
                    sqlExpression,
                    types,
                    mapper,
                    new {
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        Ids = payableInfo.AccountingPayableInfoItems.Where(i => i.Type.Equals(AccountingPayableInfoItemType.OutcomePaymentOrder)).Select(i => i.Id)
                    }
                );
            }
        } catch (Exception exc) {
            Console.WriteLine(exc);
        }

        return payableInfo;
    }

    private decimal GetExchangeRateToEuroCurrency(Currency fromCurrency, bool fromPln = false) {
        decimal exchangeRateAmount;

        if (fromPln)
            fromCurrency = _currencyExchangeConnection.Query<Currency>(
                    "SELECT TOP(1) * " +
                    "FROM [Currency] " +
                    "WHERE [Currency].Deleted = 0 " +
                    "AND [Currency].Code = 'PLN'"
                )
                .Single();

        if (fromCurrency.Code.ToLower().Equals("uah") || fromCurrency.Code.ToLower().Equals("pln"))
            exchangeRateAmount = _currencyExchangeConnection.Query<decimal>(
                "DECLARE @ExchangeRate money; " +
                "SELECT @ExchangeRate = " +
                "( " +
                "SELECT (0 - [ExchangeRate].Amount) " +
                "FROM [ExchangeRate] " +
                "WHERE [ExchangeRate].CurrencyID = @FromCurrencyId " +
                "AND [ExchangeRate].Code = 'EUR' " +
                "AND [ExchangeRate].Deleted = 0 " +
                "); " +
                "SELECT " +
                "CASE " +
                "WHEN @ExchangeRate IS NOT NULL " +
                "THEN @ExchangeRate " +
                "ELSE 1 " +
                "END",
                new { FromCurrencyId = fromCurrency.Id }
            ).Single();
        else
            exchangeRateAmount = _currencyExchangeConnection.Query<decimal>(
                "DECLARE @EuroCurrencyId bigint; " +
                "DECLARE @CrossExchangeRate money; " +
                "DECLARE @InverseCrossExchangeRate money; " +
                "SELECT @EuroCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'EUR'); " +
                "SELECT @CrossExchangeRate = " +
                "( " +
                "SELECT [CrossExchangeRate].Amount " +
                "FROM [CrossExchangeRate] " +
                "WHERE [CrossExchangeRate].CurrencyFromID = @FromCurrencyId " +
                "AND [CrossExchangeRate].CurrencyToID = @EuroCurrencyId " +
                "AND [CrossExchangeRate].Deleted = 0 " +
                "); " +
                "SELECT @InverseCrossExchangeRate = " +
                "( " +
                "SELECT (0 - [CrossExchangeRate].Amount) " +
                "FROM [CrossExchangeRate] " +
                "WHERE [CrossExchangeRate].CurrencyFromID = @EuroCurrencyId " +
                "AND [CrossExchangeRate].CurrencyToID = @FromCurrencyId " +
                "AND [CrossExchangeRate].Deleted = 0 " +
                "); " +
                "SELECT " +
                "CASE " +
                "WHEN @CrossExchangeRate IS NOT NULL " +
                "THEN @CrossExchangeRate " +
                "WHEN @InverseCrossExchangeRate IS NOT NULL " +
                "THEN @InverseCrossExchangeRate " +
                "ELSE 1 " +
                "END",
                new { FromCurrencyId = fromCurrency.Id }
            ).Single();

        return exchangeRateAmount;
    }
}