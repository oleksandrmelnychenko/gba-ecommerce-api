using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.ExchangeRates;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Services.Actors.CrossExchangeRates.CrossExchangeRatesGetActors;

public sealed class BaseCrossExchangeRatesGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;

    public BaseCrossExchangeRatesGetActor(
        IDbConnectionFactory connectionFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetAllCrossExchangeRatesMessage>(ProcessGetAllCrossExchangeRatesMessage);

        Receive<GetCrossExchangeRateToBaseByCurrencyIdMessage>(ProcessGetCrossExchangeRateToBaseByCurrencyIdMessage);

        Receive<GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdMessage>(ProcessGetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdMessage);

        Receive<GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdsMessage>(ProcessGetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdsMessage);
    }

    private void ProcessGetAllCrossExchangeRatesMessage(GetAllCrossExchangeRatesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection).GetAll());
    }

    private void ProcessGetCrossExchangeRateToBaseByCurrencyIdMessage(GetCrossExchangeRateToBaseByCurrencyIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Currency baseCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetBase();

        Sender.Tell(_exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection).GetByCurrenciesIds(message.CurrencyId, baseCurrency.Id));
    }

    private void ProcessGetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdMessage(GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdMessage message) {
        if (message.Limit <= 0) message.Limit = 10;
        if (message.Offset <= 0) message.Offset = 0;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _exchangeRateRepositoriesFactory
                .NewCrossExchangeRateHistoryRepository(connection)
                .GetAllByCrossExchangeRateNetId(
                    message.NetId,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdsMessage(GetAllCrossExchangeRateHistoriesByCrossExchangeRateNetIdsMessage message) {
        if (message.Limit <= 0) message.Limit = 10;
        if (message.Offset <= 0) message.Offset = 0;

        message.From =
            message.From.Year.Equals(1)
                ? DateTime.Now.Date
                : message.From.Date;

        message.To =
            message.From.Year.Equals(1)
                ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                : message.To.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _exchangeRateRepositoriesFactory
                .NewCrossExchangeRateHistoryRepository(connection)
                .GetAllByCrossExchangeRateNetIds(
                    message.CrossExchangeRateNetIds,
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To
                )
        );
    }
}