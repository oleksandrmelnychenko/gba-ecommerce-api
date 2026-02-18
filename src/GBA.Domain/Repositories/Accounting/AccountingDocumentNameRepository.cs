using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.AccountingDocumentNames;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Repositories.Accounting.Contracts;

namespace GBA.Domain.Repositories.Accounting;

public sealed class AccountingDocumentNameRepository : IAccountingDocumentNameRepository {
    private const string DEBTS_FROM_ONE_C_UK_KEY = "Ввід боргів з 1С";
    private readonly IDbConnection _connection;

    public AccountingDocumentNameRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<AccountingCashFlowHeadItem> GetDocumentNames(List<AccountingCashFlowHeadItem> documents, TypePaymentTask typePaymentTask) {
        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        string sqlQuery = "SELECT * FROM [AccountingDocumentName] " +
                          "WHERE [AccountingDocumentName].[Deleted] = 0;";

        List<AccountingDocumentName> accountingDocumentNames = _connection.Query<AccountingDocumentName>(sqlQuery).ToList();

        foreach (AccountingCashFlowHeadItem accountingCashFlowHeadItem in documents.Where(accountingCashFlowHeadItem =>
                     accountingDocumentNames.Any(x => x.DocumentType.Equals(accountingCashFlowHeadItem.Type)))) {
            string headDocumentName = string.Empty;

            if ((accountingCashFlowHeadItem.Type.Equals(JoinServiceType.MergedService) ||
                 accountingCashFlowHeadItem.Type.Equals(JoinServiceType.AccountingMergedService)) &&
                accountingCashFlowHeadItem.MergedService.DeliveryProductProtocolId.HasValue) {
                if (accountingCashFlowHeadItem.MergedService.ConsumableProduct != null &&
                    !string.IsNullOrEmpty(accountingCashFlowHeadItem.MergedService.ConsumableProduct.Name))
                    headDocumentName = culture == "pl"
                        ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? ""
                        : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? "";
                else
                    headDocumentName = accountingDocumentNames.FirstOrDefault(x => x.DocumentType.Equals(JoinServiceType.MergedService))?.NameUK ?? "";
            } else if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.DeliveryExpense) ||
                       accountingCashFlowHeadItem.Type.Equals(JoinServiceType.AccountingDeliveryExpense)) {
                headDocumentName = culture == "pl"
                    ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? ""
                    : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? "";
            } else {
                if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.BillOfLadingService) ||
                    accountingCashFlowHeadItem.Type.Equals(JoinServiceType.AccountingBillOfLadingService)) {
                    if (accountingCashFlowHeadItem.BillOfLadingService.TypeBillOfLadingService == TypeBillOfLadingService.Container)
                        headDocumentName = culture == "pl"
                            ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ContainerService)?.NameUK ?? ""
                            : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ContainerService)?.NameUK ?? "";
                    else
                        headDocumentName = culture == "pl"
                            ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.VehicleService)?.NameUK ?? ""
                            : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.VehicleService)?.NameUK ?? "";
                } else if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.IncomePaymentOrder)) {
                    if (accountingCashFlowHeadItem.Number == DEBTS_FROM_ONE_C_UK_KEY) {
                        headDocumentName = IncomePaymentOrderTypeConsts.FROM_ONE_C;
                    } else {
                        if (accountingCashFlowHeadItem.IncomePaymentOrder.PaymentRegister.Type == PaymentRegisterType.Bank)
                            headDocumentName = culture == "pl" ? IncomePaymentOrderTypeConsts.BANK_PL : IncomePaymentOrderTypeConsts.BANK_UK;
                        else if (accountingCashFlowHeadItem.IncomePaymentOrder.PaymentRegister.Type == PaymentRegisterType.Card)
                            headDocumentName = culture == "pl" ? IncomePaymentOrderTypeConsts.CARD_PL : IncomePaymentOrderTypeConsts.CARD_UK;
                        else if (accountingCashFlowHeadItem.IncomePaymentOrder.PaymentRegister.Type == PaymentRegisterType.Cash)
                            headDocumentName = culture == "pl" ? IncomePaymentOrderTypeConsts.CASH_PL : IncomePaymentOrderTypeConsts.CASH_UK;
                    }
                } else if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.OutcomePaymentOrder)) {
                    if (accountingCashFlowHeadItem.Number == DEBTS_FROM_ONE_C_UK_KEY) {
                        headDocumentName = OutcomePaymentOrderTypeConsts.FROM_ONE_C;
                    } else {
                        if (accountingCashFlowHeadItem.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type == PaymentRegisterType.Bank)
                            headDocumentName = culture == "pl" ? OutcomePaymentOrderTypeConsts.BANK_PL : OutcomePaymentOrderTypeConsts.BANK_UK;
                        else if (accountingCashFlowHeadItem.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type == PaymentRegisterType.Card)
                            headDocumentName = culture == "pl" ? OutcomePaymentOrderTypeConsts.CARD_PL : OutcomePaymentOrderTypeConsts.CARD_UK;
                        else if (accountingCashFlowHeadItem.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type == PaymentRegisterType.Cash)
                            headDocumentName = culture == "pl" ? OutcomePaymentOrderTypeConsts.CASH_PL : OutcomePaymentOrderTypeConsts.CASH_UK;
                    }
                } else if (accountingCashFlowHeadItem.Number == DEBTS_FROM_ONE_C_UK_KEY || accountingCashFlowHeadItem.Comment == DEBTS_FROM_ONE_C_UK_KEY) {
                    headDocumentName =
                        culture == "pl"
                            ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.DebtFromOneC)?.NameUK ?? ""
                            : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.DebtFromOneC)?.NameUK ?? "";
                } else {
                    headDocumentName = culture == "pl"
                        ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == accountingCashFlowHeadItem.Type)?.NameUK ?? ""
                        : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == accountingCashFlowHeadItem.Type)?.NameUK ?? "";
                }
            }

            if (accountingCashFlowHeadItem.IsAccounting)
                headDocumentName += " (Бух)";

            if (accountingCashFlowHeadItem.IsManagementAccounting)
                headDocumentName += " (Упр)";

            headDocumentName += $" ({accountingCashFlowHeadItem.Number}) ";

            DateTime fromDate = TimeZoneInfo.ConvertTimeFromUtc(
                accountingCashFlowHeadItem.FromDate,
                TimeZoneInfo.FindSystemTimeZoneById(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                        ? "FLE Standard Time"
                        : "Central European Standard Time"
                )
            );

            headDocumentName += culture == "pl"
                ? "Od"
                : "Від"
                  + $" {fromDate.ToShortDateString()} {fromDate.ToShortTimeString()}";

            accountingCashFlowHeadItem.Name = headDocumentName;
        }

        return documents;
    }

    public List<AccountingCashFlowHeadItem> GetDocumentNamesForClients(List<AccountingCashFlowHeadItem> documents) {
        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        string sqlQuery = "SELECT * FROM [AccountingDocumentName] " +
                          "WHERE [AccountingDocumentName].[Deleted] = 0;";

        List<AccountingDocumentName> accountingDocumentNames = _connection.Query<AccountingDocumentName>(sqlQuery).ToList();

        foreach (AccountingCashFlowHeadItem accountingCashFlowHeadItem in documents.Where(accountingCashFlowHeadItem =>
                     accountingDocumentNames.Any(x => x.DocumentType.Equals(accountingCashFlowHeadItem.Type)))) {
            string headDocumentName = string.Empty;

            if ((accountingCashFlowHeadItem.Type.Equals(JoinServiceType.MergedService) ||
                 accountingCashFlowHeadItem.Type.Equals(JoinServiceType.AccountingMergedService)) &&
                accountingCashFlowHeadItem.MergedService.DeliveryProductProtocolId.HasValue) {
                if (accountingCashFlowHeadItem.MergedService.ConsumableProduct != null &&
                    !string.IsNullOrEmpty(accountingCashFlowHeadItem.MergedService.ConsumableProduct.Name))
                    headDocumentName = culture == "pl"
                        ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? ""
                        : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? "";
                else
                    headDocumentName = accountingDocumentNames.FirstOrDefault(x => x.DocumentType.Equals(JoinServiceType.MergedService))?.NameUK ?? "";
            } else if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.DeliveryExpense) ||
                       accountingCashFlowHeadItem.Type.Equals(JoinServiceType.AccountingDeliveryExpense)) {
                headDocumentName = culture == "pl"
                    ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? ""
                    : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ActProvidingService)?.NameUK ?? "";
            } else {
                if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.BillOfLadingService) ||
                    accountingCashFlowHeadItem.Type.Equals(JoinServiceType.AccountingBillOfLadingService)) {
                    if (accountingCashFlowHeadItem.BillOfLadingService.TypeBillOfLadingService == TypeBillOfLadingService.Container)
                        headDocumentName = culture == "pl"
                            ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ContainerService)?.NameUK ?? ""
                            : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.ContainerService)?.NameUK ?? "";
                    else
                        headDocumentName = culture == "pl"
                            ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.VehicleService)?.NameUK ?? ""
                            : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.VehicleService)?.NameUK ?? "";
                } else if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.IncomePaymentOrder)) {
                    if (accountingCashFlowHeadItem.Number == DEBTS_FROM_ONE_C_UK_KEY) {
                        headDocumentName = IncomePaymentOrderTypeConsts.FROM_ONE_C;
                    } else {
                        if (accountingCashFlowHeadItem.IncomePaymentOrder.PaymentRegister.Type == PaymentRegisterType.Bank)
                            headDocumentName = culture == "pl" ? IncomePaymentOrderTypeConsts.BANK_PL : IncomePaymentOrderTypeConsts.BANK_UK;
                        else if (accountingCashFlowHeadItem.IncomePaymentOrder.PaymentRegister.Type == PaymentRegisterType.Card)
                            headDocumentName = culture == "pl" ? IncomePaymentOrderTypeConsts.CARD_PL : IncomePaymentOrderTypeConsts.CARD_UK;
                        else if (accountingCashFlowHeadItem.IncomePaymentOrder.PaymentRegister.Type == PaymentRegisterType.Cash)
                            headDocumentName = culture == "pl" ? IncomePaymentOrderTypeConsts.CASH_PL : IncomePaymentOrderTypeConsts.CASH_UK;
                    }
                } else if (accountingCashFlowHeadItem.Type.Equals(JoinServiceType.OutcomePaymentOrder)) {
                    if (accountingCashFlowHeadItem.Number == DEBTS_FROM_ONE_C_UK_KEY) {
                        headDocumentName = OutcomePaymentOrderTypeConsts.FROM_ONE_C;
                    } else {
                        if (accountingCashFlowHeadItem.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type == PaymentRegisterType.Bank)
                            headDocumentName = culture == "pl" ? OutcomePaymentOrderTypeConsts.BANK_PL : OutcomePaymentOrderTypeConsts.BANK_UK;
                        else if (accountingCashFlowHeadItem.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type == PaymentRegisterType.Card)
                            headDocumentName = culture == "pl" ? OutcomePaymentOrderTypeConsts.CARD_PL : OutcomePaymentOrderTypeConsts.CARD_UK;
                        else if (accountingCashFlowHeadItem.OutcomePaymentOrder.PaymentCurrencyRegister.PaymentRegister.Type == PaymentRegisterType.Cash)
                            headDocumentName = culture == "pl" ? OutcomePaymentOrderTypeConsts.CASH_PL : OutcomePaymentOrderTypeConsts.CASH_UK;
                    }
                } else if (accountingCashFlowHeadItem.Number == DEBTS_FROM_ONE_C_UK_KEY || accountingCashFlowHeadItem.Comment == DEBTS_FROM_ONE_C_UK_KEY) {
                    headDocumentName =
                        culture == "pl"
                            ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.DebtFromOneC)?.NameUK ?? ""
                            : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == JoinServiceType.DebtFromOneC)?.NameUK ?? "";
                } else {
                    headDocumentName = culture == "pl"
                        ? accountingDocumentNames.FirstOrDefault(x => x.DocumentType == accountingCashFlowHeadItem.Type)?.NameUK ?? ""
                        : accountingDocumentNames.FirstOrDefault(x => x.DocumentType == accountingCashFlowHeadItem.Type)?.NameUK ?? "";
                }
            }

            headDocumentName += $" ({accountingCashFlowHeadItem.Number}) ";

            DateTime fromDate = TimeZoneInfo.ConvertTimeFromUtc(
                accountingCashFlowHeadItem.FromDate,
                TimeZoneInfo.FindSystemTimeZoneById(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                        ? "FLE Standard Time"
                        : "Central European Standard Time"
                )
            );

            headDocumentName += culture == "pl"
                ? "Od"
                : "Від"
                  + $" {fromDate.ToShortDateString()} {fromDate.ToShortTimeString()}";

            accountingCashFlowHeadItem.Name = headDocumentName;
        }

        return documents;
    }
}