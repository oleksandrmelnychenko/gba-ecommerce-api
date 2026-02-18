using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.ExchangeRates.GovCrossExchangeRates;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Services.Actors.CrossExchangeRates.GovCrossExchangeRatesGetActors;

public sealed class BaseGovCrossExchangeRatesGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;

    public BaseGovCrossExchangeRatesGetActor(
        IDbConnectionFactory connectionFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetAllGovCrossExchangeRatesMessage>(ProcessGetAllGovCrossExchangeRatesMessage);

        Receive<GetGovCrossExchangeRateToBaseByCurrencyIdMessage>(ProcessGetGovCrossExchangeRateToBaseByCurrencyIdMessage);

        Receive<GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdMessage>(ProcessGetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdMessage);

        Receive<GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdsMessage>(ProcessGetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdsMessage);
    }

    private void ProcessGetAllGovCrossExchangeRatesMessage(GetAllGovCrossExchangeRatesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection).GetAll());
    }

    private void ProcessGetGovCrossExchangeRateToBaseByCurrencyIdMessage(GetGovCrossExchangeRateToBaseByCurrencyIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Currency baseCurrency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetBase();

        Sender.Tell(
            _exchangeRateRepositoriesFactory
                .NewGovCrossExchangeRateRepository(connection)
                .GetByCurrenciesIds(
                    message.CurrencyId,
                    baseCurrency.Id
                )
        );
    }

    private void ProcessGetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdMessage(
        GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Limit <= 0) message.Limit = 10;
        if (message.Offset <= 0) message.Offset = 0;

        Sender.Tell(
            _exchangeRateRepositoriesFactory
                .NewGovCrossExchangeRateHistoryRepository(connection)
                .GetAllByGovCrossExchangeRateNetId(
                    message.NetId,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdsMessage(
        GetAllGovCrossExchangeRateHistoriesByGovCrossExchangeRateNetIdsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
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

        Sender.Tell(
            _exchangeRateRepositoriesFactory
                .NewGovCrossExchangeRateHistoryRepository(connection)
                .GetAllByGovCrossExchangeRateNetIds(
                    message.GovCrossExchangeRateNetIds,
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To
                )
        );
    }
}