using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders.PaymentRegisterGetActors;

public sealed class BasePaymentRegisterGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BasePaymentRegisterGetActor(
        IDbConnectionFactory connectionFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetPaymentRegisterByNetIdMessage>(ProcessGetPaymentRegisterByNetIdMessage);

        Receive<GetAllIncomePaymentOrdersByRegisterNetIdMessage>(ProcessGetAllIncomePaymentOrdersByRegisterNetIdMessage);

        Receive<GetPaymentCurrencyRegisterByNetIdFilteredMessage>(ProcessGetPaymentCurrencyRegisterByNetIdFilteredMessage);

        Receive<GetAllPaymentRegistersMessage>(ProcessGetAllPaymentRegistersMessage);

        Receive<GetAllPaymentRegistersByBankMessage>(ProcessGetAllPaymentRegistersByBank);

        Receive<GetAllPaymentRegistersFromSearchMessage>(ProcessGetAllPaymentRegistersFromSearchMessage);

        Receive<GetAllPaymentRegistersIsForRetailMessage>(ProcessGetAllPaymentRegistersIsForRetailMessage);
    }

    private void ProcessGetPaymentRegisterByNetIdMessage(GetPaymentRegisterByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        PaymentRegister paymentRegister = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection).GetByNetId(message.NetId);

        if (paymentRegister.PaymentCurrencyRegisters.Any()) {
            Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

            if (euroCurrency != null)
                foreach (PaymentCurrencyRegister currencyRegister in paymentRegister.PaymentCurrencyRegisters)
                    if (currencyRegister.Currency.Code.ToLower().Equals("uah") || currencyRegister.Currency.Code.ToLower().Equals("pln")) {
                        ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection)
                            .GetByCurrencyIdAndCode(currencyRegister.Currency.Id, euroCurrency.Code);

                        decimal exchangeRateAmount = exchangeRate?.Amount ?? 1;

                        paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);
                    } else {
                        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                        CrossExchangeRate crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(currencyRegister.Currency.Id, euroCurrency.Id);

                        if (crossExchangeRate != null) {
                            decimal exchangeRateAmount = crossExchangeRate.Amount;

                            paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount * exchangeRateAmount, 2);
                        } else {
                            crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(euroCurrency.Id, currencyRegister.Currency.Id);

                            decimal exchangeRateAmount = crossExchangeRate?.Amount ?? 1;

                            paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);
                        }
                    }
        }

        Sender.Tell(paymentRegister);
    }

    private void ProcessGetAllIncomePaymentOrdersByRegisterNetIdMessage(GetAllIncomePaymentOrdersByRegisterNetIdMessage message) {
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset < 0) message.Offset = 0;
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        PaymentRegister paymentRegister = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection).GetByNetIdWithoutIncludes(message.RegisterNetId);

        if (paymentRegister != null)
            Sender.Tell(
                _paymentOrderRepositoriesFactory
                    .NewIncomePaymentOrderRepository(connection)
                    .GetAll(
                        paymentRegister.Id,
                        message.Limit,
                        message.Offset,
                        message.From,
                        message.To,
                        message.Value,
                        message.CurrencyNetId
                    )
            );
        else
            Sender.Tell(new List<IncomePaymentOrder>());
    }

    private void ProcessGetPaymentCurrencyRegisterByNetIdFilteredMessage(GetPaymentCurrencyRegisterByNetIdFilteredMessage message) {
        message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).GetByNetIdFiltered(message.NetId, message.From, message.To));
    }

    private void ProcessGetAllPaymentRegistersMessage(GetAllPaymentRegistersMessage message) {
        try {
            if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<PaymentRegister> paymentRegisters =
                _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection).GetAll(message.Type, message.Value, message.OrganizationNetId);

            decimal totalEuroAmount = decimal.Zero;

            foreach (PaymentRegister paymentRegister in paymentRegisters) {
                if (!paymentRegister.PaymentCurrencyRegisters.Any()) continue;

                Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                if (euroCurrency == null) continue;

                foreach (PaymentCurrencyRegister currencyRegister in paymentRegister.PaymentCurrencyRegisters)
                    if (string.IsNullOrEmpty(currencyRegister.Currency.Code)
                        || currencyRegister.Currency.Code.ToLower().Equals("uah")
                        || currencyRegister.Currency.Code.ToLower().Equals("pln")) {
                        ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection)
                            .GetByCurrencyIdAndCode(currencyRegister.Currency.Id, euroCurrency.Code);

                        decimal exchangeRateAmount = exchangeRate?.Amount ?? 1;

                        paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);

                        totalEuroAmount = Math.Round(totalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);
                    } else {
                        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                        CrossExchangeRate crossExchangeRate =
                            crossExchangeRateRepository.GetByCurrenciesIds(currencyRegister.Currency.Id, euroCurrency.Id);

                        if (crossExchangeRate != null) {
                            decimal exchangeRateAmount = crossExchangeRate.Amount;

                            paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount * exchangeRateAmount, 2);

                            totalEuroAmount = Math.Round(totalEuroAmount + currencyRegister.Amount * exchangeRateAmount, 2);
                        } else {
                            crossExchangeRate =
                                crossExchangeRateRepository.GetByCurrenciesIds(euroCurrency.Id, currencyRegister.Currency.Id);

                            decimal exchangeRateAmount = crossExchangeRate?.Amount ?? 1;

                            paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);

                            totalEuroAmount = Math.Round(totalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);
                        }
                    }
            }

            Sender.Tell(new Tuple<List<PaymentRegister>, decimal>(paymentRegisters, totalEuroAmount));
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    private void ProcessGetAllPaymentRegistersFromSearchMessage(GetAllPaymentRegistersFromSearchMessage message) {
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection).GetAllFromSearch(message.Value));
    }

    private void ProcessGetAllPaymentRegistersIsForRetailMessage(GetAllPaymentRegistersIsForRetailMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        List<PaymentRegister> paymentRegisters = paymentRegisterRepository.GetAllForRetail(message.OrganizationNetUid);

        decimal totalEuroAmount = decimal.Zero;

        foreach (PaymentRegister paymentRegister in paymentRegisters) {
            if (!paymentRegister.PaymentCurrencyRegisters.Any()) continue;

            Currency euroCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

            if (euroCurrency == null) continue;

            foreach (PaymentCurrencyRegister currencyRegister in paymentRegister.PaymentCurrencyRegisters)
                if (string.IsNullOrEmpty(currencyRegister.Currency.Code)
                    || currencyRegister.Currency.Code.ToLower().Equals("uah")
                    || currencyRegister.Currency.Code.ToLower().Equals("pln")) {
                    ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(currencyRegister.Currency.Id, euroCurrency.Code);

                    decimal exchangeRateAmount = exchangeRate?.Amount ?? 1;

                    paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);

                    totalEuroAmount = Math.Round(totalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);
                } else {
                    ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                    CrossExchangeRate crossExchangeRate =
                        crossExchangeRateRepository.GetByCurrenciesIds(currencyRegister.Currency.Id, euroCurrency.Id);

                    if (crossExchangeRate != null) {
                        decimal exchangeRateAmount = crossExchangeRate.Amount;

                        paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount * exchangeRateAmount, 2);

                        totalEuroAmount = Math.Round(totalEuroAmount + currencyRegister.Amount * exchangeRateAmount, 2);
                    } else {
                        crossExchangeRate =
                            crossExchangeRateRepository.GetByCurrenciesIds(euroCurrency.Id, currencyRegister.Currency.Id);

                        decimal exchangeRateAmount = crossExchangeRate?.Amount ?? 1;

                        paymentRegister.TotalEuroAmount = Math.Round(paymentRegister.TotalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);

                        totalEuroAmount = Math.Round(totalEuroAmount + currencyRegister.Amount / exchangeRateAmount, 2);
                    }
                }
        }

        Sender.Tell(new Tuple<List<PaymentRegister>, decimal>(paymentRegisters, totalEuroAmount));
    }

    public void ProcessGetAllPaymentRegistersByBank(GetAllPaymentRegistersByBankMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPaymentRegisterRepository paymentRegisterRepository =
            _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

        PaymentRegister paymentRegister = paymentRegisterRepository.GetByNetId(message.PaymentRegisterId);

        Sender.Tell(paymentRegisterRepository.GetAllByBank(paymentRegister.BankName));
    }
}