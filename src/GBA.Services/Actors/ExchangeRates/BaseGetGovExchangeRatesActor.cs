using System;
using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Messages.ExchangeRates.GovExchangeRates;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Services.Actors.ExchangeRates;

public sealed class BaseGetGovExchangeRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;

    public BaseGetGovExchangeRatesActor(
        IDbConnectionFactory connectionFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<GetGovExchangeRateByCultureMessage>(ProcessGetGovExchangeRateByCultureMessage);

        Receive<GetGovExchangeRateByNetIdMessage>(ProcessGetGovExchangeRateByNetIdMessage);

        Receive<GetGovExchangeRateBySpecificDateAndTimeMessage>(ProcessGetGovExchangeRateBySpecificDateAndTimeMessage);

        Receive<GetAllGovExchangeRateHistoriesByExchangeRateNetIdMessage>(ProcessGetAllGovExchangeRateHistoriesByExchangeRateNetIdMessage);

        Receive<GetAllGovExchangeRateHistoriesByExchangeRateNetIdsMessage>(ProcessGetAllGovExchangeRateHistoriesByExchangeRateNetIdsMessage);
    }

    private void ProcessGetGovExchangeRateByCultureMessage(GetGovExchangeRateByCultureMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
            ? _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection).GetAllByCulture()
            : _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection).GetAll());
    }

    private void ProcessGetGovExchangeRateByNetIdMessage(GetGovExchangeRateByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetGovExchangeRateBySpecificDateAndTimeMessage(GetGovExchangeRateBySpecificDateAndTimeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        Currency fromCurrency = currencyRepository.GetByNetId(message.FromCurrencyNetId);

        Currency toCurrency = message.ToCurrencyNetId.Equals(Guid.Empty)
            ? currencyRepository.GetEURCurrencyIfExists()
            : currencyRepository.GetByNetId(message.ToCurrencyNetId);

        if (fromCurrency.Id.Equals(toCurrency.Id)) {
            Sender.Tell((decimal)1);
        } else {
            message.FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(message.FromDate);

            decimal exchangeRateAmount;

            if (fromCurrency.Code.ToLower().Equals("uah") || fromCurrency.Code.ToLower().Equals("pln")) {
                IGovExchangeRateRepository govExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
                IGovExchangeRateHistoryRepository govExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection);

                GovExchangeRate exchangeRate = govExchangeRateRepository.GetByCurrencyIdAndCode(fromCurrency.Id, toCurrency.Code);

                if (exchangeRate == null) {
                    exchangeRateAmount = 1;
                } else {
                    GovExchangeRateHistory history = govExchangeRateHistoryRepository.GetLatestNearToDateByExchangeRateNetId(exchangeRate.NetUid, message.FromDate);

                    if (history != null) {
                        exchangeRateAmount = history.Amount;
                    } else {
                        history = govExchangeRateHistoryRepository.GetLatestByExchangeRateNetId(exchangeRate.NetUid);

                        exchangeRateAmount = history?.Amount ?? exchangeRate.Amount;
                    }
                }
            } else if (toCurrency.Code.ToLower().Equals("uah") || toCurrency.Code.ToLower().Equals("pln")) {
                IGovExchangeRateRepository govExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
                IGovExchangeRateHistoryRepository govExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection);

                GovExchangeRate exchangeRate = govExchangeRateRepository.GetByCurrencyIdAndCode(toCurrency.Id, fromCurrency.Code);

                if (exchangeRate == null) {
                    exchangeRateAmount = 1;
                } else {
                    GovExchangeRateHistory history = govExchangeRateHistoryRepository.GetLatestNearToDateByExchangeRateNetId(exchangeRate.NetUid, message.FromDate);

                    if (history != null) {
                        exchangeRateAmount = history.Amount;
                    } else {
                        history = govExchangeRateHistoryRepository.GetLatestByExchangeRateNetId(exchangeRate.NetUid);

                        exchangeRateAmount = history?.Amount ?? exchangeRate.Amount;
                    }
                }
            } else {
                IGovCrossExchangeRateRepository govCrossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
                IGovCrossExchangeRateHistoryRepository govCrossExchangeRateHistoryRepository =
                    _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateHistoryRepository(connection);

                GovCrossExchangeRate crossExchangeRate = govCrossExchangeRateRepository.GetByCurrenciesIds(fromCurrency.Id, toCurrency.Id);

                if (crossExchangeRate != null) {
                    exchangeRateAmount = crossExchangeRate.Amount;
                } else {
                    crossExchangeRate = govCrossExchangeRateRepository.GetByCurrenciesIds(toCurrency.Id, fromCurrency.Id);

                    if (crossExchangeRate == null) {
                        exchangeRateAmount = 1;
                    } else {
                        GovCrossExchangeRateHistory history =
                            govCrossExchangeRateHistoryRepository.GetLatestNearToDateByCrossExchangeRateNetId(crossExchangeRate.NetUid, message.FromDate);

                        if (history != null) {
                            exchangeRateAmount = history.Amount;
                        } else {
                            history = govCrossExchangeRateHistoryRepository.GetLatestByCrossExchangeRateNetId(crossExchangeRate.NetUid);

                            exchangeRateAmount = history?.Amount ?? crossExchangeRate.Amount;
                        }
                    }
                }
            }

            Sender.Tell(exchangeRateAmount);
        }
    }

    private void ProcessGetAllGovExchangeRateHistoriesByExchangeRateNetIdMessage(GetAllGovExchangeRateHistoriesByExchangeRateNetIdMessage message) {
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset <= 0) message.Offset = 0;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection)
            .GetAllByExchangeRateNetId(message.ExchangeRateNetId, message.Limit, message.Offset));
    }

    private void ProcessGetAllGovExchangeRateHistoriesByExchangeRateNetIdsMessage(GetAllGovExchangeRateHistoriesByExchangeRateNetIdsMessage message) {
        if (message.Limit <= 0) message.Limit = 20;
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
                .NewGovExchangeRateHistoryRepository(connection)
                .GetAllByExchangeRateNetIds(
                    message.GovExchangeRateNetIds,
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To
                )
        );
    }
}