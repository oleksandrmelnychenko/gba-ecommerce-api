using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.Helpers.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class IncomePaymentOrderActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public IncomePaymentOrderActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;

        Receive<AddNewIncomePaymentOrderMessage>(ProcessAddNewIncomePaymentOrderMessage);

        Receive<UpdateIncomePaymentOrderMessage>(ProcessUpdateIncomePaymentOrderMessage);

        Receive<DeleteIncomePaymentOrderByNetIdMessage>(ProcessDeleteIncomePaymentOrderByNetIdMessage);

        Receive<CancelIncomePaymentOrderByNetIdMessage>(ProcessCancelIncomePaymentOrderByNetIdMessage);

        Receive<ChangeClientOnIncomePaymentOrderMessage>(ProcessChangeClientOnIncomePaymentOrderMessage);

        Receive<AddNewIncomePaymentOrderFromTaxFreeMessage>(ProcessAddNewIncomePaymentOrderFromTaxFreeMessage);

        Receive<AddNewIncomePaymentOrderFromSadMessage>(ProcessAddNewIncomePaymentOrderFromSadMessage);
    }

    private void ProcessAddNewIncomePaymentOrderMessage(AddNewIncomePaymentOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.IncomePaymentOrder == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
        } else if (!message.IncomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
        } else if (message.IncomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));
        } else if (message.IncomePaymentOrder.Organization == null && message.IncomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));
        } else if (message.IncomePaymentOrder.Currency == null && message.IncomePaymentOrder.CurrencyId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.CURRENCY_NOT_SPECIFIED));
        } else if (message.IncomePaymentOrder.PaymentRegister == null && message.IncomePaymentOrder.PaymentRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_REGISTER_NOT_SPECIFIED));
        } else if (message.IncomePaymentOrder.PaymentMovementOperation == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));
        } else {
            if (message.IncomePaymentOrder.Organization != null) message.IncomePaymentOrder.OrganizationId = message.IncomePaymentOrder.Organization.Id;
            if (message.IncomePaymentOrder.Currency != null) message.IncomePaymentOrder.CurrencyId = message.IncomePaymentOrder.Currency.Id;
            if (message.IncomePaymentOrder.Client != null) message.IncomePaymentOrder.ClientId = message.IncomePaymentOrder.Client.Id;
            if (message.IncomePaymentOrder.Colleague != null) message.IncomePaymentOrder.ColleagueId = message.IncomePaymentOrder.Colleague.Id;
            if (message.IncomePaymentOrder.ClientAgreement != null) message.IncomePaymentOrder.ClientAgreementId = message.IncomePaymentOrder.ClientAgreement.Id;
            if (message.IncomePaymentOrder.SupplyOrganization != null) message.IncomePaymentOrder.SupplyOrganizationId = message.IncomePaymentOrder.SupplyOrganization.Id;
            if (message.IncomePaymentOrder.FromDate.Year.Equals(1)) message.IncomePaymentOrder.FromDate = DateTime.UtcNow;
            if (message.IncomePaymentOrder.SupplyOrganizationAgreement != null)
                message.IncomePaymentOrder.SupplyOrganizationAgreementId = message.IncomePaymentOrder.SupplyOrganizationAgreement.Id;
            if (message.IncomePaymentOrder.IncomePaymentOrderSales.Any())
                message.IncomePaymentOrder.IncomePaymentOrderSales =
                    message.IncomePaymentOrder.IncomePaymentOrderSales
                        .OrderBy(s => s.Sale?.Created ?? s.ReSale.Created).ToList();

            if (IncomeHasNoAgreement(message.IncomePaymentOrder)) {
                ProcessAddNewIncomePaymentOrderWithoutAgreement(message);

                return;
            }

            IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

            PaymentRegister register = paymentRegisterRepository
                .GetById(
                    message.IncomePaymentOrder.PaymentRegisterId.Equals(0) && message.IncomePaymentOrder.PaymentRegister != null
                        ? message.IncomePaymentOrder.PaymentRegister.Id
                        : message.IncomePaymentOrder.PaymentRegisterId
                );

            if (!register.PaymentCurrencyRegisters.Any(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId))) {
                Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_CURRENCY_REGISTER_NOT_EXISTS));
            } else {
                message.IncomePaymentOrder.PaymentRegisterId = register.Id;

                PaymentCurrencyRegister paymentCurrencyRegister =
                    register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId));

                paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + message.IncomePaymentOrder.Amount, 2);

                _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);
                ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
                IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

                if (message.IncomePaymentOrder.ClientId.HasValue) {
                    IBaseSalePaymentStatusRepository salePaymentStatusRepository = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);
                    IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                    if (string.IsNullOrEmpty(message.IncomePaymentOrder.Number)) {
                        IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister!.Type);

                        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                            message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                        else
                            message.IncomePaymentOrder.Number =
                                message.IncomePaymentOrder.Organization!.Code +
                                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);
                    }

                    message.IncomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                    if (message.IncomePaymentOrder.IncomePaymentOrderSales.Any()) {
                        Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                        PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                            euroCurrency,
                            message.IncomePaymentOrder.Currency,
                            message.IncomePaymentOrder.FromDate,
                            exchangeRateRepository,
                            crossExchangeRateRepository);

                        message.IncomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.IncomePaymentOrder.Amount);

                        AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                            message.IncomePaymentOrder.Amount,
                            message.IncomePaymentOrder.ExchangeRate,
                            message.IncomePaymentOrder.ClientAgreement.Agreement.Currency);

                        message.IncomePaymentOrder.AgreementExchangedAmount = agreementConversionResult.InAgreementCurrencyAmount;
                        message.IncomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;
                    } else {
                        ClientAgreement clientAgreement = message.IncomePaymentOrder.ClientAgreement == null || message.IncomePaymentOrder.ClientAgreement.IsNew()
                            ? clientAgreementRepository.GetActiveByClientId(message.IncomePaymentOrder.ClientId.Value)
                            : clientAgreementRepository.GetByIdWithAgreementAndOrganization(message.IncomePaymentOrder.ClientAgreement.Id);

                        message.IncomePaymentOrder.ClientAgreementId = clientAgreement.Id;

                        if (message.IncomePaymentOrder.Currency == null)
                            message.IncomePaymentOrder.Currency =
                                _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetById(message.IncomePaymentOrder.CurrencyId);

                        Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                        PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                            euroCurrency,
                            message.IncomePaymentOrder.Currency,
                            message.IncomePaymentOrder.FromDate,
                            exchangeRateRepository,
                            crossExchangeRateRepository);

                        message.IncomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.IncomePaymentOrder.Amount);

                        AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                            message.IncomePaymentOrder.Amount,
                            message.IncomePaymentOrder.ExchangeRate,
                            message.IncomePaymentOrder.ClientAgreement.Agreement.Currency);

                        message.IncomePaymentOrder.AgreementExchangedAmount = agreementConversionResult.InAgreementCurrencyAmount;
                        message.IncomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;
                        message.IncomePaymentOrder.AgreementEuroExchangeRate =
                            paymentOrdersCurrencyConvertor.GetAgreementCurrencyToEuroExchangeRate(message.IncomePaymentOrder.ClientAgreement.Agreement.Currency);

                        clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount + message.IncomePaymentOrder.EuroAmount, 4);

                        _clientRepositoriesFactory
                            .NewClientBalanceMovementRepository(connection)
                            .AddInMovement(
                                new ClientBalanceMovement {
                                    ClientAgreementId = clientAgreement.Id,
                                    Amount = message.IncomePaymentOrder.EuroAmount,
                                    ExchangeRateAmount = agreementConversionResult.ExchangeRate
                                });

                        clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                    }

                    message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

                    if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
                        (!message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                         message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                       ) {
                        if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                                message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                        message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
                    }

                    if (message.IncomePaymentOrder.IncomePaymentOrderSales.Any()) {
                        IEnumerable<long> saleIds = message
                            .IncomePaymentOrder
                            .IncomePaymentOrderSales
                            .Where(s => s.Sale != null || s.ReSale != null)
                            .Select(s => s.Sale?.Id ?? s.ReSale?.Id ?? 0)
                            .Where(x => !x.Equals(0));

                        List<ClientInDebt> clientInDebts = _clientRepositoriesFactory.NewClientInDebtRepository(connection).GetAllBySaleIds(saleIds);

                        if (!message.Auto) {
                            List<ClientInDebt> orderedClientInDebts =
                                message
                                    .IncomePaymentOrder
                                    .IncomePaymentOrderSales
                                    .Select(junction => {
                                            if (junction.Sale != null) {
                                                junction.SaleId = junction.Sale.Id;
                                                return clientInDebts
                                                    .First(d => d.SaleId.Equals(!junction.SaleId.Value.Equals(0) ? junction.SaleId : junction.Sale.Id));
                                            }

                                            junction.ReSaleId = junction.ReSale.Id;
                                            return clientInDebts
                                                .First(d => d.ReSaleId.Equals(!junction.ReSaleId.Value.Equals(0) ? junction.ReSaleId : junction.ReSale.Id));
                                        }
                                    ).ToList();

                            foreach (ClientInDebt clientInDebt in clientInDebts.Where(d => !orderedClientInDebts.Any(o => o.Id.Equals(d.Id))))
                                orderedClientInDebts.Add(clientInDebt);

                            DecreaseDebts(
                                message.IncomePaymentOrder,
                                clientInDebts.OrderBy(x => x.Created).ToList(),
                                _saleRepositoriesFactory.NewDebtRepository(connection),
                                _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                                _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection),
                                incomePaymentOrderRepository,
                                salePaymentStatusRepository,
                                _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection),
                                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                                saleRepository,
                                _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection),
                                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection),
                                _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection)
                            );
                        } else {
                            DecreaseDebts(
                                message.IncomePaymentOrder,
                                clientInDebts.OrderBy(x => x.Created).ToList(),
                                _saleRepositoriesFactory.NewDebtRepository(connection),
                                _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                                _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection),
                                incomePaymentOrderRepository,
                                salePaymentStatusRepository,
                                _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection),
                                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                                saleRepository,
                                _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection),
                                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection),
                                _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection)
                            );
                        }

                        UnlockNextUnpaidInvoiceWithEnoughAgreementDebtDays(
                            message.IncomePaymentOrder,
                            saleRepository,
                            _clientRepositoriesFactory.NewClientInDebtRepository(connection));
                    }
                } else if (message.IncomePaymentOrder.SupplyOrganizationId.HasValue) {
                    ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                        _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

                    SupplyOrganizationAgreement agreement =
                        supplyOrganizationAgreementRepository.GetById(message.IncomePaymentOrder.SupplyOrganizationAgreement.Id);

                    message.IncomePaymentOrder.SupplyOrganizationAgreementId = agreement.Id;

                    if (message.IncomePaymentOrder.Currency == null)
                        message.IncomePaymentOrder.Currency =
                            _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetById(message.IncomePaymentOrder.CurrencyId);

                    Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                        euroCurrency,
                        message.IncomePaymentOrder.Currency,
                        message.IncomePaymentOrder.FromDate,
                        exchangeRateRepository,
                        crossExchangeRateRepository);

                    message.IncomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.IncomePaymentOrder.Amount);

                    AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                        message.IncomePaymentOrder.Amount,
                        message.IncomePaymentOrder.ExchangeRate,
                        message.IncomePaymentOrder.SupplyOrganizationAgreement.Currency);

                    message.IncomePaymentOrder.AgreementExchangedAmount = agreementConversionResult.InAgreementCurrencyAmount;
                    message.IncomePaymentOrder.ExchangeRate = agreementConversionResult.ExchangeRate;
                    message.IncomePaymentOrder.AgreementEuroExchangeRate = paymentOrdersCurrencyConvertor.GetAgreementCurrencyToEuroExchangeRate(
                        message.IncomePaymentOrder.SupplyOrganizationAgreement.Currency);

                    agreement.CurrentEuroAmount = Math.Round(agreement.CurrentEuroAmount + message.IncomePaymentOrder.EuroAmount, 2);
                    agreement.CurrentAmount = Math.Round(agreement.CurrentAmount + message.IncomePaymentOrder.AgreementExchangedAmount, 2, MidpointRounding.AwayFromZero);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(agreement);

                    if (string.IsNullOrEmpty(message.IncomePaymentOrder.Number)) {
                        IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister!.Type);

                        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                            message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                        else
                            message.IncomePaymentOrder.Number =
                                message.IncomePaymentOrder.Organization!.Code +
                                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);
                    }

                    message.IncomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                    message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

                    if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
                        (!message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                         message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                       ) {
                        if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                                message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                        message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
                    }
                } else {
                    if (string.IsNullOrEmpty(message.IncomePaymentOrder.Number)) {
                        IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                            message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                        else
                            message.IncomePaymentOrder.Number =
                                message.IncomePaymentOrder.Organization!.Code +
                                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);
                    }

                    message.IncomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                    if (message.IncomePaymentOrder.Currency == null)
                        message.IncomePaymentOrder.Currency =
                            _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetById(message.IncomePaymentOrder.CurrencyId);

                    Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                        euroCurrency,
                        message.IncomePaymentOrder.Currency,
                        message.IncomePaymentOrder.FromDate,
                        exchangeRateRepository,
                        crossExchangeRateRepository);

                    message.IncomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.IncomePaymentOrder.Amount);
                    message.IncomePaymentOrder.AgreementEuroExchangeRate = decimal.One;

                    message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

                    if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
                        !message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id.Equals(0)
                       ) {
                        message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                        message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
                    }
                }

                Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomePaymentOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
            }
        }
    }

    private static bool IncomeHasNoAgreement(IncomePaymentOrder incomePaymentOrder) {
        return incomePaymentOrder.Client != null
               && incomePaymentOrder.ClientAgreement == null
               || incomePaymentOrder.SupplyOrganization != null
               && incomePaymentOrder.SupplyOrganizationAgreement == null;
    }

    private void ProcessUpdateIncomePaymentOrderMessage(UpdateIncomePaymentOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.IncomePaymentOrder == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
        } else if (message.IncomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
        } else if (message.IncomePaymentOrder.Amount < decimal.Zero) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));
        } else {
            if (message.IncomePaymentOrder.Organization != null) message.IncomePaymentOrder.OrganizationId = message.IncomePaymentOrder.Organization.Id;
            if (message.IncomePaymentOrder.FromDate.Year.Equals(1)) message.IncomePaymentOrder.FromDate = DateTime.UtcNow;
            if (message.IncomePaymentOrder.PaymentMovementOperation != null) {
                if (message.IncomePaymentOrder.PaymentMovementOperation.IsNew()) {
                    if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                        message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                    message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
                } else {
                    if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                        message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Update(message.IncomePaymentOrder.PaymentMovementOperation);
                }
            }

            IIncomePaymentOrderRepository incomeCashOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);
            IOutcomePaymentOrderRepository outcomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(connection);

            IncomePaymentOrder incomeFromDb = incomeCashOrderRepository.GetByIdWithCalculatedAmount(message.IncomePaymentOrder.Id);

            if (incomeFromDb != null) {
                IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

                long currentUserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                PaymentRegister register = paymentRegisterRepository
                    .GetById(
                        message.IncomePaymentOrder.PaymentRegisterId.Equals(0)
                            ? message.IncomePaymentOrder.PaymentRegister.Id
                            : message.IncomePaymentOrder.PaymentRegisterId
                    );

                PaymentCurrencyRegister paymentCurrencyRegister =
                    register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId));

                if (incomeFromDb.RootAssignedPaymentOrder != null) {
                    if (incomeFromDb.RootAssignedPaymentOrder.RootIncomePaymentOrderId.HasValue)
                        incomeFromDb = incomeCashOrderRepository.GetByIdWithCalculatedAmount(incomeFromDb.RootAssignedPaymentOrder.RootIncomePaymentOrderId.Value);
                    else
                        Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.NOT_MODIFY_INCOME));
                }

                if (!incomeFromDb.Amount.Equals(message.IncomePaymentOrder.Amount)) {
                    if (message.IncomePaymentOrder.ClientId.HasValue) {
                        IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
                        IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
                        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

                        if (message.IncomePaymentOrder.IncomePaymentOrderSales.Any()) {
                            if (incomeFromDb.Amount > message.IncomePaymentOrder.Amount) {
                                decimal amountDifference = Math.Round(incomeFromDb.Amount - message.IncomePaymentOrder.Amount, 2);

                                if (amountDifference < paymentCurrencyRegister.Amount) {
                                    IEnumerable<long> saleIds = message
                                        .IncomePaymentOrder
                                        .IncomePaymentOrderSales
                                        .Where(s => s.Sale != null || s.ReSale != null)
                                        .Select(s => s.Sale?.Id ?? s.ReSale?.Id ?? 0)
                                        .Where(x => !x.Equals(0));

                                    List<ClientInDebt> clientInDebts = clientInDebtRepository.GetAllBySaleIdsWithDeleted(saleIds);

                                    if (incomeFromDb.Amount <= amountDifference) {
                                        amountDifference = incomeFromDb.Amount;

                                        clientInDebtRepository.RestoreAllByIds(clientInDebts.Select(d => d.Id));
                                    } else {
                                        decimal currentAmountDifference = amountDifference;

                                        foreach (IncomePaymentOrderSale junction in message.IncomePaymentOrder.IncomePaymentOrderSales)
                                            if (junction.Amount < currentAmountDifference) {
                                                ClientInDebt clientInDebt = null;

                                                if (junction.SaleId.HasValue)
                                                    clientInDebt = clientInDebts.First(d => d.SaleId.Equals(junction.SaleId));
                                                else if (junction.ReSaleId.HasValue) clientInDebt = clientInDebts.First(d => d.ReSaleId.Equals(junction.ReSaleId));

                                                if (clientInDebt != null) {
                                                    clientInDebtRepository.Restore(clientInDebt.Id);

                                                    clientInDebt.Debt.Total = message.IncomePaymentOrder.Amount;

                                                    debtRepository.Update(clientInDebt.Debt);

                                                    currentAmountDifference = Math.Round(currentAmountDifference - message.IncomePaymentOrder.Amount, 2);
                                                }
                                            } else {
                                                ClientInDebt clientInDebt = null;

                                                if (junction.SaleId.HasValue)
                                                    clientInDebt = clientInDebts.First(d => d.SaleId.Equals(junction.SaleId));
                                                else if (junction.ReSaleId.HasValue) clientInDebt = clientInDebts.First(d => d.ReSaleId.Equals(junction.ReSaleId));

                                                if (clientInDebt != null) {
                                                    if (clientInDebt.Deleted) {
                                                        clientInDebtRepository.Restore(clientInDebt.Id);

                                                        clientInDebt.Debt.Total = currentAmountDifference;

                                                        debtRepository.Update(clientInDebt.Debt);
                                                    } else {
                                                        clientInDebtRepository.Restore(clientInDebt.Id);

                                                        clientInDebt.Debt.Total += currentAmountDifference;

                                                        debtRepository.Update(clientInDebt.Debt);
                                                    }
                                                }

                                                break;
                                            }
                                    }

                                    paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - amountDifference, 2);

                                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                                    OutcomePaymentOrder assignedOutcome = new() {
                                        Amount = amountDifference,
                                        UserId = currentUserId,
                                        FromDate = DateTime.UtcNow,
                                        OrganizationId = message.IncomePaymentOrder.OrganizationId,
                                        ColleagueId = message.IncomePaymentOrder.ColleagueId,
                                        PaymentCurrencyRegisterId = paymentCurrencyRegister.Id
                                    };

                                    OutcomePaymentOrder lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                                    if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                        assignedOutcome.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                                    else
                                        assignedOutcome.Number =
                                            message.IncomePaymentOrder.Organization!.Code +
                                            string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                                    assignedOutcome.Id = outcomePaymentOrderRepository.Add(assignedOutcome);

                                    if (message.IncomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                        _paymentOrderRepositoriesFactory
                                            .NewPaymentMovementOperationRepository(connection)
                                            .Add(new PaymentMovementOperation {
                                                OutcomePaymentOrderId = assignedOutcome.Id,
                                                PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                            });

                                    _paymentOrderRepositoriesFactory
                                        .NewAssignedPaymentOrderRepository(connection)
                                        .Add(new AssignedPaymentOrder {
                                            RootIncomePaymentOrderId = incomeFromDb.Id,
                                            AssignedOutcomePaymentOrderId = assignedOutcome.Id
                                        });

                                    message.IncomePaymentOrder.UserId = currentUserId;

                                    incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                                    Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                                } else {
                                    Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
                                }
                            } else {
                                IBaseSalePaymentStatusRepository salePaymentStatusRepository = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);
                                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                                decimal amountDifference = Math.Round(message.IncomePaymentOrder.Amount - incomeFromDb.Amount, 2);

                                IEnumerable<long> saleIds = message
                                    .IncomePaymentOrder
                                    .IncomePaymentOrderSales
                                    .Where(s => s.Sale != null || s.ReSale != null)
                                    .Select(s => s.Sale?.Id ?? s.ReSale?.Id ?? 0)
                                    .Where(x => !x.Equals(0));

                                List<ClientInDebt> clientInDebts = _clientRepositoriesFactory.NewClientInDebtRepository(connection).GetAllBySaleIds(saleIds);

                                decimal debtsTotal = clientInDebts.Sum(d => d.Debt.Total);

                                if (debtsTotal >= amountDifference) {
                                    List<IncomePaymentOrderSale> junctions = new();
                                    List<long> fullyPaidDebts = new();

                                    decimal currentAmount = amountDifference;

                                    foreach (ClientInDebt debt in clientInDebts)
                                        if (currentAmount >= 0) {
                                            IncomePaymentOrderSale junction = message
                                                .IncomePaymentOrder
                                                .IncomePaymentOrderSales
                                                .FirstOrDefault(s => s.SaleId.Equals(debt.SaleId) || s.Sale != null && s.Sale.Id.Equals(debt.SaleId)) ?? message
                                                .IncomePaymentOrder
                                                .IncomePaymentOrderSales
                                                .FirstOrDefault(s => s.ReSaleId.Equals(debt.ReSaleId) || s.ReSale != null && s.ReSale.Id.Equals(debt.ReSaleId));

                                            decimal debtAmount = Convert.ToDecimal(Math.Round(debt.Debt.Total, 2));

                                            junction.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                                            junction.SaleId = debt.SaleId ?? debt.ReSaleId.Value;

                                            junctions.Add(junction);

                                            if (debtAmount > currentAmount) {
                                                if (debt.Sale != null)
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid,
                                                        debt.Sale.BaseSalePaymentStatusId);
                                                else if (debt.ReSale != null)
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid,
                                                        debt.ReSale.BaseSalePaymentStatusId);

                                                debt.Debt.Total = Math.Round(debt.Debt.Total - currentAmount, 4);

                                                _saleRepositoriesFactory.NewDebtRepository(connection).Update(debt.Debt);
                                            } else {
                                                if (debt.Sale != null) {
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid,
                                                        debt.Sale.BaseSalePaymentStatusId);
                                                    saleRepository.UpdateIsAcceptedToPacking(debt.Sale.Id, true);

                                                    saleRepository.UnlockSaleById(debt.Sale.Id);
                                                } else if (debt.ReSale != null) {
                                                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid,
                                                        debt.ReSale.BaseSalePaymentStatusId);
                                                }

                                                fullyPaidDebts.Add(debt.Id);
                                            }

                                            currentAmount -= Math.Round(currentAmount - debtAmount, 2);
                                        } else {
                                            break;
                                        }

                                    _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection).Add(junctions);

                                    if (fullyPaidDebts.Any())
                                        _clientRepositoriesFactory
                                            .NewClientInDebtRepository(connection)
                                            .RemoveAllByIds(fullyPaidDebts);
                                } else {
                                    foreach (ClientInDebt debt in clientInDebts)
                                        if (debt.Sale != null) {
                                            salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, debt.Sale.BaseSalePaymentStatusId);
                                            saleRepository.UpdateIsAcceptedToPacking(debt.Sale.Id, true);

                                            saleRepository.UnlockSaleById(debt.Sale.Id);
                                        } else if (debt.ReSale != null) {
                                            salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, debt.ReSale.BaseSalePaymentStatusId);
                                        }

                                    decimal overpaidAmount = amountDifference - Convert.ToDecimal(debtsTotal);

                                    ClientAgreement clientAgreement =
                                        message.IncomePaymentOrder.ClientAgreement == null || message.IncomePaymentOrder.ClientAgreement.IsNew()
                                            ? clientAgreementRepository.GetActiveByClientId(message.IncomePaymentOrder.ClientId.Value)
                                            : clientAgreementRepository.GetByIdWithoutIncludes(message.IncomePaymentOrder.ClientAgreement.Id);

                                    clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount + overpaidAmount, 4);

                                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                    _clientRepositoriesFactory
                                        .NewClientBalanceMovementRepository(connection)
                                        .AddInMovement(
                                            new ClientBalanceMovement {
                                                ClientAgreementId = clientAgreement.Id,
                                                Amount = overpaidAmount,
                                                ExchangeRateAmount = 1m
                                            }
                                        );
                                }

                                paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + amountDifference, 2);

                                _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                                IncomePaymentOrder assignedIncome = new() {
                                    Amount = amountDifference,
                                    UserId = currentUserId,
                                    FromDate = DateTime.UtcNow,
                                    OrganizationId = message.IncomePaymentOrder.OrganizationId,
                                    PaymentRegisterId = incomeFromDb.PaymentRegisterId,
                                    ColleagueId = message.IncomePaymentOrder.ColleagueId,
                                    CurrencyId = incomeFromDb.CurrencyId
                                };

                                IncomePaymentOrder lastRecord = incomeCashOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                                if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                    assignedIncome.Number = string.Format("{0:D6}", 1);
                                else
                                    assignedIncome.Number = string.Format("{0:D6}", Convert.ToInt32(lastRecord.Number) + 1);

                                assignedIncome.Id = incomeCashOrderRepository.Add(assignedIncome);

                                if (message.IncomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                    _paymentOrderRepositoriesFactory
                                        .NewPaymentMovementOperationRepository(connection)
                                        .Add(new PaymentMovementOperation {
                                            IncomePaymentOrderId = assignedIncome.Id,
                                            PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                        });

                                _paymentOrderRepositoriesFactory
                                    .NewAssignedPaymentOrderRepository(connection)
                                    .Add(new AssignedPaymentOrder {
                                        RootIncomePaymentOrderId = incomeFromDb.Id,
                                        AssignedIncomePaymentOrderId = assignedIncome.Id
                                    });

                                message.IncomePaymentOrder.UserId = currentUserId;

                                incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                                Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                            }
                        } else {
                            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                            ClientAgreement clientAgreement = message.IncomePaymentOrder.ClientAgreement == null || message.IncomePaymentOrder.ClientAgreement.IsNew()
                                ? clientAgreementRepository.GetActiveByClientId(message.IncomePaymentOrder.ClientId.Value)
                                : clientAgreementRepository.GetByIdWithoutIncludes(message.IncomePaymentOrder.ClientAgreement.Id);

                            if (incomeFromDb.Amount > message.IncomePaymentOrder.Amount) {
                                decimal amountDifference = Math.Round(incomeFromDb.Amount - message.IncomePaymentOrder.Amount, 4);

                                if (clientAgreement.CurrentAmount < amountDifference) {
                                    Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.LESS_BALANCE_CLIENT));
                                } else {
                                    clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount - amountDifference, 4);

                                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                    _clientRepositoriesFactory
                                        .NewClientBalanceMovementRepository(connection)
                                        .AddOutMovement(
                                            new ClientBalanceMovement {
                                                ClientAgreementId = clientAgreement.Id,
                                                Amount = amountDifference,
                                                ExchangeRateAmount = 1m
                                            }
                                        );

                                    paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - amountDifference, 2);

                                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                                    OutcomePaymentOrder assignedOutcome = new() {
                                        Amount = amountDifference,
                                        UserId = currentUserId,
                                        FromDate = DateTime.UtcNow,
                                        OrganizationId = message.IncomePaymentOrder.OrganizationId,
                                        ColleagueId = message.IncomePaymentOrder.ColleagueId,
                                        PaymentCurrencyRegisterId = paymentCurrencyRegister.Id
                                    };

                                    OutcomePaymentOrder lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                                    if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                        assignedOutcome.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                                    else
                                        assignedOutcome.Number =
                                            message.IncomePaymentOrder.Organization!.Code +
                                            string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                                    assignedOutcome.Id = outcomePaymentOrderRepository.Add(assignedOutcome);

                                    if (message.IncomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                        _paymentOrderRepositoriesFactory
                                            .NewPaymentMovementOperationRepository(connection)
                                            .Add(new PaymentMovementOperation {
                                                OutcomePaymentOrderId = assignedOutcome.Id,
                                                PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                            });

                                    _paymentOrderRepositoriesFactory
                                        .NewAssignedPaymentOrderRepository(connection)
                                        .Add(new AssignedPaymentOrder {
                                            RootIncomePaymentOrderId = incomeFromDb.Id,
                                            AssignedOutcomePaymentOrderId = assignedOutcome.Id
                                        });

                                    message.IncomePaymentOrder.UserId = currentUserId;

                                    incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                                    Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                                }
                            } else {
                                decimal amountDifference = Math.Round(message.IncomePaymentOrder.Amount - incomeFromDb.Amount, 4);

                                clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount - amountDifference, 4);

                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                _clientRepositoriesFactory
                                    .NewClientBalanceMovementRepository(connection)
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = amountDifference,
                                            ExchangeRateAmount = 1m
                                        }
                                    );

                                paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + amountDifference, 2);

                                _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                                IncomePaymentOrder assignedIncome = new() {
                                    Amount = amountDifference,
                                    UserId = currentUserId,
                                    FromDate = DateTime.UtcNow,
                                    OrganizationId = message.IncomePaymentOrder.OrganizationId,
                                    PaymentRegisterId = incomeFromDb.PaymentRegisterId,
                                    ColleagueId = message.IncomePaymentOrder.ColleagueId,
                                    CurrencyId = incomeFromDb.CurrencyId
                                };

                                IncomePaymentOrder lastRecord = incomeCashOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                                if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                    assignedIncome.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                                else
                                    assignedIncome.Number =
                                        message.IncomePaymentOrder.Organization!.Code +
                                        string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                                assignedIncome.Id = incomeCashOrderRepository.Add(assignedIncome);

                                if (message.IncomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                    _paymentOrderRepositoriesFactory
                                        .NewPaymentMovementOperationRepository(connection)
                                        .Add(new PaymentMovementOperation {
                                            IncomePaymentOrderId = assignedIncome.Id,
                                            PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                        });

                                _paymentOrderRepositoriesFactory
                                    .NewAssignedPaymentOrderRepository(connection)
                                    .Add(new AssignedPaymentOrder {
                                        RootIncomePaymentOrderId = incomeFromDb.Id,
                                        AssignedIncomePaymentOrderId = assignedIncome.Id
                                    });

                                message.IncomePaymentOrder.UserId = currentUserId;

                                incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                                Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                            }
                        }
                    } else {
                        if (incomeFromDb.Amount > message.IncomePaymentOrder.Amount) {
                            decimal amountDifference = Math.Round(incomeFromDb.Amount - message.IncomePaymentOrder.Amount, 2);

                            if (amountDifference < paymentCurrencyRegister.Amount) {
                                paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - amountDifference, 2);

                                _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                                OutcomePaymentOrder assignedOutcome = new() {
                                    Amount = amountDifference,
                                    UserId = currentUserId,
                                    FromDate = DateTime.UtcNow,
                                    OrganizationId = message.IncomePaymentOrder.OrganizationId,
                                    ColleagueId = message.IncomePaymentOrder.ColleagueId,
                                    PaymentCurrencyRegisterId = paymentCurrencyRegister.Id
                                };

                                OutcomePaymentOrder lastRecord = outcomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                                if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                    assignedOutcome.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                                else
                                    assignedOutcome.Number =
                                        message.IncomePaymentOrder.Organization!.Code +
                                        string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                                assignedOutcome.Id = outcomePaymentOrderRepository.Add(assignedOutcome);

                                if (message.IncomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                    _paymentOrderRepositoriesFactory
                                        .NewPaymentMovementOperationRepository(connection)
                                        .Add(new PaymentMovementOperation {
                                            OutcomePaymentOrderId = assignedOutcome.Id,
                                            PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                        });

                                _paymentOrderRepositoriesFactory
                                    .NewAssignedPaymentOrderRepository(connection)
                                    .Add(new AssignedPaymentOrder {
                                        RootIncomePaymentOrderId = incomeFromDb.Id,
                                        AssignedOutcomePaymentOrderId = assignedOutcome.Id
                                    });

                                message.IncomePaymentOrder.UserId = currentUserId;

                                incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                                Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                            } else {
                                Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INSUFFICIENT_OF_FUNDS));
                            }
                        } else {
                            decimal amountDifference = Math.Round(message.IncomePaymentOrder.Amount - incomeFromDb.Amount, 2);

                            paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + amountDifference, 2);

                            _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

                            IncomePaymentOrder assignedIncome = new() {
                                Amount = amountDifference,
                                UserId = currentUserId,
                                FromDate = DateTime.UtcNow,
                                OrganizationId = message.IncomePaymentOrder.OrganizationId,
                                PaymentRegisterId = incomeFromDb.PaymentRegisterId,
                                ColleagueId = message.IncomePaymentOrder.ColleagueId,
                                CurrencyId = incomeFromDb.CurrencyId
                            };

                            IncomePaymentOrder lastRecord = incomeCashOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister.Type);

                            if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                assignedIncome.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                            // assignedIncome.Number = string.Format("{0:D6}", 1);
                            else
                                assignedIncome.Number =
                                    message.IncomePaymentOrder.Organization!.Code +
                                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);
                            // assignedIncome.Number = string.Format("{0:D6}", Convert.ToInt32(lastRecord.Number) + 1);

                            assignedIncome.Id = incomeCashOrderRepository.Add(assignedIncome);

                            if (message.IncomePaymentOrder?.PaymentMovementOperation?.PaymentMovementId != null)
                                _paymentOrderRepositoriesFactory
                                    .NewPaymentMovementOperationRepository(connection)
                                    .Add(new PaymentMovementOperation {
                                        IncomePaymentOrderId = assignedIncome.Id,
                                        PaymentMovementId = message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId
                                    });

                            _paymentOrderRepositoriesFactory
                                .NewAssignedPaymentOrderRepository(connection)
                                .Add(new AssignedPaymentOrder {
                                    RootIncomePaymentOrderId = incomeFromDb.Id,
                                    AssignedIncomePaymentOrderId = assignedIncome.Id
                                });

                            message.IncomePaymentOrder.UserId = currentUserId;

                            incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                            Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                        }
                    }
                } else {
                    message.IncomePaymentOrder.UserId = currentUserId;

                    incomeCashOrderRepository.Update(message.IncomePaymentOrder);

                    Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomeCashOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
                }
            } else {
                Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCOME_PAYMENT_ORDERS_NOT_EXIST));
            }
        }
    }

    private void ProcessDeleteIncomePaymentOrderByNetIdMessage(DeleteIncomePaymentOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);

        IncomePaymentOrder incomeFromDb = incomePaymentOrderRepository.GetByNetId(message.NetId);

        if (incomeFromDb == null) return;

        if (incomeFromDb.RootAssignedPaymentOrder != null) {
            _paymentOrderRepositoriesFactory.NewAssignedPaymentOrderRepository(connection).Remove(incomeFromDb.RootAssignedPaymentOrder.Id);

            if (incomeFromDb.RootAssignedPaymentOrder.RootOutcomePaymentOrder != null)
                _paymentOrderRepositoriesFactory
                    .NewOutcomePaymentOrderRepository(connection)
                    .SetIsUnderReportDoneById(
                        incomeFromDb.RootAssignedPaymentOrder.RootOutcomePaymentOrder.Id,
                        false
                    );
        }

        incomePaymentOrderRepository.Remove(message.NetId);
    }

    private void ProcessCancelIncomePaymentOrderByNetIdMessage(CancelIncomePaymentOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);

        IncomePaymentOrder incomeFromDb = incomePaymentOrderRepository.GetByNetIdReversed(message.NetId);

        if (incomeFromDb == null) {
            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    false,
                    IncomePaymentOrderResourceNames.INCOME_PAYMENT_ORDERS_NOT_EXIST,
                    null,
                    string.Empty
                )
            );

            return;
        }

        if (incomeFromDb.IsCanceled) {
            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    false,
                    IncomePaymentOrderResourceNames.INCOME_ALREADY_CANCELED,
                    incomeFromDb,
                    string.Empty
                )
            );

            return;
        }

        if (!incomeFromDb.Created.Year.Equals(DateTime.Now.Year) || !incomeFromDb.Created.Day.Equals(DateTime.Now.Day) ||
            !incomeFromDb.Created.Month.Equals(DateTime.Now.Month)) {
            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    false,
                    IncomePaymentOrderResourceNames.CANCEL_OLD_INCOME_NOT_ALLOWED,
                    incomeFromDb,
                    string.Empty
                )
            );

            return;
        }

        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        PaymentRegister register = paymentRegisterRepository.GetById(incomeFromDb.PaymentRegisterId);

        PaymentCurrencyRegister paymentCurrencyRegister = register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(incomeFromDb.CurrencyId));

        if (paymentCurrencyRegister.Amount < incomeFromDb.Amount) {
            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    false,
                    IncomePaymentOrderResourceNames.CANCEL_FAILED_INSUFFICIENT_OF_FUNDS,
                    incomeFromDb,
                    string.Empty
                )
            );

            return;
        }

        if (!incomeFromDb.ClientId.HasValue ||
            incomeFromDb.ClientId.HasValue && !incomeFromDb.ClientAgreementId.HasValue) {
            paymentCurrencyRegister.Amount = decimal.Round(paymentCurrencyRegister.Amount - incomeFromDb.Amount, 2, MidpointRounding.AwayFromZero);

            _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

            incomeFromDb.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            incomeFromDb.IsCanceled = true;

            if (incomeFromDb.RootAssignedPaymentOrder != null) {
                _paymentOrderRepositoriesFactory.NewAssignedPaymentOrderRepository(connection).Remove(incomeFromDb.RootAssignedPaymentOrder.Id);

                if (incomeFromDb.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder != null)
                    _paymentOrderRepositoriesFactory
                        .NewOutcomePaymentOrderRepository(connection)
                        .SetIsUnderReportDoneById(
                            incomeFromDb.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.Id,
                            true
                        );
            }

            incomePaymentOrderRepository.Update(incomeFromDb);

            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    true,
                    IncomePaymentOrderResourceNames.INCOME_CANCELED,
                    incomePaymentOrderRepository.GetByNetId(message.NetId),
                    string.Empty
                )
            );

            return;
        }

        if (incomeFromDb.IncomePaymentOrderSales.Any()) {
            IncomePaymentOrder lastForSalesByClient = incomePaymentOrderRepository.GetLastIncomeForSalesByClientId(incomeFromDb.ClientId.Value);

            if (lastForSalesByClient.Id > incomeFromDb.Id) {
                Sender.Tell(
                    new Tuple<bool, string, IncomePaymentOrder, string>(
                        false,
                        IncomePaymentOrderResourceNames.FIRST_CANCEL_INCOME_WITH_NUMBER,
                        incomeFromDb,
                        lastForSalesByClient.Number
                    )
                );

                return;
            }

            IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
            IBaseSalePaymentStatusRepository salePaymentStatusRepository =
                _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);
            IIncomePaymentOrderSaleRepository incomePaymentOrderSaleRepository =
                _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection);

            IEnumerable<long> saleIds =
                incomeFromDb.IncomePaymentOrderSales
                    .Select(j => j.SaleId ?? j.ReSaleId ?? 0)
                    .Where(x => !x.Equals(0));

            List<ClientInDebt> clientInDebts = clientInDebtRepository.GetAllBySaleIdsWithDeleted(saleIds);

            ResetDebtsOnIncomePaymentOrderSales(
                _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                _clientRepositoriesFactory.NewClientCashFlowRepository(connection),
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection),
                incomePaymentOrderRepository,
                clientInDebtRepository,
                salePaymentStatusRepository,
                incomePaymentOrderSaleRepository,
                debtRepository,
                saleRepository,
                _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection),
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection),
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists(),
                incomeFromDb,
                clientInDebts.OrderBy(x => x.Created).ToList()
            );

            incomePaymentOrderSaleRepository.RemoveAllByIds(incomeFromDb.IncomePaymentOrderSales.Select(j => j.Id));

            paymentCurrencyRegister.Amount =
                decimal.Round(
                    paymentCurrencyRegister.Amount - incomeFromDb.Amount,
                    2,
                    MidpointRounding.AwayFromZero
                );

            _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

            incomeFromDb.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            incomeFromDb.IsCanceled = true;

            if (incomeFromDb.RootAssignedPaymentOrder != null) {
                _paymentOrderRepositoriesFactory.NewAssignedPaymentOrderRepository(connection).Remove(incomeFromDb.RootAssignedPaymentOrder.Id);

                if (incomeFromDb.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder != null)
                    _paymentOrderRepositoriesFactory
                        .NewOutcomePaymentOrderRepository(connection)
                        .SetIsUnderReportDoneById(
                            incomeFromDb.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.Id,
                            true
                        );
            }

            incomePaymentOrderRepository.Update(incomeFromDb);

            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    true,
                    IncomePaymentOrderResourceNames.INCOME_CANCELED,
                    incomePaymentOrderRepository.GetByNetId(message.NetId),
                    string.Empty
                )
            );
        } else {
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
            IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
            IExchangeRateRepository exchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            ICrossExchangeRateRepository crossExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

            ClientAgreement clientAgreement = incomeFromDb.ClientAgreement == null || incomeFromDb.ClientAgreement.IsNew()
                ? clientAgreementRepository.GetActiveByClientId(incomeFromDb.ClientId.Value)
                : clientAgreementRepository.GetByIdWithAgreementAndOrganization(incomeFromDb.ClientAgreement.Id);

            ResetDebtOnToBalancePayment(
                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                _saleRepositoriesFactory.NewSaleRepository(connection),
                _reSaleRepositoriesFactory.NewReSaleRepository(connection),
                _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection),
                clientAgreementRepository,
                clientInDebtRepository,
                debtRepository,
                exchangeRateRepository,
                crossExchangeRateRepository,
                incomeFromDb,
                _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists(),
                clientAgreement
            );

            paymentCurrencyRegister.Amount = decimal.Round(paymentCurrencyRegister.Amount - incomeFromDb.Amount, 2, MidpointRounding.AwayFromZero);

            _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

            incomeFromDb.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            incomeFromDb.IsCanceled = true;

            if (incomeFromDb.RootAssignedPaymentOrder != null) {
                _paymentOrderRepositoriesFactory.NewAssignedPaymentOrderRepository(connection).Remove(incomeFromDb.RootAssignedPaymentOrder.Id);

                if (incomeFromDb.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder != null)
                    _paymentOrderRepositoriesFactory
                        .NewOutcomePaymentOrderRepository(connection)
                        .SetIsUnderReportDoneById(
                            incomeFromDb.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.Id,
                            true
                        );
            }

            incomePaymentOrderRepository.Update(incomeFromDb);

            Sender.Tell(
                new Tuple<bool, string, IncomePaymentOrder, string>(
                    true,
                    IncomePaymentOrderResourceNames.INCOME_CANCELED,
                    incomePaymentOrderRepository.GetByNetId(message.NetId),
                    string.Empty
                )
            );
        }
    }

    private void ProcessChangeClientOnIncomePaymentOrderMessage(ChangeClientOnIncomePaymentOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);

        IncomePaymentOrder incomeFromDb = incomePaymentOrderRepository.GetByNetIdReversed(message.IncomeNetId);

        if (incomeFromDb != null) {
            Client newClient = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithoutIncludes(message.ClientNetId);

            if (newClient != null) {
                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                ClientAgreement newClientAgreement = clientAgreementRepository.GetByNetIdWithAgreement(message.ClientAgreementNetId);

                if (newClientAgreement != null) {
                    if (newClientAgreement.ClientId.Equals(newClient.Id)) {
                        if (incomeFromDb.ClientId.HasValue && incomeFromDb.ClientAgreementId.HasValue && !newClient.Id.Equals(incomeFromDb.ClientId.Value)) {
                            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                            IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
                            IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
                            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
                            IExchangeRateRepository exchangeRateRepository =
                                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                            ICrossExchangeRateRepository crossExchangeRateRepository =
                                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
                            IBaseSalePaymentStatusRepository salePaymentStatusRepository = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);
                            IIncomePaymentOrderSaleRepository incomePaymentOrderSaleRepository =
                                _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection);

                            if (incomeFromDb.IncomePaymentOrderSales.Any()) {
                                IEnumerable<long> saleIds =
                                    incomeFromDb.IncomePaymentOrderSales
                                        .Select(j => j.SaleId ?? j.ReSaleId ?? 0)
                                        .Where(x => !x.Equals(0));

                                List<ClientInDebt> toRestoreClientInDebts = clientInDebtRepository.GetAllBySaleIdsWithDeleted(saleIds);

                                ResetDebtsOnIncomePaymentOrderSales(
                                    _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                                    _clientRepositoriesFactory.NewClientCashFlowRepository(connection),
                                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                                    _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection),
                                    incomePaymentOrderRepository,
                                    clientInDebtRepository,
                                    salePaymentStatusRepository,
                                    incomePaymentOrderSaleRepository,
                                    debtRepository,
                                    saleRepository,
                                    _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection),
                                    _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection),
                                    _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection),
                                    _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists(),
                                    incomeFromDb,
                                    toRestoreClientInDebts.OrderBy(x => x.Created).ToList(),
                                    true
                                );

                                incomePaymentOrderSaleRepository.RemoveAllByIds(incomeFromDb.IncomePaymentOrderSales.Select(j => j.Id));
                            } else {
                                ClientAgreement clientAgreement = incomeFromDb.ClientAgreement == null || incomeFromDb.ClientAgreement.IsNew()
                                    ? clientAgreementRepository.GetActiveByClientId(incomeFromDb.ClientId.Value)
                                    : clientAgreementRepository.GetByIdWithAgreementAndOrganization(incomeFromDb.ClientAgreement.Id);

                                ResetDebtOnToBalancePayment(
                                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                                    _saleRepositoriesFactory.NewSaleRepository(connection),
                                    _reSaleRepositoriesFactory.NewReSaleRepository(connection),
                                    _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection),
                                    clientAgreementRepository,
                                    clientInDebtRepository,
                                    debtRepository,
                                    exchangeRateRepository,
                                    crossExchangeRateRepository,
                                    incomeFromDb,
                                    _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists(),
                                    clientAgreement
                                );
                            }

                            incomeFromDb.UserId = user.Id;

                            incomeFromDb.IsCanceled = true;

                            incomePaymentOrderRepository.Update(incomeFromDb);

                            incomeFromDb = incomePaymentOrderRepository.GetByNetIdReversed(message.IncomeNetId);

                            Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                            PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                                euroCurrency,
                                incomeFromDb.Currency,
                                incomeFromDb.FromDate,
                                exchangeRateRepository,
                                crossExchangeRateRepository);

                            AgreementConversionResult agreementConversionResult = paymentOrdersCurrencyConvertor.GetConvertedAmountToAgreementCurrency(
                                incomeFromDb.Amount,
                                decimal.Zero,
                                newClientAgreement.Agreement.Currency);

                            IncomePaymentOrder newIncomePaymentOrder = new() {
                                UserId = user.Id,
                                IncomePaymentOrderType = incomeFromDb.IncomePaymentOrderType,
                                IsAccounting = incomeFromDb.IsAccounting,
                                ExchangeRate = agreementConversionResult.ExchangeRate,
                                CurrencyId = incomeFromDb.CurrencyId,
                                Currency = incomeFromDb.Currency,
                                FromDate = incomeFromDb.FromDate,
                                ColleagueId = incomeFromDb.ColleagueId,
                                Account = incomeFromDb.Account,
                                EuroAmount = incomeFromDb.EuroAmount,
                                Amount = incomeFromDb.Amount,
                                AgreementExchangedAmount = agreementConversionResult.InAgreementCurrencyAmount,
                                AgreementEuroExchangeRate = paymentOrdersCurrencyConvertor.GetAgreementCurrencyToEuroExchangeRate(newClientAgreement.Agreement.Currency),
                                BankAccount = incomeFromDb.BankAccount,
                                Comment = incomeFromDb.Comment,
                                IsManagementAccounting = incomeFromDb.IsManagementAccounting,
                                OrganizationId = incomeFromDb.OrganizationId,
                                OverpaidAmount = incomeFromDb.OverpaidAmount,
                                VAT = incomeFromDb.VAT,
                                VatPercent = incomeFromDb.VatPercent,
                                PaymentRegisterId = incomeFromDb.PaymentRegisterId,
                                ClientId = newClient.Id,
                                ClientAgreementId = newClientAgreement.Id
                            };

                            IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(incomeFromDb.PaymentRegister.Type);

                            if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                                newIncomePaymentOrder.Number = incomeFromDb.Organization.Code + string.Format("{0:D10}", 1);
                            else
                                newIncomePaymentOrder.Number =
                                    incomeFromDb.Organization.Code +
                                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

                            newClientAgreement = clientAgreementRepository.GetByIdWithAgreementAndOrganization(newClientAgreement.Id);

                            List<ClientInDebt> newClientInDebts =
                                clientInDebtRepository
                                    .GetAllByClientId(
                                        newClient.Id
                                    );

                            if (newClientInDebts.Any())
                                newIncomePaymentOrder.IncomePaymentOrderSales =
                                    newClientInDebts
                                        .Select(debt => new IncomePaymentOrderSale {
                                            SaleId = debt.SaleId ?? debt.ReSaleId
                                        }).ToList();

                            if (!newIncomePaymentOrder.IncomePaymentOrderSales.Any()) {
                                _clientRepositoriesFactory
                                    .NewClientBalanceMovementRepository(connection)
                                    .AddInMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = newClientAgreement.Id,
                                            Amount = newIncomePaymentOrder.EuroAmount,
                                            ExchangeRateAmount = newIncomePaymentOrder.EuroAmount
                                        }
                                    );

                                clientAgreementRepository.UpdateAmountByNetId(newClientAgreement.NetUid, newClientAgreement.CurrentAmount);
                            }

                            newIncomePaymentOrder.Id = incomePaymentOrderRepository.Add(newIncomePaymentOrder);

                            PaymentMovementOperation paymentMovementOperation = new() {
                                PaymentMovementId = incomeFromDb.PaymentMovementOperation.PaymentMovementId,
                                IncomePaymentOrderId = newIncomePaymentOrder.Id
                            };

                            _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(paymentMovementOperation);

                            if (newIncomePaymentOrder.IncomePaymentOrderSales.Any()) {
                                IEnumerable<long> saleIds = newIncomePaymentOrder
                                    .IncomePaymentOrderSales
                                    .Where(s => s.Sale != null || s.ReSale != null)
                                    .Select(s => s.Sale?.Id ?? s.ReSale?.Id ?? 0)
                                    .Where(x => !x.Equals(0));

                                List<ClientInDebt> clientInDebts = _clientRepositoriesFactory.NewClientInDebtRepository(connection).GetAllBySaleIds(saleIds);

                                DecreaseDebts(
                                    newIncomePaymentOrder,
                                    clientInDebts.OrderBy(x => x.Created).ToList(),
                                    _saleRepositoriesFactory.NewDebtRepository(connection),
                                    _clientRepositoriesFactory.NewClientInDebtRepository(connection),
                                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                                    _clientRepositoriesFactory.NewClientAgreementRepository(connection),
                                    _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection),
                                    incomePaymentOrderRepository,
                                    salePaymentStatusRepository,
                                    _paymentOrderRepositoriesFactory.NewIncomePaymentOrderSaleRepository(connection),
                                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                                    saleRepository
                                );
                            }

                            Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomePaymentOrderRepository.GetById(newIncomePaymentOrder.Id), string.Empty));
                        } else {
                            Sender.Tell(
                                new Tuple<IncomePaymentOrder, string>(
                                    null,
                                    IncomePaymentOrderResourceNames.TO_CHANGE_CLIENT_YOU_NEED_TO_SELECT_DIFFERENT_CLIENT_FROM_EXISTING_ONE
                                )
                            );
                        }
                    } else {
                        Sender.Tell(
                            new Tuple<IncomePaymentOrder, string>(
                                null,
                                IncomePaymentOrderResourceNames.CLIENT_AGREEMENT_NOT_ASSIGNED_TO_CLIENT
                            )
                        );
                    }
                } else {
                    Sender.Tell(
                        new Tuple<IncomePaymentOrder, string>(
                            null,
                            IncomePaymentOrderResourceNames.SPECIFIED_CLIENT_AGREEMENT_NOT_EXISTS
                        )
                    );
                }
            } else {
                Sender.Tell(
                    new Tuple<IncomePaymentOrder, string>(
                        null,
                        IncomePaymentOrderResourceNames.SPECIFIED_CLIENT_NOT_EXISTS
                    )
                );
            }
        } else {
            Sender.Tell(
                new Tuple<IncomePaymentOrder, string>(
                    null,
                    IncomePaymentOrderResourceNames.SPECIFIED_INCOME_NOT_EXISTS
                )
            );
        }
    }

    private void ProcessAddNewIncomePaymentOrderFromTaxFreeMessage(AddNewIncomePaymentOrderFromTaxFreeMessage message) {
        if (message.IncomePaymentOrder == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
            return;
        }

        if (!message.IncomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
            return;
        }

        if (message.IncomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.Organization == null || message.IncomePaymentOrder.Organization.IsNew()) &&
            message.IncomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));
            return;
        }

        if (message.IncomePaymentOrder.PaymentMovementOperation?.PaymentMovement == null ||
            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.IsNew()) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.PaymentRegister == null || message.IncomePaymentOrder.PaymentRegister.IsNew()) &&
            message.IncomePaymentOrder.PaymentRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_REGISTER_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.Currency == null || message.IncomePaymentOrder.Currency.IsNew()) &&
            message.IncomePaymentOrder.CurrencyId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.CURRENCY_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.ClientAgreement == null || message.IncomePaymentOrder.ClientAgreement.IsNew()) &&
            (message.IncomePaymentOrder.OrganizationClientAgreement == null || message.IncomePaymentOrder.OrganizationClientAgreement.IsNew())) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PARTNER_AGREEMENT_NOT_SPECIFIED));
            return;
        }

        if (message.TaxFreeNetId.Equals(Guid.Empty)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.TAX_FREE_INVALID));
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        if (message.IncomePaymentOrder.ClientAgreement != null && !message.IncomePaymentOrder.ClientAgreement.IsNew()) {
            ClientAgreement clientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByIdWithoutIncludes(message.IncomePaymentOrder.ClientAgreement.Id);

            message.IncomePaymentOrder.ClientAgreementId = clientAgreement?.Id;
            message.IncomePaymentOrder.ClientId = clientAgreement?.ClientId;
        }

        if (message.IncomePaymentOrder.OrganizationClientAgreement != null && !message.IncomePaymentOrder.OrganizationClientAgreement.IsNew()) {
            message.IncomePaymentOrder.OrganizationClientAgreementId = message.IncomePaymentOrder.OrganizationClientAgreement.Id;
            message.IncomePaymentOrder.OrganizationClientId = message.IncomePaymentOrder.OrganizationClientAgreement.OrganizationClientId;
        }

        if (message.IncomePaymentOrder.Organization != null && !message.IncomePaymentOrder.Organization.IsNew())
            message.IncomePaymentOrder.OrganizationId = message.IncomePaymentOrder.Organization.Id;

        if (message.IncomePaymentOrder.PaymentRegister != null && !message.IncomePaymentOrder.PaymentRegister.IsNew())
            message.IncomePaymentOrder.PaymentRegisterId = message.IncomePaymentOrder.PaymentRegister.Id;

        if (message.IncomePaymentOrder.Currency != null && !message.IncomePaymentOrder.Currency.IsNew())
            message.IncomePaymentOrder.CurrencyId = message.IncomePaymentOrder.Currency.Id;

        if (message.IncomePaymentOrder.FromDate.Year.Equals(1)) message.IncomePaymentOrder.FromDate = DateTime.UtcNow;

        ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

        TaxFree taxFree = taxFreeRepository.GetByNetId(message.TaxFreeNetId);

        if (taxFree == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.TAX_FREE_INVALID));
            return;
        }

        if (taxFree.TaxFreePackList == null || !taxFree.TaxFreePackList.IsSent) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.TAX_FREE_PACK_LIST_SHOULD_BE_SENT));
            return;
        }

        double daysDifference = (message.IncomePaymentOrder.FromDate - (taxFree.FormedDate ?? taxFree.Created)).TotalDays;

        if (daysDifference < 0) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_GREATER_THAN_TAX_FREE_CREATED));
            return;
        }

        if (daysDifference > 180) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_LESS_THAN_TAX_FREE_CREATED_PLUS_HALF_YEAR));
            return;
        }

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        message.IncomePaymentOrder.TaxFreeId = taxFree.Id;
        message.IncomePaymentOrder.UserId = user.Id;
        message.IncomePaymentOrder.ExchangeRate = 1m;
        message.IncomePaymentOrder.EuroAmount = message.IncomePaymentOrder.Amount;

        PaymentRegister register = paymentRegisterRepository
            .GetById(
                message.IncomePaymentOrder.PaymentRegisterId.Equals(0)
                    ? message.IncomePaymentOrder.PaymentRegister?.Id ?? 0
                    : message.IncomePaymentOrder.PaymentRegisterId
            );

        PaymentCurrencyRegister paymentCurrencyRegister =
            register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId));

        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + message.IncomePaymentOrder.Amount, 2);

        _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

        IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);

        IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister?.Type ?? PaymentRegisterType.Bank);

        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
            message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
        else
            message.IncomePaymentOrder.Number =
                message.IncomePaymentOrder.Organization!.Code +
                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

        message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

        if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
            (!message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
             message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
           ) {
            if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                    message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

            message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

            _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
        }

        Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomePaymentOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
    }

    private void ProcessAddNewIncomePaymentOrderFromSadMessage(AddNewIncomePaymentOrderFromSadMessage message) {
        if (message.IncomePaymentOrder == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
            return;
        }

        if (!message.IncomePaymentOrder.IsNew()) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.INCORRECT_INCOME_PAYMENT_ORDER));
            return;
        }

        if (message.IncomePaymentOrder.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.AMOUNT_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.Organization == null || message.IncomePaymentOrder.Organization.IsNew()) &&
            message.IncomePaymentOrder.OrganizationId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.ORGANIZATION_NOT_SPECIFIED));
            return;
        }

        if (message.IncomePaymentOrder.PaymentMovementOperation?.PaymentMovement == null ||
            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.IsNew()) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_MOVEMENT_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.PaymentRegister == null || message.IncomePaymentOrder.PaymentRegister.IsNew()) &&
            message.IncomePaymentOrder.PaymentRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_REGISTER_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.Currency == null || message.IncomePaymentOrder.Currency.IsNew()) &&
            message.IncomePaymentOrder.CurrencyId.Equals(0)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.CURRENCY_NOT_SPECIFIED));
            return;
        }

        if ((message.IncomePaymentOrder.ClientAgreement == null || message.IncomePaymentOrder.ClientAgreement.IsNew()) &&
            (message.IncomePaymentOrder.OrganizationClientAgreement == null || message.IncomePaymentOrder.OrganizationClientAgreement.IsNew())) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PARTNER_AGREEMENT_NOT_SPECIFIED));
            return;
        }

        if (message.SadNetId.Equals(Guid.Empty)) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.SAD_INVALID));
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        if (message.IncomePaymentOrder.ClientAgreement != null && !message.IncomePaymentOrder.ClientAgreement.IsNew()) {
            ClientAgreement clientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByIdWithoutIncludes(message.IncomePaymentOrder.ClientAgreement.Id);

            message.IncomePaymentOrder.ClientAgreementId = clientAgreement?.Id;
            message.IncomePaymentOrder.ClientId = clientAgreement?.ClientId;
        }

        if (message.IncomePaymentOrder.OrganizationClientAgreement != null && !message.IncomePaymentOrder.OrganizationClientAgreement.IsNew()) {
            message.IncomePaymentOrder.OrganizationClientAgreementId = message.IncomePaymentOrder.OrganizationClientAgreement.Id;
            message.IncomePaymentOrder.OrganizationClientId = message.IncomePaymentOrder.OrganizationClientAgreement.OrganizationClientId;
        }

        if (message.IncomePaymentOrder.Organization != null && !message.IncomePaymentOrder.Organization.IsNew())
            message.IncomePaymentOrder.OrganizationId = message.IncomePaymentOrder.Organization.Id;

        if (message.IncomePaymentOrder.PaymentRegister != null && !message.IncomePaymentOrder.PaymentRegister.IsNew())
            message.IncomePaymentOrder.PaymentRegisterId = message.IncomePaymentOrder.PaymentRegister.Id;

        if (message.IncomePaymentOrder.Currency != null && !message.IncomePaymentOrder.Currency.IsNew())
            message.IncomePaymentOrder.CurrencyId = message.IncomePaymentOrder.Currency.Id;

        if (message.IncomePaymentOrder.FromDate.Year.Equals(1)) message.IncomePaymentOrder.FromDate = DateTime.UtcNow;

        ISadRepository sadRepository = _supplyUkraineRepositoriesFactory.NewSadRepository(connection, exchangeRateConnection);

        Sad sad = sadRepository.GetByNetId(message.SadNetId);

        if (sad == null) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.SAD_INVALID));
            return;
        }

        if (!sad.IsSend) {
            Sender.Tell(new Tuple<OutcomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.SAD_SHOULD_BE_SENT));
            return;
        }

        double daysDifference = (message.IncomePaymentOrder.FromDate - sad.FromDate).TotalDays;

        if (daysDifference < 0) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_GREATER_THAN_SAD_CREATED));
            return;
        }

        if (daysDifference > 180) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.FROM_DATE_SHOULD_BE_LESS_THAN_SAD_CREATED_PLUS_HALF_YEAR));
            return;
        }

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        message.IncomePaymentOrder.SadId = sad.Id;
        message.IncomePaymentOrder.UserId = user.Id;
        message.IncomePaymentOrder.ExchangeRate = 1m;
        message.IncomePaymentOrder.EuroAmount = message.IncomePaymentOrder.Amount;

        PaymentRegister register = paymentRegisterRepository
            .GetById(
                message.IncomePaymentOrder.PaymentRegisterId.Equals(0)
                    ? message.IncomePaymentOrder.PaymentRegister?.Id ?? 0
                    : message.IncomePaymentOrder.PaymentRegisterId
            );

        PaymentCurrencyRegister paymentCurrencyRegister =
            register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId));

        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + message.IncomePaymentOrder.Amount, 2);

        _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

        IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);

        IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister?.Type ?? PaymentRegisterType.Bank);

        if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
            message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
        else
            message.IncomePaymentOrder.Number =
                message.IncomePaymentOrder.Organization!.Code +
                string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);

        message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

        if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
            (!message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
             message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
           ) {
            if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                    message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

            message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

            _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
        }

        Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomePaymentOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
    }

    private static void ResetDebtOnToBalancePayment(
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        ISaleRepository saleRepository,
        IReSaleRepository reSaleRepository,
        IBaseSalePaymentStatusRepository baseSalePaymentStatusRepository,
        IClientAgreementRepository clientAgreementRepository,
        IClientInDebtRepository clientInDebtRepository,
        IDebtRepository debtRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IncomePaymentOrder incomeFromDb,
        Currency euroCurrency,
        ClientAgreement clientAgreement
    ) {
        if (clientAgreement.CurrentAmount >= incomeFromDb.EuroAmount) {
            clientAgreement.CurrentAmount =
                decimal.Round(clientAgreement.CurrentAmount - incomeFromDb.EuroAmount, 4, MidpointRounding.AwayFromZero);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddOutMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreement.Id,
                        Amount = incomeFromDb.EuroAmount,
                        ExchangeRateAmount = 1m
                    }
                );
        } else {
            decimal toDebtAmount =
                decimal.Round(incomeFromDb.EuroAmount - clientAgreement.CurrentAmount, 4, MidpointRounding.AwayFromZero);

            clientAgreement.CurrentAmount = decimal.Zero;

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddOutMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreement.Id,
                        Amount = incomeFromDb.EuroAmount,
                        ExchangeRateAmount = 1m
                    }
                );

            Sale lastSale =
                saleRepository
                    .GetLastPaidSalesByClientAgreementId(
                        clientAgreement.Id,
                        incomeFromDb.Created
                    ).FirstOrDefault();

            ReSale lastReSale =
                reSaleRepository
                    .GetLastPaidReSalesByClientAgreementId(
                        clientAgreement.Id,
                        incomeFromDb.Created
                    ).FirstOrDefault();


            if (lastSale == null && lastReSale == null) return;

            long baseSalePaymentStatusId;
            long saleId;

            bool isReSaleId = false;

            if (lastSale != null) {
                if (lastReSale == null) {
                    baseSalePaymentStatusId = lastSale.BaseSalePaymentStatusId;
                    saleId = lastSale.Id;
                } else {
                    if (lastSale.Created > lastReSale.Created) {
                        baseSalePaymentStatusId = lastSale.BaseSalePaymentStatusId;
                        saleId = lastSale.Id;
                    } else {
                        baseSalePaymentStatusId = lastReSale.BaseSalePaymentStatusId;
                        saleId = lastReSale.Id;
                        isReSaleId = true;
                    }
                }
            } else {
                baseSalePaymentStatusId = lastReSale.BaseSalePaymentStatusId;
                saleId = lastReSale.Id;
                isReSaleId = true;
            }

            ClientInDebt clientInDebt = clientInDebtRepository.GetBySaleAndClientAgreementIds(saleId, clientAgreement.Id);

            decimal euroExchangeRate;

            if (euroCurrency.Id.Equals(clientAgreement.Agreement.CurrencyId ?? 0)) {
                euroExchangeRate = 1m;
            } else {
                ExchangeRate exchangeRate =
                    exchangeRateRepository
                        .GetByCurrencyIdAndCode(
                            clientAgreement.Agreement.CurrencyId ?? 0,
                            euroCurrency.Code,
                            TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                        );

                if (exchangeRate != null) {
                    euroExchangeRate = exchangeRate.Amount;
                } else {
                    CrossExchangeRate crossExchangeRate =
                        crossExchangeRateRepository
                            .GetByCurrenciesIds(
                                clientAgreement.Agreement.CurrencyId ?? 0,
                                euroCurrency.Id,
                                TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                            );

                    if (crossExchangeRate != null) {
                        euroExchangeRate = decimal.Zero - crossExchangeRate.Amount;
                    } else {
                        crossExchangeRate =
                            crossExchangeRateRepository
                                .GetByCurrenciesIds(
                                    euroCurrency.Id,
                                    clientAgreement.Agreement.CurrencyId ?? 0,
                                    TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                );

                        euroExchangeRate = crossExchangeRate?.Amount ?? 1m;
                    }
                }
            }

            if (clientInDebt != null) {
                clientInDebt.Debt.Total =
                    decimal.Round(clientInDebt.Debt.Total + toDebtAmount * euroExchangeRate, 4, MidpointRounding.AwayFromZero);

                debtRepository.Update(clientInDebt.Debt);
            } else {
                if (isReSaleId)
                    clientInDebtRepository.Add(new ClientInDebt {
                        AgreementId = clientAgreement.AgreementId,
                        ClientId = clientAgreement.ClientId,
                        ReSaleId = saleId,
                        DebtId = debtRepository.Add(new Debt {
                            Days = 0,
                            Total = decimal.Round(toDebtAmount * euroExchangeRate, 4, MidpointRounding.AwayFromZero)
                        })
                    });
                else
                    clientInDebtRepository.Add(new ClientInDebt {
                        AgreementId = clientAgreement.AgreementId,
                        ClientId = clientAgreement.ClientId,
                        SaleId = saleId,
                        DebtId = debtRepository.Add(new Debt {
                            Days = 0,
                            Total = decimal.Round(toDebtAmount * euroExchangeRate, 4, MidpointRounding.AwayFromZero)
                        })
                    });
            }

            baseSalePaymentStatusRepository
                .SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, baseSalePaymentStatusId);
        }
    }

    private static void ResetDebtsOnIncomePaymentOrderSales(
        IClientAgreementRepository clientAgreementRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        IClientCashFlowRepository clientCashFlowRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IIncomePaymentOrderRepository incomePaymentOrderRepository,
        IClientInDebtRepository clientInDebtRepository,
        IBaseSalePaymentStatusRepository salePaymentStatusRepository,
        IIncomePaymentOrderSaleRepository incomePaymentOrderSaleRepository,
        IDebtRepository debtRepository,
        ISaleRepository saleRepository,
        IRetailPaymentStatusRepository retailPaymentStatusRepository,
        IRetailClientPaymentImageRepository paymentImageRepository,
        IRetailClientPaymentImageItemRepository paymentImageItemRepository,
        Currency euroCurrency,
        IncomePaymentOrder incomeFromDb,
        List<ClientInDebt> clientInDebts,
        bool resetTotalOnDeletedDebt = false
    ) {
        foreach (IncomePaymentOrderSale junction in incomeFromDb.IncomePaymentOrderSales) {
            ClientInDebt clientInDebt = clientInDebts.First(c => c.SaleId.Equals(junction.SaleId) || c.ReSaleId.Equals(junction.ReSaleId));

            if (clientInDebt != null) {
                bool partiallyPaidFromBalance = false;
                bool fullyPaidFromBalance = false;

                if (clientInDebt.Deleted) {
                    clientInDebtRepository.Restore(clientInDebt.Id);

                    if (resetTotalOnDeletedDebt) {
                        clientInDebt.Debt.Total = decimal.Round(junction.Amount * junction.ExchangeRate, 4, MidpointRounding.AwayFromZero);

                        debtRepository.Update(clientInDebt.Debt);
                    }

                    if (clientInDebt.Sale.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.Overpaid)) {
                        ClientAgreement clientAgreement =
                            incomeFromDb.ClientAgreement != null && !incomeFromDb.ClientAgreement.IsNew()
                                ? clientAgreementRepository.GetByIdWithAgreementAndOrganization(incomeFromDb.ClientAgreement.Id)
                                : clientAgreementRepository.GetByClientAndAgreementIds(clientInDebt.ClientId, clientInDebt.AgreementId);

                        if (clientAgreement.CurrentAmount.Equals(incomeFromDb.OverpaidAmount)) {
                            clientAgreement.CurrentAmount = decimal.Zero;

                            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                            clientBalanceMovementRepository
                                .AddOutMovement(
                                    new ClientBalanceMovement {
                                        ClientAgreementId = clientAgreement.Id,
                                        Amount = incomeFromDb.OverpaidAmount,
                                        ExchangeRateAmount = 1m
                                    }
                                );
                        } else if (incomeFromDb.OverpaidAmount > clientAgreement.CurrentAmount) {
                            decimal toDebtAmount =
                                decimal.Round(incomeFromDb.OverpaidAmount - clientAgreement.CurrentAmount, 4, MidpointRounding.AwayFromZero);

                            clientAgreement.CurrentAmount = decimal.Zero;

                            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                            clientBalanceMovementRepository
                                .AddOutMovement(
                                    new ClientBalanceMovement {
                                        ClientAgreementId = clientAgreement.Id,
                                        Amount = incomeFromDb.OverpaidAmount,
                                        ExchangeRateAmount = 1m
                                    }
                                );

                            decimal exchangeRateAmount;

                            if (euroCurrency.Id.Equals(clientAgreement.Agreement.CurrencyId.Value)) {
                                exchangeRateAmount = 1m;
                            } else {
                                ExchangeRate exchangeRate =
                                    exchangeRateRepository
                                        .GetByCurrencyIdAndCode(
                                            clientAgreement.Agreement.CurrencyId.Value,
                                            euroCurrency.Code,
                                            TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                        );

                                if (exchangeRate != null) {
                                    exchangeRateAmount = exchangeRate.Amount;
                                } else {
                                    CrossExchangeRate crossExchangeRate =
                                        crossExchangeRateRepository
                                            .GetByCurrenciesIds(
                                                clientAgreement.Agreement.CurrencyId.Value,
                                                euroCurrency.Id,
                                                TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                            );

                                    if (crossExchangeRate != null) {
                                        exchangeRateAmount = decimal.Zero - crossExchangeRate.Amount;
                                    } else {
                                        crossExchangeRate =
                                            crossExchangeRateRepository
                                                .GetByCurrenciesIds(
                                                    euroCurrency.Id,
                                                    clientAgreement.Agreement.CurrencyId.Value,
                                                    TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                                );

                                        exchangeRateAmount =
                                            crossExchangeRate?.Amount ?? 1m;
                                    }
                                }
                            }

                            clientInDebt.Debt.Total =
                                decimal.Round(clientInDebt.Debt.Total + toDebtAmount * exchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                            debtRepository.Update(clientInDebt.Debt);
                        } else {
                            decimal exchangeRateAmount;

                            if (euroCurrency.Id.Equals(clientAgreement.Agreement.CurrencyId.Value)) {
                                exchangeRateAmount = 1m;
                            } else {
                                ExchangeRate exchangeRate =
                                    exchangeRateRepository
                                        .GetByCurrencyIdAndCode(
                                            clientAgreement.Agreement.CurrencyId.Value,
                                            euroCurrency.Code,
                                            TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                        );

                                if (exchangeRate != null) {
                                    exchangeRateAmount = exchangeRate.Amount;
                                } else {
                                    CrossExchangeRate crossExchangeRate =
                                        crossExchangeRateRepository
                                            .GetByCurrenciesIds(
                                                clientAgreement.Agreement.CurrencyId.Value,
                                                euroCurrency.Id,
                                                TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                            );

                                    if (crossExchangeRate != null) {
                                        exchangeRateAmount = decimal.Zero - crossExchangeRate.Amount;
                                    } else {
                                        crossExchangeRate =
                                            crossExchangeRateRepository
                                                .GetByCurrenciesIds(
                                                    euroCurrency.Id,
                                                    clientAgreement.Agreement.CurrencyId.Value,
                                                    TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                                );

                                        exchangeRateAmount =
                                            crossExchangeRate?.Amount ?? 1m;
                                    }
                                }
                            }

                            clientAgreement.CurrentAmount =
                                decimal.Round(clientAgreement.CurrentAmount - incomeFromDb.OverpaidAmount, 4, MidpointRounding.AwayFromZero);

                            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                            clientBalanceMovementRepository
                                .AddOutMovement(
                                    new ClientBalanceMovement {
                                        ClientAgreementId = clientAgreement.Id,
                                        Amount = incomeFromDb.OverpaidAmount,
                                        ExchangeRateAmount = 1m
                                    }
                                );

                            decimal amountInDebtCurrency =
                                decimal.Round(clientAgreement.CurrentAmount * exchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                            if (amountInDebtCurrency > clientInDebt.Debt.Total) {
                                fullyPaidFromBalance = true;

                                amountInDebtCurrency =
                                    decimal.Round(amountInDebtCurrency - clientInDebt.Debt.Total, 4, MidpointRounding.AwayFromZero);

                                clientBalanceMovementRepository
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = decimal.Round(
                                                clientInDebt.Debt.Total / exchangeRateAmount,
                                                4,
                                                MidpointRounding.AwayFromZero
                                            ),
                                            ExchangeRateAmount = exchangeRateAmount
                                        }
                                    );

                                clientAgreement.CurrentAmount =
                                    decimal.Round(amountInDebtCurrency / exchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                clientInDebtRepository.Remove(clientInDebt.NetUid);
                            } else if (amountInDebtCurrency.Equals(clientInDebt.Debt.Total)) {
                                fullyPaidFromBalance = true;

                                clientBalanceMovementRepository
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = clientAgreement.CurrentAmount,
                                            ExchangeRateAmount = exchangeRateAmount
                                        }
                                    );

                                clientAgreement.CurrentAmount = decimal.Zero;

                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                clientInDebtRepository.Remove(clientInDebt.NetUid);
                            } else {
                                partiallyPaidFromBalance = true;

                                clientBalanceMovementRepository
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = clientAgreement.CurrentAmount,
                                            ExchangeRateAmount = exchangeRateAmount
                                        }
                                    );

                                clientAgreement.CurrentAmount = decimal.Zero;

                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                clientInDebt.Debt.Total =
                                    decimal.Round(clientInDebt.Debt.Total - amountInDebtCurrency, 4, MidpointRounding.AwayFromZero);

                                debtRepository.Update(clientInDebt.Debt);
                            }
                        }
                    } else {
                        ClientAgreement clientAgreement =
                            incomeFromDb.ClientAgreement != null && !incomeFromDb.ClientAgreement.IsNew()
                                ? clientAgreementRepository.GetByIdWithAgreementAndOrganization(incomeFromDb.ClientAgreement.Id)
                                : clientAgreementRepository.GetByClientAndAgreementIds(clientInDebt.ClientId, clientInDebt.AgreementId);

                        decimal exchangeRateAmount;

                        incomeFromDb.IsCanceled = true;

                        incomePaymentOrderRepository.Update(incomeFromDb);

                        if (euroCurrency.Id.Equals(clientAgreement.Agreement.CurrencyId.Value)) {
                            exchangeRateAmount = 1m;
                        } else {
                            ExchangeRate exchangeRate =
                                exchangeRateRepository
                                    .GetByCurrencyIdAndCode(
                                        clientAgreement.Agreement.CurrencyId.Value,
                                        euroCurrency.Code,
                                        TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                    );

                            if (exchangeRate != null) {
                                exchangeRateAmount = exchangeRate.Amount;
                            } else {
                                CrossExchangeRate crossExchangeRate =
                                    crossExchangeRateRepository
                                        .GetByCurrenciesIds(
                                            clientAgreement.Agreement.CurrencyId.Value,
                                            euroCurrency.Id,
                                            TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                        );

                                if (crossExchangeRate != null) {
                                    exchangeRateAmount = decimal.Zero - crossExchangeRate.Amount;
                                } else {
                                    crossExchangeRate =
                                        crossExchangeRateRepository
                                            .GetByCurrenciesIds(
                                                euroCurrency.Id,
                                                clientAgreement.Agreement.CurrencyId.Value,
                                                TimeZoneInfo.ConvertTimeToUtc(incomeFromDb.FromDate)
                                            );

                                    exchangeRateAmount =
                                        crossExchangeRate?.Amount ?? 1m;
                                }
                            }
                        }

                        clientAgreement = clientAgreementRepository.GetByNetIdWithIncludes(clientAgreement.NetUid);

                        AccountingCashFlow accountingCashFlow =
                            clientCashFlowRepository
                                .GetRangedByClientAgreement(
                                    clientAgreement,
                                    incomeFromDb.FromDate.Date.AddMonths(-2),
                                    DateTime.Now.Date.AddDays(3)
                                );

                        decimal currentAmount = accountingCashFlow.AccountingCashFlowHeadItems.Last().CurrentBalance;

                        if (!currentAmount.Equals(decimal.Zero)) {
                            if (currentAmount > 0) {
                                clientAgreement.CurrentAmount =
                                    decimal.Round(currentAmount / exchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                                clientBalanceMovementRepository
                                    .AddInMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = clientAgreement.CurrentAmount,
                                            ExchangeRateAmount = exchangeRateAmount
                                        }
                                    );

                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                clientInDebtRepository.Remove(clientInDebt.NetUid);

                                fullyPaidFromBalance = true;
                            } else {
                                if (clientInDebt.Sale?.RetailClientId != null) {
                                    RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(clientInDebt.Sale.Id);
                                    retailPaymentStatus.PaidAmount -= decimal.Round(clientInDebt.Debt.Total, 2, MidpointRounding.AwayFromZero);
                                    retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.ChangedToInvoice;
                                    retailPaymentStatusRepository.Update(retailPaymentStatus);

                                    RetailClientPaymentImage paymentImage = paymentImageRepository.GetPaymentImageBySaleNetId(clientInDebt.Sale.NetUid);

                                    // need to test
                                    paymentImage.RetailClientPaymentImageItems.ForEach(i => i.IsLocked = false);

                                    paymentImageItemRepository.Update(paymentImage.RetailClientPaymentImageItems);
                                }

                                clientInDebt.Debt.Total = 0m - currentAmount;

                                debtRepository.Update(clientInDebt.Debt);

                                clientBalanceMovementRepository
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = clientAgreement.CurrentAmount,
                                            ExchangeRateAmount = exchangeRateAmount
                                        }
                                    );

                                clientAgreement.CurrentAmount = decimal.Zero;

                                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                            }
                        }
                    }
                } else {
                    clientInDebt.Debt.Total += decimal.Round(junction.Amount * junction.ExchangeRate, 2, MidpointRounding.AwayFromZero);

                    if (clientInDebt.Sale?.RetailClientId != null) {
                        RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(clientInDebt.Sale.Id);
                        retailPaymentStatus.PaidAmount -= decimal.Round(junction.Amount * junction.ExchangeRate, 2, MidpointRounding.AwayFromZero);
                        retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.PartialPaid;
                        retailPaymentStatusRepository.Update(retailPaymentStatus);

                        RetailClientPaymentImage paymentImage = paymentImageRepository.GetPaymentImageBySaleNetId(clientInDebt.Sale.NetUid);
                        // need to test
                        RetailClientPaymentImageItem item =
                            paymentImage.RetailClientPaymentImageItems.OrderBy(i => i.Created).First();
                        item.IsLocked = false;

                        paymentImageItemRepository.Update(item);
                    }

                    debtRepository.Update(clientInDebt.Debt);
                }

                if (clientInDebt.Sale != null) {
                    if (fullyPaidFromBalance) {
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.Paid,
                            clientInDebt.Sale.BaseSalePaymentStatusId
                        );
                        saleRepository.UpdateIsAcceptedToPacking(clientInDebt.Sale.Id, true);

                        saleRepository.UnlockSaleById(clientInDebt.Sale.Id);
                    } else if (partiallyPaidFromBalance) {
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.PartialPaid,
                            clientInDebt.Sale.BaseSalePaymentStatusId
                        );
                    } else if (!incomePaymentOrderSaleRepository.CheckIsMoreThanOnePaymentBySaleId(junction.SaleId.Value)) {
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.NotPaid,
                            clientInDebt.Sale.BaseSalePaymentStatusId
                        );
                    } else {
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.PartialPaid,
                            clientInDebt.Sale.BaseSalePaymentStatusId
                        );
                    }
                } else if (clientInDebt.ReSale != null) {
                    if (fullyPaidFromBalance)
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.Paid,
                            clientInDebt.ReSale.BaseSalePaymentStatusId
                        );
                    else if (partiallyPaidFromBalance)
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.PartialPaid,
                            clientInDebt.ReSale.BaseSalePaymentStatusId
                        );
                    else if (!incomePaymentOrderSaleRepository.CheckIsMoreThanOnePaymentBySaleId(junction.ReSaleId.Value))
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.NotPaid,
                            clientInDebt.ReSale.BaseSalePaymentStatusId
                        );
                    else
                        salePaymentStatusRepository.SetSalePaymentStatusTypeById(
                            SalePaymentStatusType.PartialPaid,
                            clientInDebt.ReSale.BaseSalePaymentStatusId
                        );
                }
            }
        }
    }

    private static void DecreaseDebts(
        IncomePaymentOrder incomePaymentOrder,
        List<ClientInDebt> clientInDebts,
        IDebtRepository debtRepository,
        IClientInDebtRepository clientInDebtRepository,
        IExchangeRateRepository exchangeRateRepository,
        IClientAgreementRepository clientAgreementRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IIncomePaymentOrderRepository incomePaymentOrderRepository,
        IBaseSalePaymentStatusRepository salePaymentStatusRepository,
        IIncomePaymentOrderSaleRepository incomePaymentOrderSaleRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        ISaleRepository saleRepository,
        IRetailPaymentStatusRepository retailPaymentStatusRepository = null,
        IRetailClientPaymentImageRepository paymentImageRepository = null,
        IRetailClientPaymentImageItemRepository paymentImageItemRepository = null
    ) {
        for (int i = 0; i < clientInDebts.Count; i++) {
            decimal amountInDebtCurrency;

            if (clientInDebts[i].Agreement.CurrencyId.Equals(incomePaymentOrder.CurrencyId)) {
                amountInDebtCurrency = decimal.Round(incomePaymentOrder.Amount, 2, MidpointRounding.AwayFromZero);
            } else {
                ExchangeRate exchangeRate =
                    exchangeRateRepository.GetByCurrencyIdAndCode(clientInDebts[i].Agreement.Currency.Id, incomePaymentOrder.Currency.Code,
                        TimeZoneInfo.ConvertTimeToUtc(incomePaymentOrder.FromDate));

                if (exchangeRate != null) {
                    incomePaymentOrder.ExchangeRate = exchangeRate.Amount;
                } else {
                    exchangeRate =
                        exchangeRateRepository.GetByCurrencyIdAndCode(incomePaymentOrder.Currency.Id, clientInDebts[i].Agreement.Currency.Code,
                            TimeZoneInfo.ConvertTimeToUtc(incomePaymentOrder.FromDate));

                    if (exchangeRate != null) {
                        incomePaymentOrder.ExchangeRate = decimal.Zero - exchangeRate.Amount;
                    } else {
                        CrossExchangeRate crossExchangeRate =
                            crossExchangeRateRepository.GetByCurrenciesIds(clientInDebts[i].Agreement.Currency.Id, incomePaymentOrder.Currency.Id,
                                TimeZoneInfo.ConvertTimeToUtc(incomePaymentOrder.FromDate));

                        if (crossExchangeRate != null) {
                            incomePaymentOrder.ExchangeRate = decimal.Zero - crossExchangeRate.Amount;
                        } else {
                            crossExchangeRate =
                                crossExchangeRateRepository.GetByCurrenciesIds(incomePaymentOrder.Currency.Id, clientInDebts[i].Agreement.Currency.Id,
                                    TimeZoneInfo.ConvertTimeToUtc(incomePaymentOrder.FromDate));

                            incomePaymentOrder.ExchangeRate =
                                crossExchangeRate?.Amount ?? 1;
                        }
                    }
                }

                amountInDebtCurrency = incomePaymentOrder.ExchangeRate < decimal.Zero
                    ? decimal.Round(incomePaymentOrder.Amount / (decimal.Zero - incomePaymentOrder.ExchangeRate), 2, MidpointRounding.AwayFromZero)
                    : decimal.Round(incomePaymentOrder.Amount * incomePaymentOrder.ExchangeRate, 2, MidpointRounding.AwayFromZero);
            }

            bool finalOperation = i.Equals(clientInDebts.Count - 1);

            decimal debtAmount = decimal.Round(clientInDebts[i].Debt.Total, 2, MidpointRounding.AwayFromZero);

            decimal euroExchangeRateAmount =
                clientInDebts[i].Agreement.Currency.Code.ToLower().Equals("eur")
                    ? 1m
                    : exchangeRateRepository.GetExchangeRateToEuroCurrency(clientInDebts[i].Agreement.Currency);

            if (debtAmount > amountInDebtCurrency) {
                //We have less than we need for fully paid sale

                clientInDebts[i].Debt.Total =
                    decimal.Round(clientInDebts[i].Debt.Total - amountInDebtCurrency, 4, MidpointRounding.AwayFromZero);

                if (clientInDebts[i].SaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(amountInDebtCurrency / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        SaleId = clientInDebts[i].SaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = 0m
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, clientInDebts[i].Sale.BaseSalePaymentStatusId);

                    if (clientInDebts[i].Sale.RetailClientId.HasValue) {
                        RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(clientInDebts[i].Sale.Id);
                        retailPaymentStatus.PaidAmount += decimal.Round(amountInDebtCurrency, 4, MidpointRounding.AwayFromZero);
                        retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.PartialPaid;

                        RetailClientPaymentImage paymentImage = paymentImageRepository.GetPaymentImageBySaleNetId(clientInDebts[i].Sale.NetUid);
                        paymentImage.RetailClientPaymentImageItems.ForEach(e => e.IsLocked = true);

                        paymentImageItemRepository.Update(paymentImage.RetailClientPaymentImageItems);

                        retailPaymentStatusRepository.Update(retailPaymentStatus);
                    }
                } else if (clientInDebts[i].ReSaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(amountInDebtCurrency / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        ReSaleId = clientInDebts[i].ReSaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = 0m
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, clientInDebts[i].ReSale.BaseSalePaymentStatusId);
                }

                debtRepository.Update(clientInDebts[i].Debt);

                break;
            }

            if (debtAmount.Equals(amountInDebtCurrency)) {
                // We have enough for fully paid sale

                if (clientInDebts[i].SaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(amountInDebtCurrency / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        SaleId = clientInDebts[i].SaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = 0m
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, clientInDebts[i].Sale.BaseSalePaymentStatusId);

                    if (clientInDebts[i].Sale.RetailClientId.HasValue) {
                        Sale sale = saleRepository.GetByNetIdWithAgreement(clientInDebts[i].Sale.NetUid);

                        sale.TotalAmountLocal = sale.Order.OrderItems.Sum(o =>
                            decimal.Round(o.PricePerItem * (int)o.Qty * o.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero));

                        RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(clientInDebts[i].Sale.Id);
                        retailPaymentStatus.PaidAmount += decimal.Round(amountInDebtCurrency, 4, MidpointRounding.AwayFromZero);
                        retailPaymentStatus.RetailPaymentStatusType =
                            sale.TotalAmountLocal.Equals(retailPaymentStatus.PaidAmount)
                                ? RetailPaymentStatusType.Paid
                                : RetailPaymentStatusType.PartialPaid;

                        RetailClientPaymentImage paymentImage = paymentImageRepository.GetPaymentImageBySaleNetId(clientInDebts[i].Sale.NetUid);
                        paymentImage.RetailClientPaymentImageItems.ForEach(e => e.IsLocked = true);

                        paymentImageItemRepository.Update(paymentImage.RetailClientPaymentImageItems);

                        retailPaymentStatusRepository.Update(retailPaymentStatus);
                    }
                } else if (clientInDebts[i].ReSaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(amountInDebtCurrency / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        ReSaleId = clientInDebts[i].ReSaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = 0m
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, clientInDebts[i].ReSale.BaseSalePaymentStatusId);
                }

                if (clientInDebts[i].Sale != null) {
                    saleRepository.UpdateIsAcceptedToPacking(clientInDebts[i].Sale.Id, true);

                    saleRepository.UnlockSaleById(clientInDebts[i].Sale.Id);
                }

                clientInDebtRepository.Remove(clientInDebts[i].NetUid);

                break;
            }
// We have enough for overpaid sale

            if (finalOperation) {
                amountInDebtCurrency = decimal.Round(amountInDebtCurrency - clientInDebts[i].Debt.Total, 4, MidpointRounding.AwayFromZero);

                incomePaymentOrder.OverpaidAmount = decimal.Round(amountInDebtCurrency / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                if (clientInDebts[i].ReSaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(clientInDebts[i].Debt.Total / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        ReSaleId = clientInDebts[i].ReSaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = incomePaymentOrder.OverpaidAmount
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Overpaid, clientInDebts[i].ReSale.BaseSalePaymentStatusId);
                } else if (clientInDebts[i].SaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(clientInDebts[i].Debt.Total / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        SaleId = clientInDebts[i].SaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = incomePaymentOrder.OverpaidAmount
                    });

                    if (clientInDebts[i].Sale.RetailClientId.HasValue) {
                        RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetBySaleId(clientInDebts[i].Sale.Id);
                        retailPaymentStatus.PaidAmount += decimal.Round(amountInDebtCurrency, 4, MidpointRounding.AwayFromZero);
                        retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.Paid;

                        RetailClientPaymentImage paymentImage = paymentImageRepository.GetPaymentImageBySaleNetId(clientInDebts[i].Sale.NetUid);
                        paymentImage.RetailClientPaymentImageItems.ForEach(e => e.IsLocked = true);

                        paymentImageItemRepository.Update(paymentImage.RetailClientPaymentImageItems);

                        retailPaymentStatusRepository.Update(retailPaymentStatus);
                    }

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Overpaid, clientInDebts[i].Sale.BaseSalePaymentStatusId);
                }

                if (clientInDebts[i].SaleId.HasValue) {
                    saleRepository.UpdateIsAcceptedToPacking(clientInDebts[i].Sale.Id, true);

                    saleRepository.UnlockSaleById(clientInDebts[i].Sale.Id);
                }

                clientInDebtRepository.Remove(clientInDebts[i].NetUid);

                incomePaymentOrderRepository.UpdateOverpaidAmountById(incomePaymentOrder.Id, incomePaymentOrder.OverpaidAmount);

                ClientAgreement clientAgreement =
                    incomePaymentOrder.ClientAgreement != null && !incomePaymentOrder.ClientAgreement.IsNew()
                        ? clientAgreementRepository.GetByIdWithoutIncludes(incomePaymentOrder.ClientAgreement.Id)
                        : clientAgreementRepository.GetByClientAndAgreementIds(clientInDebts[i].ClientId, clientInDebts[i].AgreementId);

                if (!clientInDebts[i].Sale.RetailClientId.HasValue) {
                    clientAgreement.CurrentAmount =
                        decimal.Round(clientAgreement.CurrentAmount + incomePaymentOrder.OverpaidAmount, 4, MidpointRounding.AwayFromZero);

                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                }

                clientBalanceMovementRepository
                    .AddOutMovement(
                        new ClientBalanceMovement {
                            ClientAgreementId = clientAgreement.Id,
                            Amount = incomePaymentOrder.OverpaidAmount,
                            ExchangeRateAmount = euroExchangeRateAmount
                        }
                    );

                incomePaymentOrder.ClientAgreementId = clientAgreement.Id;

                incomePaymentOrderRepository.UpdateAgreementId(incomePaymentOrder);
            } else {
                //Re-Check
                amountInDebtCurrency = decimal.Round(amountInDebtCurrency - clientInDebts[i].Debt.Total, 4, MidpointRounding.AwayFromZero);

                if (clientInDebts[i].ReSaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(clientInDebts[i].Debt.Total / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        ReSaleId = clientInDebts[i].ReSaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = 0m
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, clientInDebts[i].ReSale.BaseSalePaymentStatusId);
                } else if (clientInDebts[i].SaleId.HasValue) {
                    incomePaymentOrderSaleRepository.Add(new IncomePaymentOrderSale {
                        Amount = decimal.Round(clientInDebts[i].Debt.Total / euroExchangeRateAmount, 4, MidpointRounding.AwayFromZero),
                        SaleId = clientInDebts[i].SaleId.Value,
                        IncomePaymentOrderId = incomePaymentOrder.Id,
                        ExchangeRate = euroExchangeRateAmount,
                        OverpaidAmount = 0m
                    });

                    salePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, clientInDebts[i].Sale.BaseSalePaymentStatusId);

                    saleRepository.UpdateIsAcceptedToPacking(clientInDebts[i].Sale.Id, true);

                    saleRepository.UnlockSaleById(clientInDebts[i].Sale.Id);
                }

                clientInDebtRepository.Remove(clientInDebts[i].NetUid);

                if (clientInDebts[i].Agreement.CurrencyId.Equals(incomePaymentOrder.CurrencyId))
                    incomePaymentOrder.Amount = amountInDebtCurrency;
                else
                    incomePaymentOrder.Amount = incomePaymentOrder.ExchangeRate < decimal.Zero
                        ? decimal.Round(amountInDebtCurrency * (decimal.Zero - incomePaymentOrder.ExchangeRate), 2, MidpointRounding.AwayFromZero)
                        : decimal.Round(amountInDebtCurrency / incomePaymentOrder.ExchangeRate, 2, MidpointRounding.AwayFromZero);
            }
        }
    }

    private void UnlockNextUnpaidInvoiceWithEnoughAgreementDebtDays(
        IncomePaymentOrder incomePaymentOrder,
        ISaleRepository saleRepository,
        IClientInDebtRepository clientInDebtRepository) {
        int clientDebtDaysDelay = incomePaymentOrder.ClientAgreement.Agreement.NumberDaysDebt;

        List<ClientInDebt> activeDebts = clientInDebtRepository.GetAllActiveByClientAgreementId(incomePaymentOrder.ClientAgreementId.Value);

        foreach (ClientInDebt clientInDebt in activeDebts)
            if (DateTime.Now.Subtract(clientInDebt.Created).Days < clientDebtDaysDelay) {
                saleRepository.UnlockSaleById(clientInDebt.SaleId.Value);
                saleRepository.UpdateIsAcceptedToPacking(clientInDebt.SaleId.Value, true);
            }
    }


    private void ProcessAddNewIncomePaymentOrderWithoutAgreement(AddNewIncomePaymentOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIncomePaymentOrderRepository incomePaymentOrderRepository = _paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection);
        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
        IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        PaymentRegister register = paymentRegisterRepository
            .GetById(
                message.IncomePaymentOrder.PaymentRegisterId.Equals(0) && message.IncomePaymentOrder.PaymentRegister != null
                    ? message.IncomePaymentOrder.PaymentRegister.Id
                    : message.IncomePaymentOrder.PaymentRegisterId
            );

        message.IncomePaymentOrder.PaymentRegisterId = register.Id;

        PaymentCurrencyRegister paymentCurrencyRegister =
            register.PaymentCurrencyRegisters.First(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId));

        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount + message.IncomePaymentOrder.Amount, 2);

        _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).UpdateAmount(paymentCurrencyRegister);

        if (!register.PaymentCurrencyRegisters.Any(r => r.CurrencyId.Equals(message.IncomePaymentOrder.CurrencyId))) {
            Sender.Tell(new Tuple<IncomePaymentOrder, string>(null, IncomePaymentOrderResourceNames.PAYMENT_CURRENCY_REGISTER_NOT_EXISTS));
        } else {
            if (message.IncomePaymentOrder.ClientId.HasValue) {
                if (string.IsNullOrEmpty(message.IncomePaymentOrder.Number)) {
                    IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister!.Type);

                    if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                        message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                    else
                        message.IncomePaymentOrder.Number =
                            message.IncomePaymentOrder.Organization!.Code +
                            string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);
                }

                message.IncomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                if (message.IncomePaymentOrder.Currency == null)
                    message.IncomePaymentOrder.Currency =
                        _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetById(message.IncomePaymentOrder.CurrencyId);

                Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                    euroCurrency,
                    message.IncomePaymentOrder.Currency,
                    message.IncomePaymentOrder.FromDate,
                    exchangeRateRepository,
                    crossExchangeRateRepository);

                message.IncomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.IncomePaymentOrder.Amount);

                message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

                if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
                    (!message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                     message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                   ) {
                    if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                        message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                    message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
                }
            } else if (message.IncomePaymentOrder.SupplyOrganizationId.HasValue) {
                if (message.IncomePaymentOrder.Currency == null)
                    message.IncomePaymentOrder.Currency =
                        _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetById(message.IncomePaymentOrder.CurrencyId);

                Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                    euroCurrency,
                    message.IncomePaymentOrder.Currency,
                    message.IncomePaymentOrder.FromDate,
                    exchangeRateRepository,
                    crossExchangeRateRepository);

                message.IncomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(message.IncomePaymentOrder.Amount);

                if (string.IsNullOrEmpty(message.IncomePaymentOrder.Number)) {
                    IncomePaymentOrder lastRecord = incomePaymentOrderRepository.GetLastRecord(message.IncomePaymentOrder.PaymentRegister!.Type);

                    if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                        message.IncomePaymentOrder.Number = message.IncomePaymentOrder.Organization!.Code + string.Format("{0:D10}", 1);
                    else
                        message.IncomePaymentOrder.Number =
                            message.IncomePaymentOrder.Organization!.Code +
                            string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastRecord.Number, @"(\d+)").Value) + 1);
                }

                message.IncomePaymentOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                message.IncomePaymentOrder.Id = incomePaymentOrderRepository.Add(message.IncomePaymentOrder);

                if (message.IncomePaymentOrder.PaymentMovementOperation != null &&
                    (!message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                     message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                   ) {
                    if (message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
                        message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                            message.IncomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

                    message.IncomePaymentOrder.PaymentMovementOperation.IncomePaymentOrderId = message.IncomePaymentOrder.Id;

                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(connection).Add(message.IncomePaymentOrder.PaymentMovementOperation);
                }
            }
        }

        Sender.Tell(new Tuple<IncomePaymentOrder, string>(incomePaymentOrderRepository.GetById(message.IncomePaymentOrder.Id), string.Empty));
    }
}