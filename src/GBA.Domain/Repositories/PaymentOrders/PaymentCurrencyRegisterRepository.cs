using System;
using System.Collections.Generic;
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
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentCurrencyRegisterRepository : IPaymentCurrencyRegisterRepository {
    private readonly IDbConnection _connection;

    public PaymentCurrencyRegisterRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<PaymentCurrencyRegister> paymentCurrencyRegisters) {
        _connection.Execute(
            "INSERT INTO [PaymentCurrencyRegister] (Amount, InitialAmount, PaymentRegisterId, CurrencyId, Updated) " +
            "VALUES (@Amount, @InitialAmount, @PaymentRegisterId, @CurrencyId, getutcdate())",
            paymentCurrencyRegisters
        );
    }

    public void UpdateAmount(PaymentCurrencyRegister paymentCurrencyRegister) {
        _connection.Execute(
            "UPDATE [PaymentCurrencyRegister] " +
            "SET Amount = @Amount, Updated = getutcdate() " +
            "WHERE [PaymentCurrencyRegister].ID = @Id",
            paymentCurrencyRegister
        );
    }

    public PaymentCurrencyRegister GetById(long id) {
        return _connection.Query<PaymentCurrencyRegister, Currency, PaymentCurrencyRegister>(
                "SELECT * " +
                "FROM [PaymentCurrencyRegister] " +
                "LEFT JOIN [Currency] " +
                "ON [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "WHERE [PaymentCurrencyRegister].ID = @Id",
                (register, currency) => {
                    register.Currency = currency;

                    return register;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public List<PaymentCurrencyRegister> GetAll() {
        return _connection.Query<PaymentCurrencyRegister, Currency, PaymentCurrencyRegister>(
                "SELECT * " +
                "FROM [PaymentCurrencyRegister] " +
                "LEFT JOIN [Currency] " +
                "ON [PaymentCurrencyRegister].CurrencyID = [Currency].ID ",
                (register, currency) => {
                    register.Currency = currency;

                    return register;
                }
            )
            .ToList();
    }

    public PaymentCurrencyRegister GetByNetId(Guid netId) {
        return _connection.Query<PaymentCurrencyRegister, Currency, PaymentCurrencyRegister>(
                "SELECT * " +
                "FROM [PaymentCurrencyRegister] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "WHERE [PaymentCurrencyRegister].NetUID = @NetId",
                (register, currency) => {
                    register.Currency = currency;

                    return register;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public PaymentCurrencyRegister GetByNetIdFiltered(Guid netId, DateTime from, DateTime to) {
        PaymentCurrencyRegister toReturn = null;

        _connection.Query<PaymentCurrencyRegister, Currency, PaymentRegister, Organization, PaymentCurrencyRegister>(
            "SELECT * " +
            "FROM [PaymentCurrencyRegister] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [PaymentRegister].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [PaymentCurrencyRegister].NetUID = @NetId",
            (currencyRegister, currency, paymentRegister, organization) => {
                paymentRegister.Organization = organization;

                currencyRegister.Currency = currency;
                currencyRegister.PaymentRegister = paymentRegister;

                toReturn = currencyRegister;

                return currencyRegister;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn != null) {
            Type[] incomesTypes = {
                typeof(IncomePaymentOrder),
                typeof(Client),
                typeof(Organization),
                typeof(Currency),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(Client),
                typeof(RegionCode),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement)
            };

            Func<object[], IncomePaymentOrder> incomesMapper = objects => {
                IncomePaymentOrder income = (IncomePaymentOrder)objects[0];
                Client client = (Client)objects[1];
                Organization organization = (Organization)objects[2];
                Currency currency = (Currency)objects[3];
                User user = (User)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User colleague = (User)objects[7];
                ClientAgreement clientAgreement = (ClientAgreement)objects[8];
                Agreement agreement = (Agreement)objects[9];
                Currency agreementCurrency = (Currency)objects[10];
                Client agreementClient = (Client)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                SupplyOrganization supplyOrganization = (SupplyOrganization)objects[13];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[14];

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    income.PaymentMovementOperation = paymentMovementOperation;
                }

                if (clientAgreement != null) {
                    agreementClient.RegionCode = regionCode;

                    agreement.Currency = agreementCurrency;

                    clientAgreement.Client = agreementClient;
                    clientAgreement.Agreement = agreement;
                }

                income.Client = client;
                income.Organization = organization;
                income.Currency = currency;
                income.User = user;
                income.Colleague = colleague;
                income.SupplyOrganization = supplyOrganization;
                income.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.PaymentRegister.IncomePaymentOrders.Add(income);

                toReturn.RangeTotal = Math.Round(toReturn.RangeTotal + income.Amount, 2);

                return income;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [IncomePaymentOrder].ClientID " +
                "LEFT JOIN [views].[OrganizationView] AS [IncomePaymentOrderOrganization] " +
                "ON [IncomePaymentOrderOrganization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [IncomePaymentOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [IncomePaymentOrderCurrency] " +
                "ON [IncomePaymentOrderCurrency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [IncomePaymentOrderCurrency].CultureCode = @Culture " +
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
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [AgreementClient] " +
                "ON [AgreementClient].ID = [ClientAgreement].ClientID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [AgreementClient].RegionCodeID " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
                "WHERE [IncomePaymentOrder].PaymentRegisterID = @PaymentRegisterId " +
                "AND [IncomePaymentOrder].CurrencyID = @CurrencyId " +
                "AND (" +
                "[IncomePaymentOrder].FromDate >= @From " +
                "AND " +
                "[IncomePaymentOrder].FromDate < @To " +
                ")",
                incomesTypes,
                incomesMapper,
                new { toReturn.PaymentRegisterId, toReturn.CurrencyId, From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

            if (toReturn.PaymentRegister.IncomePaymentOrders.Any()) {
                _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
                    "SELECT * " +
                    "FROM [AssignedPaymentOrder] " +
                    "LEFT JOIN [OutcomePaymentOrder] " +
                    "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                    "LEFT JOIN [IncomePaymentOrder] " +
                    "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                    "WHERE [AssignedPaymentOrder].RootIncomePaymentOrderID IN @Ids " +
                    "AND [AssignedPaymentOrder].Deleted = 0",
                    (assigned, assignedOutcome, assignedIncome) => {
                        if (assigned.RootIncomePaymentOrderId == null) return assigned;

                        assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                        assigned.AssignedIncomePaymentOrder = assignedIncome;

                        toReturn.PaymentRegister.IncomePaymentOrders.First(o => o.Id.Equals(assigned.RootIncomePaymentOrderId.Value)).AssignedPaymentOrders.Add(assigned);

                        return assigned;
                    },
                    new { Ids = toReturn.OutcomePaymentOrders.Select(o => o.Id) }
                );

                _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
                    "SELECT * " +
                    "FROM [AssignedPaymentOrder] " +
                    "LEFT JOIN [OutcomePaymentOrder] " +
                    "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
                    "LEFT JOIN [IncomePaymentOrder] " +
                    "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
                    "WHERE [AssignedPaymentOrder].AssignedIncomePaymentOrderID IN @Ids " +
                    "AND [AssignedPaymentOrder].Deleted = 0",
                    (assigned, assignedOutcome, assignedIncome) => {
                        if (assigned.AssignedIncomePaymentOrderId == null) return assigned;

                        assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                        assigned.AssignedIncomePaymentOrder = assignedIncome;

                        toReturn.PaymentRegister.IncomePaymentOrders.First(o => o.Id.Equals(assigned.AssignedIncomePaymentOrderId.Value)).RootAssignedPaymentOrder = assigned;

                        return assigned;
                    },
                    new { Ids = toReturn.OutcomePaymentOrders.Select(o => o.Id) }
                );
            }

            string transfersSqlStatement =
                "SELECT * " +
                "FROM [PaymentRegisterTransfer] " +
                "LEFT JOIN [User] AS [PaymentRegisterTransferUser] " +
                "ON [PaymentRegisterTransferUser].ID = [PaymentRegisterTransfer].UserID " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentRegisterTransferRegister] " +
                "ON [FromPaymentRegisterTransferRegister].ID = [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [FromPaymentRegisterTransferRegisterCurrency] " +
                "ON [FromPaymentRegisterTransferRegisterCurrency].ID = [FromPaymentRegisterTransferRegister].CurrencyID " +
                "AND [FromPaymentRegisterTransferRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentRegisterTransferRegisterPaymentRegister] " +
                "ON [FromPaymentRegisterTransferRegisterPaymentRegister].ID = [FromPaymentRegisterTransferRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [FromPaymentRegisterTransferRegisterPaymentRegisterOrganization] " +
                "ON [FromPaymentRegisterTransferRegisterPaymentRegisterOrganization].ID = [FromPaymentRegisterTransferRegisterPaymentRegister].OrganizationID " +
                "AND [FromPaymentRegisterTransferRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentRegisterTransferRegister] " +
                "ON [ToPaymentRegisterTransferRegister].ID = [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [ToPaymentRegisterTransferRegisterCurrency] " +
                "ON [ToPaymentRegisterTransferRegisterCurrency].ID = [ToPaymentRegisterTransferRegister].CurrencyID " +
                "AND [ToPaymentRegisterTransferRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentRegisterTransferRegisterPaymentRegister] " +
                "ON [ToPaymentRegisterTransferRegisterPaymentRegister].ID = [ToPaymentRegisterTransferRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [ToPaymentRegisterTransferRegisterPaymentRegisterOrganization] " +
                "ON [ToPaymentRegisterTransferRegisterPaymentRegisterOrganization].ID = [ToPaymentRegisterTransferRegisterPaymentRegister].OrganizationID " +
                "AND [ToPaymentRegisterTransferRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentRegisterTransfer].ID = [PaymentMovementOperation].PaymentRegisterTransferID " +
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
                "WHERE (" +
                "[PaymentRegisterTransfer].FromPaymentCurrencyRegisterID = @Id " +
                "OR " +
                "[PaymentRegisterTransfer].ToPaymentCurrencyRegisterID = @Id " +
                ") " +
                "AND ( " +
                "[PaymentRegisterTransfer].FromDate >= @From " +
                "AND " +
                "[PaymentRegisterTransfer].FromDate < @To " +
                ")";

            var joinProps = new { toReturn.Id, From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

            Type[] transfersTypes = {
                typeof(PaymentRegisterTransfer),
                typeof(User),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement)
            };

            Func<object[], PaymentRegisterTransfer> transfersMapper = objects => {
                PaymentRegisterTransfer paymentRegisterTransfer = (PaymentRegisterTransfer)objects[0];
                User user = (User)objects[1];
                PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[2];
                Currency fromCurrency = (Currency)objects[3];
                PaymentRegister fromPaymentRegister = (PaymentRegister)objects[4];
                Organization fromOrganization = (Organization)objects[5];
                PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[6];
                Currency toCurrency = (Currency)objects[7];
                PaymentRegister toPaymentRegister = (PaymentRegister)objects[8];
                Organization toOrganization = (Organization)objects[9];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[10];
                PaymentMovement paymentMovement = (PaymentMovement)objects[11];

                if (fromPaymentCurrencyRegister.NetUid.Equals(netId)) {
                    paymentRegisterTransfer.Type = PaymentRegisterTransferType.Outcome;

                    toReturn.RangeTotal = Math.Round(toReturn.RangeTotal - paymentRegisterTransfer.Amount, 2);
                } else {
                    paymentRegisterTransfer.Type = PaymentRegisterTransferType.Income;

                    toReturn.RangeTotal = Math.Round(toReturn.RangeTotal + paymentRegisterTransfer.Amount, 2);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegisterTransfer.PaymentMovementOperation = paymentMovementOperation;
                }

                fromPaymentRegister.Organization = fromOrganization;

                fromPaymentCurrencyRegister.Currency = fromCurrency;
                fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

                toPaymentRegister.Organization = toOrganization;

                toPaymentCurrencyRegister.Currency = toCurrency;
                toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

                paymentRegisterTransfer.User = user;
                paymentRegisterTransfer.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
                paymentRegisterTransfer.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;

                toReturn.PaymentRegisterTransfers.Add(paymentRegisterTransfer);

                return paymentRegisterTransfer;
            };

            _connection.Query(
                transfersSqlStatement,
                transfersTypes,
                transfersMapper,
                joinProps
            );

            string currencyExchangesSqlStatement =
                "SELECT * " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "LEFT JOIN [User] AS [PaymentRegisterCurrencyExchangeUser] " +
                "ON [PaymentRegisterCurrencyExchangeUser].ID = [PaymentRegisterCurrencyExchange].UserID " +
                "LEFT JOIN [CurrencyTrader] " +
                "ON [CurrencyTrader].ID = [PaymentRegisterCurrencyExchange].CurrencyTraderID " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentRegisterCurrencyExchangeRegister] " +
                "ON [FromPaymentRegisterCurrencyExchangeRegister].ID = [PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [FromPaymentRegisterCurrencyExchangeRegisterCurrency] " +
                "ON [FromPaymentRegisterCurrencyExchangeRegisterCurrency].ID = [FromPaymentRegisterCurrencyExchangeRegister].CurrencyID " +
                "AND [FromPaymentRegisterCurrencyExchangeRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentRegisterCurrencyExchangeRegisterPaymentRegister] " +
                "ON [FromPaymentRegisterCurrencyExchangeRegisterPaymentRegister].ID = [FromPaymentRegisterCurrencyExchangeRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [FromPaymentRegisterCurrencyExchangeRegisterPaymentRegisterOrganization] " +
                "ON [FromPaymentRegisterCurrencyExchangeRegisterPaymentRegisterOrganization].ID = [FromPaymentRegisterCurrencyExchangeRegisterPaymentRegister].OrganizationID " +
                "AND [FromPaymentRegisterCurrencyExchangeRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentRegisterCurrencyExchangeRegister] " +
                "ON [ToPaymentRegisterCurrencyExchangeRegister].ID = [PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [ToPaymentRegisterCurrencyExchangeRegisterCurrency] " +
                "ON [ToPaymentRegisterCurrencyExchangeRegisterCurrency].ID = [ToPaymentRegisterCurrencyExchangeRegister].CurrencyID " +
                "AND [ToPaymentRegisterCurrencyExchangeRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentRegisterCurrencyExchangeRegisterPaymentRegister] " +
                "ON [ToPaymentRegisterCurrencyExchangeRegisterPaymentRegister].ID = [ToPaymentRegisterCurrencyExchangeRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [ToPaymentRegisterCurrencyExchangeRegisterPaymentRegisterOrganization] " +
                "ON [ToPaymentRegisterCurrencyExchangeRegisterPaymentRegisterOrganization].ID = [ToPaymentRegisterCurrencyExchangeRegisterPaymentRegister].OrganizationID " +
                "AND [ToPaymentRegisterCurrencyExchangeRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentRegisterCurrencyExchange].ID = [PaymentMovementOperation].PaymentRegisterCurrencyExchangeID " +
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
                "WHERE (" +
                "[PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID = @Id " +
                "OR " +
                "[PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID = @Id" +
                ") " +
                "AND (" +
                "[PaymentRegisterCurrencyExchange].FromDate >= @From " +
                "AND " +
                "[PaymentRegisterCurrencyExchange].FromDate < @To " +
                ")";

            Type[] currencyExchangesTypes = {
                typeof(PaymentRegisterCurrencyExchange),
                typeof(User),
                typeof(CurrencyTrader),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement)
            };

            Func<object[], PaymentRegisterCurrencyExchange> currencyExchangesMapper = objects => {
                PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange = (PaymentRegisterCurrencyExchange)objects[0];
                User user = (User)objects[1];
                CurrencyTrader currencyTrader = (CurrencyTrader)objects[2];
                PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[3];
                Currency fromCurrency = (Currency)objects[4];
                PaymentRegister fromPaymentRegister = (PaymentRegister)objects[5];
                Organization fromOrganization = (Organization)objects[6];
                PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[7];
                Currency toCurrency = (Currency)objects[8];
                PaymentRegister toPaymentRegister = (PaymentRegister)objects[9];
                Organization toOrganization = (Organization)objects[10];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[11];
                PaymentMovement paymentMovement = (PaymentMovement)objects[12];

                if (fromPaymentCurrencyRegister.NetUid.Equals(netId)) {
                    paymentRegisterCurrencyExchange.Type = PaymentRegisterTransferType.Outcome;

                    toReturn.RangeTotal = Math.Round(toReturn.RangeTotal - paymentRegisterCurrencyExchange.Amount, 2);
                } else {
                    paymentRegisterCurrencyExchange.Type = PaymentRegisterTransferType.Income;

                    if (fromCurrency.Code.ToLower().Equals("uah"))
                        paymentRegisterCurrencyExchange.Amount =
                            Math.Round(
                                Math.Round(paymentRegisterCurrencyExchange.Amount / paymentRegisterCurrencyExchange.ExchangeRate, 2)
                                , 2);
                    else
                        paymentRegisterCurrencyExchange.Amount =
                            Math.Round(
                                Math.Round(paymentRegisterCurrencyExchange.Amount * paymentRegisterCurrencyExchange.ExchangeRate, 2)
                                , 2);

                    toReturn.RangeTotal = Math.Round(toReturn.RangeTotal + paymentRegisterCurrencyExchange.Amount, 2);
                }

                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegisterCurrencyExchange.PaymentMovementOperation = paymentMovementOperation;
                }

                fromPaymentRegister.Organization = fromOrganization;

                fromPaymentCurrencyRegister.Currency = fromCurrency;
                fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

                toPaymentRegister.Organization = toOrganization;

                toPaymentCurrencyRegister.Currency = toCurrency;
                toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

                paymentRegisterCurrencyExchange.User = user;
                paymentRegisterCurrencyExchange.CurrencyTrader = currencyTrader;
                paymentRegisterCurrencyExchange.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
                paymentRegisterCurrencyExchange.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;

                toReturn.PaymentRegisterCurrencyExchanges.Add(paymentRegisterCurrencyExchange);

                return paymentRegisterCurrencyExchange;
            };

            _connection.Query(
                currencyExchangesSqlStatement,
                currencyExchangesTypes,
                currencyExchangesMapper,
                joinProps
            );

            string outcomeOrdersExpression =
                "SELECT * " +
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
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeSupplyOrganization] " +
                "ON [OutcomeSupplyOrganization].ID = [OutcomePaymentOrder].ConsumableProductOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [OutcomeSupplyOrganization].[ID] " +
                "LEFT JOIN [views].[OrganizationView] AS [OutcomeSupplyOrganizationOrganization] " +
                "ON [OutcomeSupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [OutcomeSupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [OutcomeClient] " +
                "ON [OutcomeClient].ID = [OutcomePaymentOrder].ClientID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [OutcomePaymentOrder].ClientAgreementID = [ClientAgreement].ID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [ClientAgreement].ClientID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].PaymentCurrencyRegisterID = @Id " +
                "AND [OutcomePaymentOrder].FromDate >= @From " +
                "AND [OutcomePaymentOrder].FromDate < @To";

            Type[] outcomeOrdersTypes = {
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
                typeof(User),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Client),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency),
                typeof(Client),
                typeof(RegionCode),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> outcomeOrdersMapper = objects => {
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
                User colleague = (User)objects[15];
                User consumablesOrderUser = (User)objects[16];
                SupplyOrganization outcomePaymentOrderSupplyOrganization = (SupplyOrganization)objects[17];
                SupplyOrganizationAgreement outcomePaymentOrderSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[18];
                Organization outcomePaymentOrderSupplyOrganizationOrganization = (Organization)objects[19];
                Client outcomeClient = (Client)objects[20];
                ClientAgreement clientAgreement = (ClientAgreement)objects[21];
                Agreement agreement = (Agreement)objects[22];
                Currency agreementCurrency = (Currency)objects[23];
                Client client = (Client)objects[24];
                RegionCode regionCode = (RegionCode)objects[25];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[26];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[27];
                SupplyOrganizationAgreement consumablesOrderSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[28];
                Currency consumablesOrderSupplyOrganizationAgreementCurrency = (Currency)objects[29];

                if (!toReturn.OutcomePaymentOrders.Any(o => o.Id.Equals(outcomePaymentOrder.Id))) {
                    if (consumablesOrder != null && consumablesOrderItem != null) {
                        if (consumablesOrderSupplyOrganizationAgreement != null)
                            consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        consumablesOrder.User = consumablesOrderUser;
                        consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrder.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    if (outcomePaymentOrderSupplyOrganization != null) {
                        if (!outcomePaymentOrderSupplyOrganization.SupplyOrganizationAgreements.Any(x => x.Id.Equals(outcomePaymentOrderSupplyOrganizationAgreement.Id)))
                            outcomePaymentOrderSupplyOrganization.SupplyOrganizationAgreements.Add(outcomePaymentOrderSupplyOrganizationAgreement);
                        else
                            outcomePaymentOrderSupplyOrganizationAgreement =
                                outcomePaymentOrderSupplyOrganization.SupplyOrganizationAgreements.First(x =>
                                    x.Id.Equals(outcomePaymentOrderSupplyOrganizationAgreement.Id));

                        outcomePaymentOrderSupplyOrganizationAgreement.Organization = outcomePaymentOrderSupplyOrganizationOrganization;
                    }

                    if (clientAgreement != null) {
                        client.RegionCode = regionCode;

                        agreement.Currency = agreementCurrency;

                        clientAgreement.Client = client;
                        clientAgreement.Agreement = agreement;
                    }

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                    paymentRegister.Organization = paymentRegisterOrganization;

                    paymentCurrencyRegister.PaymentRegister = paymentRegister;
                    paymentCurrencyRegister.Currency = currency;

                    outcomePaymentOrder.Organization = organization;
                    outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                    outcomePaymentOrder.User = user;
                    outcomePaymentOrder.ClientAgreement = clientAgreement;
                    outcomePaymentOrder.ConsumableProductOrganization = outcomePaymentOrderSupplyOrganization;
                    outcomePaymentOrder.Colleague = colleague;
                    outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    outcomePaymentOrder.Client = outcomeClient;

                    toReturn.RangeTotal = Math.Round(toReturn.RangeTotal - outcomePaymentOrder.Amount, 2);

                    toReturn.OutcomePaymentOrders.Add(outcomePaymentOrder);
                } else {
                    OutcomePaymentOrder fromList = toReturn.OutcomePaymentOrders.First(o => o.Id.Equals(outcomePaymentOrder.Id));

                    if (outcomePaymentOrderConsumablesOrder == null) return outcomePaymentOrder;

                    if (outcomePaymentOrderSupplyOrganization != null &&
                        !outcomePaymentOrderSupplyOrganization.SupplyOrganizationAgreements.Any(x => x.Id.Equals(outcomePaymentOrderSupplyOrganizationAgreement.Id)))
                        outcomePaymentOrderSupplyOrganization.SupplyOrganizationAgreements.Add(outcomePaymentOrderSupplyOrganizationAgreement);

                    if (fromList.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                        if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                        OutcomePaymentOrderConsumablesOrder orderFromList =
                            fromList.OutcomePaymentOrderConsumablesOrders.First(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                        if (consumablesOrderSupplyOrganizationAgreement != null)
                            consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                    } else {
                        if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                        if (consumablesOrderSupplyOrganizationAgreement != null)
                            consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        consumablesOrder.User = consumablesOrderUser;
                        consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrder.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        fromList.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                outcomeOrdersExpression,
                outcomeOrdersTypes,
                outcomeOrdersMapper,
                joinProps
            );

            if (toReturn.OutcomePaymentOrders.Any()) {
                _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
                    "SELECT * " +
                    "FROM [AssignedPaymentOrder] " +
                    "LEFT JOIN [OutcomePaymentOrder] " +
                    "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                    "LEFT JOIN [IncomePaymentOrder] " +
                    "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                    "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID IN @Ids " +
                    "AND [AssignedPaymentOrder].Deleted = 0",
                    (assigned, assignedOutcome, assignedIncome) => {
                        if (assigned.RootOutcomePaymentOrderId == null) return assigned;

                        assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                        assigned.AssignedIncomePaymentOrder = assignedIncome;

                        toReturn.OutcomePaymentOrders.First(o => o.Id.Equals(assigned.RootOutcomePaymentOrderId.Value)).AssignedPaymentOrders.Add(assigned);

                        return assigned;
                    },
                    new { Ids = toReturn.OutcomePaymentOrders.Select(o => o.Id) }
                );

                _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
                    "SELECT * " +
                    "FROM [AssignedPaymentOrder] " +
                    "LEFT JOIN [OutcomePaymentOrder] " +
                    "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
                    "LEFT JOIN [IncomePaymentOrder] " +
                    "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
                    "WHERE [AssignedPaymentOrder].AssignedOutcomePaymentOrderID IN @Ids " +
                    "AND [AssignedPaymentOrder].Deleted = 0",
                    (assigned, assignedOutcome, assignedIncome) => {
                        if (assigned.AssignedOutcomePaymentOrderId == null) return assigned;

                        assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                        assigned.AssignedIncomePaymentOrder = assignedIncome;

                        toReturn.OutcomePaymentOrders.First(o => o.Id.Equals(assigned.AssignedOutcomePaymentOrderId.Value)).RootAssignedPaymentOrder = assigned;

                        return assigned;
                    },
                    new { Ids = toReturn.OutcomePaymentOrders.Select(o => o.Id) }
                );
            }

            toReturn.BeforeRangeTotal = _connection.Query<decimal>(
                    "SELECT " +
                    "ROUND( " +
                    "CASE WHEN [PaymentCurrencyRegister].Created < @From THEN ISNULL([PaymentCurrencyRegister].InitialAmount, 0) ELSE 0 END " +
                    "+ " +
                    "ISNULL(( " +
                    "SELECT SUM([IncomePaymentOrder].Amount) " +
                    "FROM [IncomePaymentOrder] " +
                    "WHERE [IncomePaymentOrder].PaymentRegisterID = [PaymentRegister].ID " +
                    "AND [IncomePaymentOrder].CurrencyID = [Currency].ID " +
                    "AND [IncomePaymentOrder].FromDate < @From " +
                    "), 0) " +
                    "- " +
                    "ISNULL(( " +
                    "SELECT SUM([PaymentRegisterTransfer].Amount) " +
                    "FROM [PaymentRegisterTransfer] " +
                    "WHERE " +
                    "[PaymentRegisterTransfer].FromPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                    "AND [PaymentRegisterTransfer].FromDate < @From " +
                    "), 0) " +
                    "+ " +
                    "ISNULL(( " +
                    "SELECT SUM([PaymentRegisterTransfer].Amount) " +
                    "FROM [PaymentRegisterTransfer] " +
                    "WHERE " +
                    "[PaymentRegisterTransfer].ToPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                    "AND [PaymentRegisterTransfer].FromDate < @From " +
                    "), 0) " +
                    "- " +
                    "ISNULL(( " +
                    "SELECT SUM([PaymentRegisterCurrencyExchange].Amount) " +
                    "FROM [PaymentRegisterCurrencyExchange] " +
                    "WHERE " +
                    "[PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                    "AND [PaymentRegisterCurrencyExchange].FromDate < @From " +
                    "), 0) " +
                    "+ " +
                    "ISNULL(( " +
                    "SELECT SUM([PaymentRegisterCurrencyExchange].Amount) " +
                    "FROM [PaymentRegisterCurrencyExchange] " +
                    "WHERE " +
                    "[PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                    "AND [PaymentRegisterCurrencyExchange].FromDate < @From " +
                    "), 0) " +
                    "- " +
                    "ISNULL(( " +
                    "SELECT SUM([OutcomePaymentOrder].Amount) " +
                    "FROM [OutcomePaymentOrder] " +
                    "WHERE [OutcomePaymentOrder].PaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                    "AND [OutcomePaymentOrder].FromDate < @From " +
                    "), 0) " +
                    ", 2) " +
                    "FROM [PaymentCurrencyRegister] " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                    "LEFT JOIN [PaymentRegister] " +
                    "ON [PaymentRegister].ID = [PaymentCurrencyRegister].ID " +
                    "WHERE [PaymentCurrencyRegister].NetUID = @NetId ",
                    new { NetId = netId, From = from }
                )
                .Single();

            if (toReturn.Created >= from) toReturn.RangeTotal += toReturn.InitialAmount;

            toReturn.RangeTotal = Math.Round(toReturn.BeforeRangeTotal + toReturn.RangeTotal, 2);
        }

        return toReturn;
    }

    public decimal GetPaymentCurrencyRegisterAmountByIdAtSpecifiedDate(DateTime from, long id) {
        return _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "ISNULL([PaymentCurrencyRegister].InitialAmount, 0) " +
                "+ " +
                "ISNULL(( " +
                "SELECT SUM([IncomePaymentOrder].Amount) " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [IncomePaymentOrder].CurrencyID = [Currency].ID " +
                "AND [IncomePaymentOrder].Created < @From " +
                "), 0) " +
                "- " +
                "ISNULL(( " +
                "SELECT SUM([PaymentRegisterTransfer].Amount) " +
                "FROM [PaymentRegisterTransfer] " +
                "WHERE " +
                "[PaymentRegisterTransfer].FromPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                "AND [PaymentRegisterTransfer].IsCanceled = 0 " +
                "AND [PaymentRegisterTransfer].Created < @From " +
                "), 0) " +
                "+ " +
                "ISNULL(( " +
                "SELECT SUM([PaymentRegisterTransfer].Amount) " +
                "FROM [PaymentRegisterTransfer] " +
                "WHERE " +
                "[PaymentRegisterTransfer].ToPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                "AND [PaymentRegisterTransfer].IsCanceled = 0 " +
                "AND [PaymentRegisterTransfer].Created < @From " +
                "), 0) " +
                "- " +
                "ISNULL(( " +
                "SELECT SUM([PaymentRegisterCurrencyExchange].Amount) " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "WHERE " +
                "[PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                "AND [PaymentRegisterCurrencyExchange].IsCanceled = 0 " +
                "AND [PaymentRegisterCurrencyExchange].Created < @From " +
                "), 0) " +
                "+ " +
                "ISNULL(( " +
                "SELECT SUM([PaymentRegisterCurrencyExchange].Amount) " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "WHERE " +
                "[PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                "AND [PaymentRegisterCurrencyExchange].IsCanceled = 0 " +
                "AND [PaymentRegisterCurrencyExchange].Created < @From " +
                "), 0) " +
                "- " +
                "ISNULL(( " +
                "SELECT SUM([OutcomePaymentOrder].Amount) " +
                "FROM [OutcomePaymentOrder] " +
                "WHERE [OutcomePaymentOrder].PaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
                "AND [OutcomePaymentOrder].Created < @From " +
                "), 0) " +
                ", 2) " +
                "FROM [PaymentCurrencyRegister] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].ID " +
                "WHERE [PaymentCurrencyRegister].ID = @CurrencyRegisterId",
                new { CurrencyRegisterId = id, From = from }
            )
            .Single();
    }

    public decimal GetAmountOfAllOperationsAfterDateByIds(DateTime from, long currencyRegisterId, long currencyId, long registerId) {
        return _connection.Query<decimal>(
                "SELECT " +
                "ROUND( " +
                "( " +
                "ISNULL( " +
                "( " +
                "SELECT SUM([PaymentRegisterTransfer].Amount) " +
                "FROM [PaymentRegisterTransfer] " +
                "WHERE [PaymentRegisterTransfer].Created > @From " +
                "AND [PaymentRegisterTransfer].IsCanceled = 0 " +
                "AND [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID = @CurrencyRegisterId " +
                ") " +
                ", 0) " +
                "- " +
                "ISNULL( " +
                "( " +
                "SELECT SUM([PaymentRegisterTransfer].Amount) " +
                "FROM [PaymentRegisterTransfer] " +
                "WHERE [PaymentRegisterTransfer].Created > @From " +
                "AND [PaymentRegisterTransfer].IsCanceled = 0 " +
                "AND [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID = @CurrencyRegisterId " +
                ") " +
                ", 0) " +
                ") " +
                "+ " +
                "( " +
                "ISNULL( " +
                "( " +
                "SELECT SUM([PaymentRegisterCurrencyExchange].Amount) " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "WHERE [PaymentRegisterCurrencyExchange].Created > @From " +
                "AND [PaymentRegisterCurrencyExchange].IsCanceled = 0 " +
                "AND [PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID = @CurrencyRegisterId " +
                ") " +
                ", 0) " +
                "- " +
                "ISNULL( " +
                "( " +
                "SELECT SUM([PaymentRegisterCurrencyExchange].Amount) " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "WHERE [PaymentRegisterCurrencyExchange].Created > @From " +
                "AND [PaymentRegisterCurrencyExchange].IsCanceled = 0 " +
                "AND [PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID = @CurrencyRegisterId " +
                ") " +
                ", 0) " +
                ") " +
                "+ " +
                "ISNULL( " +
                "( " +
                "SELECT SUM([IncomePaymentOrder].Amount) " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].Created > @From " +
                "AND [IncomePaymentOrder].CurrencyID = @CurrencyId " +
                "AND [IncomePaymentOrder].PaymentRegisterID = @RegisterId " +
                ") " +
                ", 0) " +
                "- " +
                "ISNULL( " +
                "( " +
                "SELECT SUM([OutcomePaymentOrder].Amount) " +
                "FROM [OutcomePaymentOrder] " +
                "WHERE [OutcomePaymentOrder].Created > @From " +
                "AND [OutcomePaymentOrder].PaymentCurrencyRegisterID = @CurrencyRegisterId " +
                ") " +
                ", 0) " +
                ", 2)",
                new { CurrencyId = currencyId, RegisterId = registerId, CurrencyRegisterId = currencyRegisterId, From = from }
            )
            .Single();
    }
}