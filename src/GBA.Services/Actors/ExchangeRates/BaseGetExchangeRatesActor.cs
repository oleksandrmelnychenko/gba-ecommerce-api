using System;
using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Messages.ExchangeRates;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Services.Actors.ExchangeRates;

public sealed class BaseGetExchangeRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;

    public BaseGetExchangeRatesActor(
        IDbConnectionFactory connectionFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<GetExchangeRateByCultureMessage>(ProcessGetExchangeRateByCultureMessage);

        Receive<GetExchangeRateByNetIdMessage>(ProcessGetExchangeRateByNetIdMessage);

        Receive<GetExchangeRateBySpecificDateAndTimeMessage>(ProcessGetExchangeRateBySpecificDateAndTimeMessage);

        Receive<GetAllExchangeRateHistoriesByExchangeRateNetIdMessage>(ProcessGetAllExchangeRateHistoriesByExchangeRateNetIdMessage);

        Receive<GetAllExchangeRateHistoriesByExchangeRateNetIdsMessage>(ProcessGetAllExchangeRateHistoriesByExchangeRateNetIdsMessage);
    }

    private void ProcessGetExchangeRateByCultureMessage(GetExchangeRateByCultureMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
            ? _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetAllByCulture()
            : _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetAll());
    }

    private void ProcessGetExchangeRateByNetIdMessage(GetExchangeRateByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetExchangeRateBySpecificDateAndTimeMessage(GetExchangeRateBySpecificDateAndTimeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        Currency fromCurrency = currencyRepository.GetByNetId(message.FromCurrencyNetId);

        Currency toCurrency = message.ToCurrencyNetId.Equals(Guid.Empty)
            ? currencyRepository.GetEURCurrencyIfExists()
            : currencyRepository.GetByNetId(message.ToCurrencyNetId);

        if (fromCurrency == null) {
            Sender.Tell(0);
            return;
        }

        if (fromCurrency.Id.Equals(toCurrency.Id)) {
            Sender.Tell((decimal)1);
        } else {
            message.FromDate = message.FromDate.Year.Equals(1) ? DateTime.UtcNow : TimeZoneInfo.ConvertTimeToUtc(message.FromDate);

            decimal exchangeRateAmount;

            if (fromCurrency.Code.ToLower().Equals("uah") || fromCurrency.Code.ToLower().Equals("pln")) {
                IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                IExchangeRateHistoryRepository exchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewExchangeRateHistoryRepository(connection);

                ExchangeRate exchangeRate = exchangeRateRepository.GetByCurrencyIdAndCode(fromCurrency.Id, toCurrency.Code);

                if (exchangeRate == null) {
                    exchangeRate = exchangeRateRepository.GetByCurrencyIdAndCode(toCurrency.Id, fromCurrency.Code);

                    if (exchangeRate == null) {
                        exchangeRateAmount = 1m;
                    } else {
                        ExchangeRateHistory history = exchangeRateHistoryRepository.GetLatestNearToDateByExchangeRateNetId(exchangeRate.NetUid, message.FromDate);

                        if (history != null) {
                            exchangeRateAmount = history.Amount;
                        } else {
                            history = exchangeRateHistoryRepository.GetLatestByExchangeRateNetId(exchangeRate.NetUid);

                            exchangeRateAmount = history?.Amount ?? exchangeRate.Amount;
                        }
                    }
                } else {
                    ExchangeRateHistory history = exchangeRateHistoryRepository.GetLatestNearToDateByExchangeRateNetId(exchangeRate.NetUid, message.FromDate);

                    if (history != null) {
                        exchangeRateAmount = history.Amount;
                    } else {
                        history = exchangeRateHistoryRepository.GetLatestByExchangeRateNetId(exchangeRate.NetUid);

                        exchangeRateAmount = history?.Amount ?? exchangeRate.Amount;
                    }
                }
            } else if (toCurrency.Code.ToLower().Equals("uah") || toCurrency.Code.ToLower().Equals("pln")) {
                IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                IExchangeRateHistoryRepository exchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewExchangeRateHistoryRepository(connection);

                ExchangeRate exchangeRate = exchangeRateRepository.GetByCurrencyIdAndCode(fromCurrency.Id, toCurrency.Code);

                if (exchangeRate == null) {
                    exchangeRate = exchangeRateRepository.GetByCurrencyIdAndCode(toCurrency.Id, fromCurrency.Code);

                    if (exchangeRate == null) {
                        exchangeRateAmount = 1m;
                    } else {
                        ExchangeRateHistory history = exchangeRateHistoryRepository.GetLatestNearToDateByExchangeRateNetId(exchangeRate.NetUid, message.FromDate);

                        if (history != null) {
                            exchangeRateAmount = history.Amount;
                        } else {
                            history = exchangeRateHistoryRepository.GetLatestByExchangeRateNetId(exchangeRate.NetUid);

                            exchangeRateAmount = history?.Amount ?? exchangeRate.Amount;
                        }
                    }
                } else {
                    ExchangeRateHistory history = exchangeRateHistoryRepository.GetLatestNearToDateByExchangeRateNetId(exchangeRate.NetUid, message.FromDate);

                    if (history != null) {
                        exchangeRateAmount = history.Amount;
                    } else {
                        history = exchangeRateHistoryRepository.GetLatestByExchangeRateNetId(exchangeRate.NetUid);

                        exchangeRateAmount = history?.Amount ?? exchangeRate.Amount;
                    }
                }
            } else {
                ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
                ICrossExchangeRateHistoryRepository crossExchangeRateHistoryRepository =
                    _exchangeRateRepositoriesFactory.NewCrossExchangeRateHistoryRepository(connection);

                CrossExchangeRate crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(fromCurrency.Id, toCurrency.Id);

                if (crossExchangeRate != null) {
                    exchangeRateAmount = crossExchangeRate.Amount;
                } else {
                    crossExchangeRate = crossExchangeRateRepository.GetByCurrenciesIds(toCurrency.Id, fromCurrency.Id);

                    if (crossExchangeRate == null) {
                        exchangeRateAmount = 1;
                    } else {
                        CrossExchangeRateHistory history =
                            crossExchangeRateHistoryRepository.GetLatestNearToDateByCrossExchangeRateNetId(crossExchangeRate.NetUid, message.FromDate);

                        if (history != null) {
                            exchangeRateAmount = history.Amount;
                        } else {
                            history = crossExchangeRateHistoryRepository.GetLatestByCrossExchangeRateNetId(crossExchangeRate.NetUid);

                            exchangeRateAmount = history?.Amount ?? crossExchangeRate.Amount;
                        }
                    }
                }
            }

            Sender.Tell(exchangeRateAmount);
        }
    }

    private void ProcessGetAllExchangeRateHistoriesByExchangeRateNetIdMessage(GetAllExchangeRateHistoriesByExchangeRateNetIdMessage message) {
        if (message.Limit <= 0) message.Limit = 20;
        if (message.Offset <= 0) message.Offset = 0;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_exchangeRateRepositoriesFactory.NewExchangeRateHistoryRepository(connection)
            .GetAllByExchangeRateNetId(message.ExchangeRateNetId, message.Limit, message.Offset));
    }

    private void ProcessGetAllExchangeRateHistoriesByExchangeRateNetIdsMessage(GetAllExchangeRateHistoriesByExchangeRateNetIdsMessage message) {
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
                .NewExchangeRateHistoryRepository(connection)
                .GetAllByExchangeRateNetIds(
                    message.ExchangeRateNetIds,
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To
                )
        );
    }
}