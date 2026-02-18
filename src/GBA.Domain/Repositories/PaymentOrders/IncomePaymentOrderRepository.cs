using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Extensions;
using GBA.Domain.Entities;
using GBA.Domain.Entities.AccountingDocumentNames;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.SalesModels.ChartOfSalesModels;
using GBA.Domain.EntityHelpers.TotalDashboards;
using GBA.Domain.EntityHelpers.TotalDashboards.Charts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class IncomePaymentOrderRepository : IIncomePaymentOrderRepository {
    private readonly IDbConnection _connection;

    private readonly string FROM_ONE_C = "Ввід боргів з 1С";

    public IncomePaymentOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(IncomePaymentOrder incomePaymentOrder) {
        return _connection.Query<long>(
                "INSERT INTO [IncomePaymentOrder] (Number, BankAccount, Comment, FromDate, IncomePaymentOrderType, VatPercent, VAT, Amount, ExchangeRate, IsManagementAccounting, " +
                "IsAccounting, Account, ClientId, OrganizationId, CurrencyId, PaymentRegisterId, UserId, ColleagueId, ClientAgreementId, EuroAmount, " +
                "AgreementEuroExchangeRate, OrganizationClientId, OrganizationClientAgreementId, TaxFreeId, SadId, ArrivalNumber, PaymentPurpose, " +
                "OperationType, SupplyOrganizationID, SupplyOrganizationAgreementID, Updated, AgreementExchangedAmount) " +
                "VALUES (@Number, @BankAccount, @Comment, @FromDate, @IncomePaymentOrderType, @VatPercent, @VAT, @Amount, @ExchangeRate, @IsManagementAccounting, " +
                "@IsAccounting, @Account, @ClientId, @OrganizationId, @CurrencyId, @PaymentRegisterId, @UserId, @ColleagueId, @ClientAgreementId, @EuroAmount, " +
                "@AgreementEuroExchangeRate, @OrganizationClientId, @OrganizationClientAgreementId, @TaxFreeId, @SadId, @ArrivalNumber, @PaymentPurpose, " +
                "@OperationType, @SupplyOrganizationID, @SupplyOrganizationAgreementID, getutcdate(), @AgreementExchangedAmount); " +
                "SELECT SCOPE_IDENTITY()",
                incomePaymentOrder
            )
            .Single();
    }

    public void Update(IncomePaymentOrder incomePaymentOrder) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrder] " +
            "SET " +
            "BankAccount = @BankAccount, Comment = @Comment, FromDate = @FromDate, IncomePaymentOrderType = @IncomePaymentOrderType, UserId = @UserId, ColleagueId = @ColleagueId, " +
            "IsManagementAccounting = @IsManagementAccounting, IsAccounting = @IsAccounting, Account = @Account, OrganizationId = @OrganizationId, ClientId = @ClientId, " +
            "IsCanceled = @IsCanceled, ClientAgreementId = @ClientAgreementId, ArrivalNumber = @ArrivalNumber, PaymentPurpose = @PaymentPurpose, Updated = getutcdate() " +
            "WHERE [IncomePaymentOrder].ID = @Id",
            incomePaymentOrder
        );
    }

    public void UpdateAgreementId(IncomePaymentOrder incomePaymentOrder) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrder] " +
            "SET ClientAgreementID = @ClientAgreementId, Updated = getutcdate() " +
            "WHERE [IncomePaymentOrder].ID = @Id",
            incomePaymentOrder
        );
    }

    public void UpdateExchangeRateById(long id, decimal exchangeRate) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrder] " +
            "SET ExchangeRate = @ExchangeRate, Updated = GETUTCDATE() " +
            "WHERE [IncomePaymentOrder].ID = @Id",
            new { Id = id, ExchangeRate = exchangeRate }
        );
    }

    public void UpdateOverpaidAmountById(long id, decimal overpaidAmount) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrder] " +
            "SET OverpaidAmount = @OverpaidAmount, Updated = GETUTCDATE() " +
            "WHERE [IncomePaymentOrder].ID = @Id",
            new { Id = id, OverpaidAmount = overpaidAmount }
        );
    }

    public IncomePaymentOrder GetByIdWithCalculatedAmount(long id) {
        return _connection.Query<IncomePaymentOrder, AssignedPaymentOrder, IncomePaymentOrder>(
                "SELECT [IncomePaymentOrder].[ID] " +
                ",[IncomePaymentOrder].[Account] " +
                ",ROUND([IncomePaymentOrder].Amount " +
                "- " +
                "ISNULL(( " +
                "SELECT SUM([AssignedOutcome].Amount) " +
                "FROM [AssignedPaymentOrder] " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [AssignedPaymentOrder].Deleted = 0 " +
                "), 0) " +
                "+ " +
                "ISNULL(( " +
                "SELECT SUM([AssignedIncome].Amount) " +
                "FROM [AssignedPaymentOrder] " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [AssignedPaymentOrder].Deleted = 0 " +
                "), 0) " +
                ", 2) AS [Amount] " +
                ",[IncomePaymentOrder].[BankAccount] " +
                ",[IncomePaymentOrder].[ClientID] " +
                ",[IncomePaymentOrder].[Comment] " +
                ",[IncomePaymentOrder].[Created] " +
                ",[IncomePaymentOrder].[CurrencyID] " +
                ",[IncomePaymentOrder].[Deleted] " +
                ",[IncomePaymentOrder].[ExchangeRate] " +
                ",[IncomePaymentOrder].[FromDate] " +
                ",[IncomePaymentOrder].[IsAccounting] " +
                ",[IncomePaymentOrder].[IsManagementAccounting] " +
                ",[IncomePaymentOrder].[NetUID] " +
                ",[IncomePaymentOrder].[Number] " +
                ",[IncomePaymentOrder].[OrganizationID] " +
                ",[IncomePaymentOrder].[PaymentRegisterID] " +
                ",[IncomePaymentOrder].[Updated] " +
                ",[IncomePaymentOrder].[VAT] " +
                ",[IncomePaymentOrder].[VatPercent] " +
                ",[IncomePaymentOrderType] " +
                ",[IncomePaymentOrder].[UserID] " +
                ",[IncomePaymentOrder].[ColleagueID] " +
                ",[AssignedPaymentOrder].* " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [AssignedPaymentOrder] " +
                "ON [AssignedPaymentOrder].AssignedIncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "WHERE [IncomePaymentOrder].ID = @Id",
                (income, assignedOrder) => {
                    income.RootAssignedPaymentOrder = assignedOrder;

                    return income;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public IncomePaymentOrder GetById(long id) {
        IncomePaymentOrder toReturn = null;

        Type[] types = {
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(ReSale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(SupplyOrganization),
            typeof(AccountingOperationName)
        };

        Func<object[], IncomePaymentOrder> mapper = objects => {
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
            Client client = (Client)objects[1];
            Organization organization = (Organization)objects[2];
            Currency currency = (Currency)objects[3];
            PaymentRegister paymentRegister = (PaymentRegister)objects[4];
            Organization paymentRegisterOrganization = (Organization)objects[5];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[6];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[7];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[8];
            Sale sale = (Sale)objects[9];
            ReSale reSale = (ReSale)objects[10];
            SaleNumber saleNumber = (SaleNumber)objects[11];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[12];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[13];
            User user = (User)objects[14];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[15];
            PaymentMovement paymentMovement = (PaymentMovement)objects[16];
            User colleague = (User)objects[17];
            OrganizationClient organizationClient = (OrganizationClient)objects[18];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[19];
            Currency organizationClientAgreementCurrency = (Currency)objects[20];
            TaxFree taxFree = (TaxFree)objects[21];
            Sad sad = (Sad)objects[22];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[23];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[24];

            if (toReturn == null) {
                if (paymentCurrencyRegister != null) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale != null) {
                    if (sale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    } else if (reSale != null) {
                        reSale.SaleNumber = saleNumber;
                        reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    }

                    incomePaymentOrderSale.Sale = sale;
                    incomePaymentOrderSale.ReSale = reSale;

                    incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                }

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                incomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                incomePaymentOrder.OrganizationClient = organizationClient;
                incomePaymentOrder.TaxFree = taxFree;
                incomePaymentOrder.Sad = sad;
                incomePaymentOrder.Client = client;
                incomePaymentOrder.Organization = organization;
                incomePaymentOrder.Currency = currency;
                incomePaymentOrder.PaymentRegister = paymentRegister;
                incomePaymentOrder.User = user;
                incomePaymentOrder.Colleague = colleague;
                incomePaymentOrder.SupplyOrganization = supplyOrganization;
                incomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;

                toReturn = incomePaymentOrder;
            } else {
                if (paymentCurrencyRegister != null && !toReturn.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.Id.Equals(paymentCurrencyRegister.Id))) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    toReturn.PaymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale == null || toReturn.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) return incomePaymentOrder;

                if (sale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                } else if (reSale != null) {
                    reSale.SaleNumber = saleNumber;
                    reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                }

                incomePaymentOrderSale.Sale = sale;
                incomePaymentOrderSale.ReSale = reSale;

                toReturn.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return incomePaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[SaleNumberID] ELSE [ReSale].[SaleNumberID] END = [SaleNumber].[ID] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[BaseLifeCycleStatusID] ELSE [ReSale].[BaseLifeCycleStatusID] END = [BaseLifeCycleStatus].[ID] " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[BaseSalePaymentStatusID] ELSE [ReSale].[BaseSalePaymentStatusID] END = [BaseSalePaymentStatus].[ID] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [IncomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [IncomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [IncomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [IncomePaymentOrder].SadID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "LEFT JOIN [AccountingOperationName] " +
            "ON [AccountingOperationName].OperationType = [IncomePaymentOrder].OperationType " +
            "WHERE [IncomePaymentOrder].ID = @Id",
            types,
            mapper,
            new {
                Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        if (toReturn == null) return toReturn;

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.AssignedPaymentOrders.Add(assigned);

                return assigned;
            },
            new {
                Id = id
            }
        );

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].AssignedIncomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.RootAssignedPaymentOrder = assigned;

                return assigned;
            },
            new {
                Id = id
            }
        );
        return toReturn;
    }

    public IncomePaymentOrder GetLastIncomeForSalesByClientId(long clientId) {
        return _connection.Query<IncomePaymentOrder>(
                "SELECT TOP(1) [IncomePaymentOrder].* " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [IncomePaymentOrderSale] " +
                "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "WHERE [IncomePaymentOrder].ClientID = @ClientId " +
                "AND [IncomePaymentOrder].IsCanceled = 0 " +
                "AND [IncomePaymentOrderSale].ID IS NOT NULL " +
                "ORDER BY [IncomePaymentOrder].ID DESC",
                new {
                    ClientId = clientId
                }
            )
            .SingleOrDefault();
    }

    public IncomePaymentOrder GetByNetId(Guid netId) {
        IncomePaymentOrder toReturn = null;

        Type[] types = {
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(ClientAgreement),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(AccountingOperationName)
        };

        Func<object[], IncomePaymentOrder> mapper = objects => {
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
            Client client = (Client)objects[1];
            Organization organization = (Organization)objects[2];
            Currency currency = (Currency)objects[3];
            PaymentRegister paymentRegister = (PaymentRegister)objects[4];
            Organization paymentRegisterOrganization = (Organization)objects[5];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[6];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[7];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[8];
            Sale sale = (Sale)objects[9];
            SaleNumber saleNumber = (SaleNumber)objects[10];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[11];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[12];
            User user = (User)objects[13];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[14];
            PaymentMovement paymentMovement = (PaymentMovement)objects[15];
            User colleague = (User)objects[16];
            ClientAgreement clientAgreement = (ClientAgreement)objects[17];
            OrganizationClient organizationClient = (OrganizationClient)objects[18];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[19];
            Currency organizationClientAgreementCurrency = (Currency)objects[20];
            TaxFree taxFree = (TaxFree)objects[21];
            Sad sad = (Sad)objects[22];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[23];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[24];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[25];

            if (toReturn == null) {
                if (paymentCurrencyRegister != null) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                    incomePaymentOrderSale.Sale = sale;

                    incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                }

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                incomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                incomePaymentOrder.OrganizationClient = organizationClient;
                incomePaymentOrder.TaxFree = taxFree;
                incomePaymentOrder.Sad = sad;
                incomePaymentOrder.Client = client;
                incomePaymentOrder.Organization = organization;
                incomePaymentOrder.Currency = currency;
                incomePaymentOrder.ClientAgreement = clientAgreement;
                incomePaymentOrder.PaymentRegister = paymentRegister;
                incomePaymentOrder.User = user;
                incomePaymentOrder.Colleague = colleague;
                incomePaymentOrder.SupplyOrganization = supplyOrganization;
                incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                incomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;

                toReturn = incomePaymentOrder;
            } else {
                if (paymentCurrencyRegister != null && !toReturn.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.Id.Equals(paymentCurrencyRegister.Id))) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    toReturn.PaymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale == null || toReturn.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) return incomePaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                toReturn.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return incomePaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [IncomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [IncomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [IncomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [IncomePaymentOrder].SadID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [AccountingOperationName] " +
            "ON [AccountingOperationName].OperationType = [IncomePaymentOrder].OperationType " +
            "WHERE [IncomePaymentOrder].NetUID = @NetId",
            types,
            mapper,
            new {
                NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        if (toReturn == null) return toReturn;
        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.AssignedPaymentOrders.Add(assigned);

                return assigned;
            },
            new {
                toReturn.Id
            }
        );

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].AssignedIncomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.RootAssignedPaymentOrder = assigned;

                return assigned;
            },
            new {
                toReturn.Id
            }
        );
        return toReturn;
    }

    public IncomePaymentOrder GetByNetIdReversed(Guid netId) {
        IncomePaymentOrder toReturn = null;

        Type[] types = {
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(ClientAgreement),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(AccountingOperationName)
        };

        Func<object[], IncomePaymentOrder> mapper = objects => {
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
            Client client = (Client)objects[1];
            Organization organization = (Organization)objects[2];
            Currency currency = (Currency)objects[3];
            PaymentRegister paymentRegister = (PaymentRegister)objects[4];
            Organization paymentRegisterOrganization = (Organization)objects[5];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[6];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[7];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[8];
            Sale sale = (Sale)objects[9];
            SaleNumber saleNumber = (SaleNumber)objects[10];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[11];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[12];
            User user = (User)objects[13];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[14];
            PaymentMovement paymentMovement = (PaymentMovement)objects[15];
            User colleague = (User)objects[16];
            ClientAgreement clientAgreement = (ClientAgreement)objects[17];
            OrganizationClient organizationClient = (OrganizationClient)objects[18];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[19];
            Currency organizationClientAgreementCurrency = (Currency)objects[20];
            TaxFree taxFree = (TaxFree)objects[21];
            Sad sad = (Sad)objects[22];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[23];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[24];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[25];

            if (toReturn == null) {
                if (paymentCurrencyRegister != null) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                    incomePaymentOrderSale.Sale = sale;

                    incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                }

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                incomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                incomePaymentOrder.OrganizationClient = organizationClient;
                incomePaymentOrder.TaxFree = taxFree;
                incomePaymentOrder.Sad = sad;
                incomePaymentOrder.Client = client;
                incomePaymentOrder.Organization = organization;
                incomePaymentOrder.Currency = currency;
                incomePaymentOrder.ClientAgreement = clientAgreement;
                incomePaymentOrder.PaymentRegister = paymentRegister;
                incomePaymentOrder.User = user;
                incomePaymentOrder.Colleague = colleague;
                incomePaymentOrder.SupplyOrganization = supplyOrganization;
                incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                incomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;

                toReturn = incomePaymentOrder;
            } else {
                if (paymentCurrencyRegister != null && !toReturn.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.Id.Equals(paymentCurrencyRegister.Id))) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    toReturn.PaymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale == null || toReturn.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) return incomePaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                toReturn.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return incomePaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [IncomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [IncomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [IncomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [IncomePaymentOrder].SadID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [AccountingOperationName] " +
            "ON [AccountingOperationName].OperationType = [AccountingOperationName].OperationType " +
            "WHERE [IncomePaymentOrder].NetUID = @NetId " +
            "ORDER BY [IncomePaymentOrderSale].ID DESC",
            types,
            mapper,
            new {
                NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        if (toReturn == null) return toReturn;
        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.AssignedPaymentOrders.Add(assigned);

                return assigned;
            },
            new {
                toReturn.Id
            }
        );

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].AssignedIncomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.RootAssignedPaymentOrder = assigned;

                return assigned;
            },
            new {
                toReturn.Id
            }
        );
        return toReturn;
    }

    public IncomePaymentOrder GetLastRecord(PaymentRegisterType type) {
        return _connection.Query<IncomePaymentOrder>(
                "SELECT TOP(1) [IncomePaymentOrder].* " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "WHERE [IncomePaymentOrder].Deleted = 0 " +
                "AND [PaymentRegister].Type = @Type " +
                "AND [IncomePaymentOrder].[Number] NOT LIKE '%' + @FromOneC + '%' " +
                "ORDER BY [IncomePaymentOrder].ID DESC",
                new {
                    FromOneC = FROM_ONE_C,
                    Type = type
                }
            )
            .SingleOrDefault();
    }

    public IncomePaymentOrder GetLastBySaleId(long saleId) {
        IncomePaymentOrder toReturn = null;
        _connection.Query<IncomePaymentOrder, IncomePaymentOrderSale, IncomePaymentOrder>(
            "SELECT TOP(1) * " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "AND [IncomePaymentOrderSale].Deleted = 0 " +
            "WHERE [IncomePaymentOrder].IsCanceled = 0 " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrderSale].SaleID = @SaleId " +
            "ORDER BY [IncomePaymentOrder].ID DESC",
            (income, incomeSale) => {
                if (toReturn != null) {
                    toReturn.IncomePaymentOrderSales.Add(incomeSale);
                } else {
                    income.IncomePaymentOrderSales.Add(incomeSale);

                    toReturn = income;
                }

                return income;
            },
            new {
                SaleId = saleId
            }
        );
        return toReturn;
    }

    public IEnumerable<IncomePaymentOrder> GetAll(long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId, Guid? registerNetId,
        long[] organizationIds) {
        List<IncomePaymentOrder> toReturn = new();

        Type[] types = {
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(ReSale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(AccountingOperationName),
            typeof(int)
        };

        Func<object[], IncomePaymentOrder> mapper = objects => {
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
            Client client = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];

            Organization organization = (Organization)objects[4];
            Currency currency = (Currency)objects[5];
            PaymentRegister paymentRegister = (PaymentRegister)objects[6];
            Organization paymentRegisterOrganization = (Organization)objects[7];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[8];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[9];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[10];
            Sale sale = (Sale)objects[11];
            ReSale reSale = (ReSale)objects[12];
            SaleNumber saleNumber = (SaleNumber)objects[13];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[14];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[15];
            User user = (User)objects[16];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[17];
            PaymentMovement paymentMovement = (PaymentMovement)objects[18];
            User colleague = (User)objects[19];
            OrganizationClient organizationClient = (OrganizationClient)objects[20];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[21];
            Currency organizationClientAgreementCurrency = (Currency)objects[22];
            TaxFree taxFree = (TaxFree)objects[23];
            Sad sad = (Sad)objects[24];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[25];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[26];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[27];
            int totalQty = (int)objects[28];

            if (!toReturn.Any(o => o.Id.Equals(incomePaymentOrder.Id))) {
                if (paymentCurrencyRegister != null) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale != null) {
                    if (sale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    } else if (reSale != null) {
                        reSale.SaleNumber = saleNumber;
                        reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    }

                    incomePaymentOrderSale.Sale = sale;
                    incomePaymentOrderSale.ReSale = reSale;

                    incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                }

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                if (clientAgreement != null) {
                    clientAgreement.Agreement = agreement;
                    incomePaymentOrder.ClientAgreement = clientAgreement;
                }

                if (supplyOrganizationAgreement != null) incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                paymentRegister.Organization = paymentRegisterOrganization;

                incomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;
                incomePaymentOrder.EuroAmount = decimal.Round(incomePaymentOrder.EuroAmount, 2, MidpointRounding.AwayFromZero);
                incomePaymentOrder.AgreementExchangedAmount = decimal.Round(incomePaymentOrder.AgreementExchangedAmount, 2, MidpointRounding.AwayFromZero);

                incomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                incomePaymentOrder.OrganizationClient = organizationClient;
                incomePaymentOrder.TaxFree = taxFree;
                incomePaymentOrder.Sad = sad;
                incomePaymentOrder.Client = client;
                incomePaymentOrder.Organization = organization;
                incomePaymentOrder.Currency = currency;
                incomePaymentOrder.PaymentRegister = paymentRegister;
                incomePaymentOrder.User = user;
                incomePaymentOrder.Colleague = colleague;
                incomePaymentOrder.SupplyOrganization = supplyOrganization;
                incomePaymentOrder.TotalQty = totalQty;

                toReturn.Add(incomePaymentOrder);
            } else {
                IncomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(incomePaymentOrder.Id));

                if (paymentCurrencyRegister != null && !fromList.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.Id.Equals(paymentCurrencyRegister.Id))) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    fromList.PaymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale == null || fromList.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) return incomePaymentOrder;

                if (sale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                } else if (reSale != null) {
                    reSale.SaleNumber = saleNumber;
                    reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                }

                incomePaymentOrderSale.Sale = sale;
                incomePaymentOrderSale.ReSale = reSale;

                fromList.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return incomePaymentOrder;
        };

        value = value.Trim();
        string[] concreteValues = value.Split(' ');
        string sqlExpression = string.Empty;
        dynamic props = new ExpandoObject();
        props.Limit = limit;
        props.Offset = offset;
        props.From = from;
        props.To = to;
        props.Value = value;
        props.CurrencyNetId = currencyNetId ?? Guid.Empty;
        props.RegisterNetId = registerNetId ?? Guid.Empty;
        props.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        props.OrganizationIds = organizationIds;
        for (int i = 0;
             i < concreteValues.Length;
             i++)
            (props as ExpandoObject).AddProperty($"Var{i}", concreteValues[i]);

        sqlExpression +=
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [IncomePaymentOrder].FromDate DESC) AS RowNumber " +
            ", [IncomePaymentOrder].ID " +
            ", COUNT(*) OVER() [TotalQty] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "WHERE [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].FromDate >= @From " +
            "AND [IncomePaymentOrder].FromDate <= @To ";
        for (int i = 0;
             i < concreteValues.Length;
             i++)
            sqlExpression +=
                "AND ( " +
                $"[IncomePaymentOrder].Amount like '%' + @Var{i} + '%' " +
                $"OR [Client].FullName like '%' + @Var{i} + '%' " +
                $"OR [IncomePaymentOrder].Number like '%' + @Var{i} + '%' " +
                $"OR [IncomePaymentOrder].Comment like '%' + @Var{i} + '%' " +
                $"OR [User].LastName like '%' + @Var{i} + '%' " +
                $"OR [Colleague].FirstName like '%' + @Var{i} + '%'  " +
                $"OR [Colleague].LastName like '%' + @Var{i} + '%' " +
                $"OR [Currency].Code like '%' + @Var{i} + '%' " +
                $"OR [PaymentRegister].Name like '%' + @Var{i} + '%' " +
                $"OR [SupplyOrganization].Name like '%' + @Var{i} + '%' " +
                $"OR [PaymentMovement].OperationName like '%' + @Var{i} + '%' " +
                ") ";

        if (registerNetId.HasValue) sqlExpression += "AND [PaymentRegister].NetUID = @RegisterNetId ";

        if (currencyNetId.HasValue) sqlExpression += "AND [Currency].NetUID = @CurrencyNetId ";

        if (organizationIds.Any()) sqlExpression += "AND [Organization].ID IN @OrganizationIds ";

        sqlExpression +=
            "GROUP BY IncomePaymentOrder.FromDate, [IncomePaymentOrder].ID " +
            ") " +
            "SELECT *, " +
            "(SELECT TOP 1 TotalQty FROM [Search_CTE]) AS TotalQty " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[SaleNumberID] ELSE [ReSale].[SaleNumberID] END = [SaleNumber].[ID] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[BaseLifeCycleStatusID] ELSE [ReSale].[BaseLifeCycleStatusID] END = [BaseLifeCycleStatus].[ID] " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[BaseSalePaymentStatusID] ELSE [ReSale].[BaseSalePaymentStatusID] END = [BaseSalePaymentStatus].[ID] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [IncomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [IncomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [IncomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [IncomePaymentOrder].SadID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [AccountingOperationName] " +
            "ON [AccountingOperationName].OperationType = [IncomePaymentOrder].OperationType " +
            "WHERE [IncomePaymentOrder].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [IncomePaymentOrder].FromDate DESC, [IncomePaymentOrder].Updated DESC ";
        _connection.Query(
            sqlExpression,
            types,
            mapper,
            (object)props,
            splitOn: "ID,TotalQty"
        );

        if (!toReturn.Any()) return toReturn;

        Type[] assignedPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(SupplyOrganization)
        };

        Func<object[], AssignedPaymentOrder> assignedPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];

            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];

            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[11];

            Client incomePaymentOrderClient = (Client)objects[12];
            Organization incomePaymentOrderOrganization = (Organization)objects[13];
            Currency incomePaymentOrderCurrency = (Currency)objects[14];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[15];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[16];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[17];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[18];
            User incomePaymentOrderUser = (User)objects[19];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[20];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[21];
            User incomePaymentOrderColleague = (User)objects[22];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[23];

            if (outcomePaymentOrder != null) {
                if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                outcomePaymentOrder.User = outcomePaymentOrderUser;
                outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
            }

            if (incomePaymentOrder != null) {
                if (incomePaymentOrderPaymentCurrencyRegister != null) {
                    incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                    incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                }

                if (incomePaymentOrderPaymentMovementOperation != null) {
                    incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                }

                incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                incomePaymentOrder.Client = incomePaymentOrderClient;
                incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                incomePaymentOrder.User = incomePaymentOrderUser;
                incomePaymentOrder.Colleague = incomePaymentOrderColleague;
                incomePaymentOrder.SupplyOrganization = supplyOrganization;
            }

            assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
            assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

            if (assignedPaymentOrder.RootIncomePaymentOrderId != null)
                toReturn.First(o => o.Id.Equals(assignedPaymentOrder.RootIncomePaymentOrderId.Value)).AssignedPaymentOrders.Add(assignedPaymentOrder);

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            assignedPaymentOrderTypes,
            assignedPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        Type[] rootPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(CompanyCarFueling),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SupplyOrganization)
        };

        Func<object[], AssignedPaymentOrder> rootPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];
            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[11];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[12];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[13];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[14];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[15];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[16];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[17];
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[18];
            Client incomePaymentOrderClient = (Client)objects[19];
            Organization incomePaymentOrderOrganization = (Organization)objects[20];
            Currency incomePaymentOrderCurrency = (Currency)objects[21];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[22];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[23];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[24];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[25];
            User incomePaymentOrderUser = (User)objects[26];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[27];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[28];
            User incomePaymentOrderColleague = (User)objects[29];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[30];
            Sale sale = (Sale)objects[31];
            SaleNumber saleNumber = (SaleNumber)objects[32];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[33];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[34];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[35];

            if (assignedPaymentOrder.AssignedIncomePaymentOrderId == null) return assignedPaymentOrder;

            IncomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(assignedPaymentOrder.AssignedIncomePaymentOrderId.Value));

            if (fromList.RootAssignedPaymentOrder == null) {
                if (outcomePaymentOrder != null) {
                    if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                    if (outcomePaymentOrderConsumablesOrder != null) {
                        if (consumablesOrderItem != null) {
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (companyCarFueling != null) outcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);

                    outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                    outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                    outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                    outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                    outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
                }

                if (incomePaymentOrder != null) {
                    if (incomePaymentOrderPaymentCurrencyRegister != null) {
                        incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                        incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                    }

                    if (incomePaymentOrderPaymentMovementOperation != null) {
                        incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                        incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                    }

                    if (incomePaymentOrderSale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                        incomePaymentOrderSale.Sale = sale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                    }

                    incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                    incomePaymentOrder.Client = incomePaymentOrderClient;
                    incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                    incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                    incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                    incomePaymentOrder.User = incomePaymentOrderUser;
                    incomePaymentOrder.Colleague = incomePaymentOrderColleague;
                    incomePaymentOrder.SupplyOrganization = supplyOrganization;
                }

                assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
                assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

                fromList.RootAssignedPaymentOrder = assignedPaymentOrder;
            } else {
                if (outcomePaymentOrder != null && outcomePaymentOrderConsumablesOrder != null) {
                    if (fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                            o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                        if (consumablesOrderItem != null) {
                            OutcomePaymentOrderConsumablesOrder orderFromList =
                                fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(o =>
                                    o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }
                    } else {
                        if (consumablesOrderItem != null) {
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (companyCarFueling != null
                        && !fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.CompanyCarFuelings.Any(o => o.Id.Equals(companyCarFueling.Id)))
                        fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);
                }

                if (incomePaymentOrder == null || incomePaymentOrderSale == null) return assignedPaymentOrder;

                if (fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id)))
                    return assignedPaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [CompanyCarFueling] " +
            "ON [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
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
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] "
            +
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
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "WHERE [AssignedPaymentOrder].AssignedIncomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            rootPaymentOrderTypes,
            rootPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        return toReturn;
    }

    public IEnumerable<IncomePaymentOrder> GetAll(long registerId, long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId) {
        List<IncomePaymentOrder> toReturn = new();

        Type[] types = {
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(SupplyOrganization),
            typeof(AccountingOperationName)
        };

        Func<object[], IncomePaymentOrder> mapper = objects => {
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
            Client client = (Client)objects[1];
            Organization organization = (Organization)objects[2];
            Currency currency = (Currency)objects[3];
            PaymentRegister paymentRegister = (PaymentRegister)objects[4];
            Organization paymentRegisterOrganization = (Organization)objects[5];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[6];
            Currency paymentCurrencyRegisterCurrency = (Currency)objects[7];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[8];
            Sale sale = (Sale)objects[9];
            SaleNumber saleNumber = (SaleNumber)objects[10];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[11];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[12];
            User user = (User)objects[13];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[14];
            PaymentMovement paymentMovement = (PaymentMovement)objects[15];
            User colleague = (User)objects[16];
            OrganizationClient organizationClient = (OrganizationClient)objects[17];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[18];
            Currency organizationClientAgreementCurrency = (Currency)objects[19];
            TaxFree taxFree = (TaxFree)objects[20];
            Sad sad = (Sad)objects[21];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[22];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[23];

            if (!toReturn.Any(o => o.Id.Equals(incomePaymentOrder.Id))) {
                if (paymentCurrencyRegister != null) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                    incomePaymentOrderSale.Sale = sale;

                    incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                }

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                incomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                incomePaymentOrder.OrganizationClient = organizationClient;
                incomePaymentOrder.TaxFree = taxFree;
                incomePaymentOrder.Sad = sad;
                incomePaymentOrder.Client = client;
                incomePaymentOrder.Organization = organization;
                incomePaymentOrder.Currency = currency;
                incomePaymentOrder.PaymentRegister = paymentRegister;
                incomePaymentOrder.User = user;
                incomePaymentOrder.Colleague = colleague;
                incomePaymentOrder.SupplyOrganization = supplyOrganization;
                incomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;


                toReturn.Add(incomePaymentOrder);
            } else {
                IncomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(incomePaymentOrder.Id));

                if (paymentCurrencyRegister != null && !fromList.PaymentRegister.PaymentCurrencyRegisters.Any(r => r.Id.Equals(paymentCurrencyRegister.Id))) {
                    paymentCurrencyRegister.Currency = paymentCurrencyRegisterCurrency;

                    fromList.PaymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                if (incomePaymentOrderSale == null || fromList.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id))) return incomePaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                fromList.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return incomePaymentOrder;
        };

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [IncomePaymentOrder].FromDate DESC) AS RowNumber " +
            ", [IncomePaymentOrder].ID " +
            ", COUNT(*) OVER() [TotalQty] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "WHERE [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].FromDate >= @From " +
            "AND [IncomePaymentOrder].FromDate <= @To " +
            "AND [IncomePaymentOrder].PaymentRegisterID = @RegisterId " +
            "AND ( " +
            "[IncomePaymentOrder].Amount like '%' + @Value + '%' " +
            "OR [Client].FullName like '%' + @Value + '%' " +
            "OR [PaymentRegister].Name like '%' + @Value + '%' " +
            ") ";
        if (currencyNetId.HasValue) sqlExpression += " AND [Currency].NetUID = @CurrencyNetId";

        sqlExpression +=
            ") " +
            "SELECT (SELECT TOP 1 [Search_CTE].TotalQty FROM [Search_CTE]) AS TotalQty, * " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [IncomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [IncomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [IncomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [IncomePaymentOrder].SadID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
            "LEFT JOIN [AccountingOperationName] " +
            "ON [AccountingOperationName].OperationType = [IncomePaymentOrder].OperationType " +
            "WHERE [IncomePaymentOrder].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [IncomePaymentOrder].FromDate DESC";
        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Limit = limit,
                Offset = offset,
                From = from,
                To = to,
                Value = value,
                CurrencyNetId = currencyNetId ?? Guid.Empty,
                RegisterId = registerId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        if (!toReturn.Any()) return toReturn;

        Type[] assignedPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User)
        };

        Func<object[], AssignedPaymentOrder> assignedPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];

            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];

            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[11];

            Client incomePaymentOrderClient = (Client)objects[12];
            Organization incomePaymentOrderOrganization = (Organization)objects[13];
            Currency incomePaymentOrderCurrency = (Currency)objects[14];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[15];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[16];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[17];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[18];
            User incomePaymentOrderUser = (User)objects[19];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[20];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[21];
            User incomePaymentOrderColleague = (User)objects[22];

            if (outcomePaymentOrder != null) {
                if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                outcomePaymentOrder.User = outcomePaymentOrderUser;
                outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
            }

            if (incomePaymentOrder != null) {
                if (incomePaymentOrderPaymentCurrencyRegister != null) {
                    incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                    incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                }

                if (incomePaymentOrderPaymentMovementOperation != null) {
                    incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                }

                incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                incomePaymentOrder.Client = incomePaymentOrderClient;
                incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                incomePaymentOrder.User = incomePaymentOrderUser;
                incomePaymentOrder.Colleague = incomePaymentOrderColleague;
            }

            assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
            assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

            if (assignedPaymentOrder.RootIncomePaymentOrderId != null)
                toReturn.First(o => o.Id.Equals(assignedPaymentOrder.RootIncomePaymentOrderId.Value)).AssignedPaymentOrders.Add(assignedPaymentOrder);

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            assignedPaymentOrderTypes,
            assignedPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        Type[] rootPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus)
        };

        Func<object[], AssignedPaymentOrder> rootPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];
            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[11];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[12];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[13];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[14];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[15];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[16];
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[17];
            Client incomePaymentOrderClient = (Client)objects[18];
            Organization incomePaymentOrderOrganization = (Organization)objects[19];
            Currency incomePaymentOrderCurrency = (Currency)objects[20];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[21];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[22];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[23];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[24];
            User incomePaymentOrderUser = (User)objects[25];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[26];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[27];
            User incomePaymentOrderColleague = (User)objects[28];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[29];
            Sale sale = (Sale)objects[30];
            SaleNumber saleNumber = (SaleNumber)objects[31];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[32];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[33];

            if (assignedPaymentOrder.AssignedIncomePaymentOrderId == null) return assignedPaymentOrder;

            IncomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(assignedPaymentOrder.AssignedIncomePaymentOrderId.Value));

            if (fromList.RootAssignedPaymentOrder == null) {
                if (outcomePaymentOrder != null) {
                    if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                    if (outcomePaymentOrderConsumablesOrder != null) {
                        if (consumablesOrderItem != null) {
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                    outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                    outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                    outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                    outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
                }

                if (incomePaymentOrder != null) {
                    if (incomePaymentOrderPaymentCurrencyRegister != null) {
                        incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                        incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                    }

                    if (incomePaymentOrderPaymentMovementOperation != null) {
                        incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                        incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                    }

                    if (incomePaymentOrderSale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                        incomePaymentOrderSale.Sale = sale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                    }

                    incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                    incomePaymentOrder.Client = incomePaymentOrderClient;
                    incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                    incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                    incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                    incomePaymentOrder.User = incomePaymentOrderUser;
                    incomePaymentOrder.Colleague = incomePaymentOrderColleague;
                }

                assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
                assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

                fromList.RootAssignedPaymentOrder = assignedPaymentOrder;
            } else {
                if (outcomePaymentOrder != null && outcomePaymentOrderConsumablesOrder != null) {
                    if (fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                            o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                        if (consumablesOrderItem != null) {
                            OutcomePaymentOrderConsumablesOrder orderFromList =
                                fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(o =>
                                    o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }
                    } else {
                        if (consumablesOrderItem != null) {
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }
                }

                if (incomePaymentOrder == null || incomePaymentOrderSale == null) return assignedPaymentOrder;

                if (fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id)))
                    return assignedPaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
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
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] "
            +
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
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture" +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "WHERE [AssignedPaymentOrder].AssignedIncomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            rootPaymentOrderTypes,
            rootPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [IncomePaymentOrder] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [IncomePaymentOrder].NetUID = @NetId",
            new {
                NetId = netId
            }
        );
    }

    public TotalDashboardItem GetTotalsAmountByDayAndCurrentMonth() {
        TotalDashboardItem toReturn = new();
        _connection.Query<TotalItem, TotalItem, TotalItem>(
            "DECLARE @INCOME_LIST_TABLE TABLE ( " +
            "[Amount] money, " +
            "[Created] datetime, " +
            "[IsVatIncome] bit " +
            ") " +
            ";WITH LIST_INCOMES_CTE AS ( " +
            "SELECT " +
            "dbo.GetExchangedToEuroValue( " +
            "[IncomePaymentOrder].[Amount] " +
            ", [IncomePaymentOrder].[CurrencyID], " +
            "[IncomePaymentOrder].[Created]) / " +
            "CASE " +
            "WHEN [IncomePaymentOrder].[ExchangeRate] <= 0 " +
            "THEN 1 " +
            "ELSE [IncomePaymentOrder].[ExchangeRate] END AS [Amount], " +
            "[IncomePaymentOrder].[Created], " +
            "[IncomePaymentOrder].[IsAccounting] AS [IsVatIncome] " +
            "FROM [IncomePaymentOrder] " +
            "WHERE [IncomePaymentOrder].[Deleted] = 0 " +
            ") " +
            "INSERT INTO @INCOME_LIST_TABLE([Amount], [Created],[IsVatIncome]) " +
            "SELECT [Amount] " +
            ",[Created] " +
            ",[IsVatIncome] " +
            "FROM [LIST_INCOMES_CTE] " +
            "SELECT ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1 " +
            ")       AS [ValueByDay] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) - " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) " +
            ")   AS [IncreaseByDay] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) > " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByDay] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 1 " +
            ")       AS [ValueByMonth] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 1) " +
            ")   AS [IncreaseByMonth] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 1) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByMonth] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0 " +
            ")       AS [ValueByDay] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) " +
            ")   AS [IncreaseByDay] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByDay] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 0 " +
            ")       AS [ValueByMonth] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 0) " +
            ")   AS [IncreaseByMonth] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @INCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 0) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByMonth] "
            , (vatTotalItem, notVatTotalItem) => {
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
            }, new {
                QtyHour = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl") ? 3 : 2
            }, splitOn: "ValueByDay");
        return toReturn;
    }

    public IEnumerable<GroupedPaymentsByPeriod> GetFilteredGroupedPaymentsByPeriod(
        DateTime from,
        DateTime to,
        TypePeriodGrouping period,
        Guid? netId) {
        List<GroupedPaymentsByPeriod> toReturn = new();

        string byPaymentCurrencyRegisterNetId =
            netId.HasValue ? "AND [PaymentCurrencyRegister].[NetUID] = @NetId " : "";

        string joinForPaymentCurrencyRegisterNetId = netId.HasValue
            ? "LEFT JOIN [PaymentRegister] " +
              "ON [PaymentRegister].[ID] = [PaymentRegisterID] " +
              "LEFT JOIN [PaymentCurrencyRegister] " +
              "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] "
            : "";

        _connection.Query<DateTime, decimal, TypeMovement, decimal>(
            string.Format(";WITH REVENUES_EXPENDITURES_CTE AS ( " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0}, 0, [IncomePaymentOrder].[Created]), 0) AS [Period] " +
                          ", CONVERT( " +
                          "money, SUM(dbo.GetExchangedToEuroValue( " +
                          "[IncomePaymentOrder].[Amount], " +
                          "[IncomePaymentOrder].[CurrencyID], " +
                          "[IncomePaymentOrder].[Created])) " +
                          ") AS [Amount] " +
                          ", 2 AS [TypeMovement] " +
                          "FROM [IncomePaymentOrder] " +
                          joinForPaymentCurrencyRegisterNetId +
                          "WHERE [IncomePaymentOrder].[Deleted] = 0 " +
                          "AND [IncomePaymentOrder].[IsAccounting] = 0 " +
                          "AND [IncomePaymentOrder].[Created] >= @From " +
                          "AND [IncomePaymentOrder].[Created] <= @To " +
                          byPaymentCurrencyRegisterNetId +
                          "GROUP BY DATEADD({0}, DATEDIFF({0},0, [IncomePaymentOrder].[Created]), 0) " +
                          ", [IncomePaymentOrder].[IsAccounting] " +
                          "UNION " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0},0, [IncomePaymentOrder].[Created]), 0) AS [Period] " +
                          ", CONVERT( " +
                          "money, SUM(dbo.GetExchangedToEuroValue( " +
                          "[IncomePaymentOrder].[Amount], " +
                          "[IncomePaymentOrder].[CurrencyID], " +
                          "[IncomePaymentOrder].[Created])) " +
                          ") AS [Amount] " +
                          ", 1 AS [TypeMovement] " +
                          "FROM [IncomePaymentOrder] " +
                          joinForPaymentCurrencyRegisterNetId +
                          "WHERE [IncomePaymentOrder].[Deleted] = 0 " +
                          "AND [IncomePaymentOrder].[IsAccounting] = 1 " +
                          "AND [IncomePaymentOrder].[Created] >= @From " +
                          "AND [IncomePaymentOrder].[Created] <= @To " +
                          byPaymentCurrencyRegisterNetId +
                          "GROUP BY DATEADD({0}, DATEDIFF({0},0, [IncomePaymentOrder].[Created]), 0) " +
                          ", [IncomePaymentOrder].[IsAccounting] " +
                          "UNION " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0},0, [OutcomePaymentOrder].[Created]), 0) AS [Period] " +
                          ", CONVERT( " +
                          "money, SUM(dbo.GetExchangedToEuroValue( " +
                          "[OutcomePaymentOrder].[Amount], " +
                          "[PaymentCurrencyRegister].[CurrencyID], " +
                          "[OutcomePaymentOrder].[Created])) " +
                          ") AS [Amount] " +
                          ", 4 AS [TypeMovement] " +
                          "FROM [OutcomePaymentOrder] " +
                          "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                          "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                          "LEFT JOIN [SupplyPaymentTask] " +
                          "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                          "LEFT JOIN [PaymentCurrencyRegister] " +
                          "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                          "WHERE [OutcomePaymentOrder].[Deleted] = 0 " +
                          "AND [SupplyPaymentTask].[IsAccounting] = 0 " +
                          "AND [SupplyPaymentTask].[Deleted] = 0 " +
                          "AND [OutcomePaymentOrder].[Created] >= @From " +
                          "AND [OutcomePaymentOrder].[Created] <= @To " +
                          byPaymentCurrencyRegisterNetId +
                          "GROUP BY DATEADD({0}, DATEDIFF({0},0, [OutcomePaymentOrder].[Created]), 0) " +
                          ", [SupplyPaymentTask].[IsAccounting] " +
                          "UNION " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0},0, [OutcomePaymentOrder].[Created]), 0) AS [Period] " +
                          ", CONVERT( " +
                          "money, SUM(dbo.GetExchangedToEuroValue( " +
                          "[OutcomePaymentOrder].[Amount], " +
                          "[PaymentCurrencyRegister].[CurrencyID], " +
                          "[OutcomePaymentOrder].[Created])) " +
                          ") AS [Amount] " +
                          ", 3 AS [TypeMovement] " +
                          "FROM [OutcomePaymentOrder] " +
                          "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                          "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                          "LEFT JOIN [SupplyPaymentTask] " +
                          "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                          "LEFT JOIN [PaymentCurrencyRegister] " +
                          "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                          "WHERE [OutcomePaymentOrder].[Deleted] = 0 " +
                          "AND [SupplyPaymentTask].[IsAccounting] = 1 " +
                          "AND [SupplyPaymentTask].[Deleted] = 0 " +
                          "AND [OutcomePaymentOrder].[Created] >= @From " +
                          "AND [OutcomePaymentOrder].[Created] <= @To " +
                          byPaymentCurrencyRegisterNetId +
                          "GROUP BY DATEADD({0}, DATEDIFF({0},0, [OutcomePaymentOrder].[Created]), 0) " +
                          ", [SupplyPaymentTask].[IsAccounting] " +
                          "UNION " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0},0, [IncomePaymentOrder].[Created]), 0) AS [Period] " +
                          ", CONVERT( " +
                          "money, SUM(dbo.GetExchangedToEuroValue( " +
                          "[IncomePaymentOrder].[Amount], " +
                          "[IncomePaymentOrder].[CurrencyID], " +
                          "[IncomePaymentOrder].[Created])) " +
                          ") AS [Amount] " +
                          ", 5 AS [TypeMovement] " +
                          "FROM [IncomePaymentOrder] " +
                          joinForPaymentCurrencyRegisterNetId +
                          "WHERE [IncomePaymentOrder].[Deleted] = 0 " +
                          "AND [IncomePaymentOrder].[Created] >= @From " +
                          "AND [IncomePaymentOrder].[Created] <= @To " +
                          byPaymentCurrencyRegisterNetId +
                          "GROUP BY DATEADD({0}, DATEDIFF({0},0, [IncomePaymentOrder].[Created]), 0) " +
                          "UNION " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0},0, [OutcomePaymentOrder].[Created]), 0) AS [Period] " +
                          ", CONVERT( " +
                          "money, SUM(dbo.GetExchangedToEuroValue( " +
                          "[OutcomePaymentOrder].[Amount], " +
                          "[PaymentCurrencyRegister].[CurrencyID], " +
                          "[OutcomePaymentOrder].[Created])) " +
                          ") AS [Amount] " +
                          ", 6 AS [TypeMovement] " +
                          "FROM [OutcomePaymentOrder] " +
                          "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                          "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                          "LEFT JOIN [SupplyPaymentTask] " +
                          "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                          "LEFT JOIN [PaymentCurrencyRegister] " +
                          "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                          "WHERE [OutcomePaymentOrder].[Deleted] = 0 " +
                          "AND [SupplyPaymentTask].[Deleted] = 0 " +
                          byPaymentCurrencyRegisterNetId +
                          "GROUP BY DATEADD({0}, DATEDIFF({0},0, [OutcomePaymentOrder].[Created]), 0) " +
                          ") " +
                          "SELECT " +
                          "DATEADD({0}, DATEDIFF({0}, 0 , DATEADD({0}, x.number, @From)), 0) AS [Period] " +
                          ", CASE " +
                          "WHEN [REVENUES_EXPENDITURES_CTE].[Amount] IS NOT NULL " +
                          "THEN [REVENUES_EXPENDITURES_CTE].[Amount] " +
                          "ELSE 0 " +
                          "END AS [Amount] " +
                          ", CASE " +
                          "WHEN [REVENUES_EXPENDITURES_CTE].[TypeMovement] IS NOT NULL " +
                          "THEN [REVENUES_EXPENDITURES_CTE].[TypeMovement] " +
                          "ELSE 0 " +
                          "END AS [TypeMovement] " +
                          "FROM master.dbo.spt_values x " +
                          "LEFT JOIN [REVENUES_EXPENDITURES_CTE] " +
                          "ON [REVENUES_EXPENDITURES_CTE].[Period] = DATEADD({0}, DATEDIFF({0}, 0 , DATEADD({0}, x.number, @From)), 0) " +
                          "WHERE x.type = 'P' " +
                          "AND  x.number <= DATEDIFF({0}, @From, @To) ", period == TypePeriodGrouping.Day ? "DAY" :
                period == TypePeriodGrouping.Week ? "WEEK" :
                period == TypePeriodGrouping.Month ? "MONTH" :
                period == TypePeriodGrouping.Year ? "YEAR" : "MONTH"),
            (periodMovement, amount, type) => {
                GroupedPaymentsByPeriod movement = new() {
                    Period = periodMovement
                };

                if (!toReturn.Any(x => x.Period.Equals(periodMovement)))
                    toReturn.Add(movement);
                else
                    movement = toReturn.First(x => x.Period.Equals(periodMovement));

                if (type.Equals(TypeMovement.NotMovement)) return amount;

                switch (type) {
                    case TypeMovement.IncomeVat:
                        movement.Vat.Income = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                        break;
                    case TypeMovement.IncomeNotVat:
                        movement.NotVat.Income = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                        break;
                    case TypeMovement.OutcomeVat:
                        movement.Vat.Outcome = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                        break;
                    case TypeMovement.OutcomeNotVat:
                        movement.NotVat.Outcome = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                        break;
                    case TypeMovement.TotalIncome:
                        movement.TotalIncome = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                        break;
                    case TypeMovement.TotalOutcome:
                        movement.TotalOutcome = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                        break;
                }

                return amount;
            },
            new {
                From = from, To = to, NetId = netId ?? Guid.Empty
            }, splitOn: "Amount,TypeMovement");
        return toReturn;
    }
}