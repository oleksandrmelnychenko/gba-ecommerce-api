using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using static GBA.Common.Helpers.DateTimeHelper;

namespace GBA.Services.Actors.PaymentOrders.IncomePaymentOrderGetActors;

public sealed class BaseIncomePaymentOrderGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BaseIncomePaymentOrderGetActor(
        IDbConnectionFactory connectionFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<GetAllIncomePaymentOrdersMessage>(ProcessGetAllIncomePaymentOrdersMessage);

        Receive<GetIncomePaymentOrderByNetIdMessage>(ProcessGetIncomePaymentOrderByNetIdMessage);

        Receive<CalculateConvertedAmountMessage>(ProcessCalculateConvertedAmountMessage);
    }

    private void ProcessGetAllIncomePaymentOrdersMessage(GetAllIncomePaymentOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset < 0) message.Offset = 0;
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        message.To = ConvertDateTimeToUtcInUkraineTimeZone(message.To.AddHours(23).AddMinutes(59).AddSeconds(59));
        message.From = ConvertDateTimeToUtcInUkraineTimeZone(message.From.Date);

        Sender.Tell(
            _paymentOrderRepositoriesFactory
                .NewIncomePaymentOrderRepository(connection)
                .GetAll(
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To,
                    message.Value,
                    message.CurrencyNetId,
                    message.RegisterNetId,
                    message.OrganizationIds
                )
        );
    }

    private void ProcessGetIncomePaymentOrderByNetIdMessage(GetIncomePaymentOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewIncomePaymentOrderRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessCalculateConvertedAmountMessage(CalculateConvertedAmountMessage message) {
        if (message.FromCurrencyId.Equals(0)) {
            Sender.Tell(decimal.Zero);
        } else if (message.ToCurrencyId.Equals(0)) {
            Sender.Tell(decimal.Zero);
        } else if (message.Amount <= decimal.Zero) {
            Sender.Tell(decimal.Zero);
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (!message.FromCurrencyId.Equals(message.ToCurrencyId)) {
                ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

                Currency fromCurrency = currencyRepository.GetById(message.FromCurrencyId);
                Currency toCurrency = currencyRepository.GetById(message.ToCurrencyId);

                if (message.ExchangeRate <= decimal.Zero) {
                    if (fromCurrency == null || toCurrency == null) {
                        Sender.Tell(message.Amount);
                    } else {
                        if (fromCurrency.Code.ToLower().Equals("uah") || fromCurrency.Code.ToLower().Equals("pln")) {
                            ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection)
                                .GetByCurrencyIdAndCode(fromCurrency.Id, toCurrency.Code);

                            message.ExchangeRate = exchangeRate?.Amount ?? 1;

                            Sender.Tell(Math.Round(message.Amount / message.ExchangeRate, 2));
                        } else {
                            ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

                            CrossExchangeRate crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(fromCurrency.Id, toCurrency.Id);

                            if (crossExchangeRate != null) {
                                message.ExchangeRate = crossExchangeRate.Amount;

                                Sender.Tell(Math.Round(message.Amount * message.ExchangeRate, 2));
                            } else {
                                crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(toCurrency.Id, fromCurrency.Id);

                                message.ExchangeRate = crossExchangeRate?.Amount ?? 1;

                                Sender.Tell(Math.Round(message.Amount / message.ExchangeRate, 2));
                            }
                        }
                    }
                } else {
                    switch (fromCurrency.Code.ToLower()) {
                        case "usd" when toCurrency.Code.ToLower().Equals("eur"):
                        case "uah":
                        case "pln" when !toCurrency.Code.ToLower().Equals("uah"):
                            Sender.Tell(Math.Round(message.Amount / message.ExchangeRate, 2));
                            break;
                        default:
                            Sender.Tell(Math.Round(message.Amount * message.ExchangeRate, 2));
                            break;
                    }
                }
            } else {
                Sender.Tell(message.Amount);
            }
        }
    }
}