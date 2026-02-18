using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.EntityHelpers.ExchangeRateModels;
using GBA.Domain.Messages.ExchangeRates.GovExchangeRates;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.ExchangeRates;

public sealed class GovExchangeRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private Currency _eur;
    private Currency _uah;
    private Currency _usd;

    public GovExchangeRatesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<AddGovExchangeRateMessage>(ProcessAddGovExchangeRateMessage);

        Receive<UpdateGovExchangeRateMessage>(ProcessUpdateGovExchangeRateMessage);

        Receive<DeleteGovExchangeRateMessage>(ProcessDeleteGovExchangeRateMessage);
    }

    private void ProcessAddGovExchangeRateMessage(AddGovExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IGovExchangeRateRepository govExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
        IGovExchangeRateHistoryRepository govExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection);

        message.GovExchangeRate.Id = govExchangeRateRepository.Add(message.GovExchangeRate);

        govExchangeRateHistoryRepository.Add(new GovExchangeRateHistory {
            UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
            GovExchangeRateId = message.GovExchangeRate.Id,
            Amount = message.GovExchangeRate.Amount
        });

        message.GovExchangeRate = govExchangeRateRepository.GetById(message.GovExchangeRate.Id);

        message.GovExchangeRate.GovExchangeRateHistories = govExchangeRateHistoryRepository.GetAllByExchangeRateId(message.GovExchangeRate.Id, 10, 0).ToList();

        Sender.Tell(message.GovExchangeRate);
    }

    private void ProcessUpdateGovExchangeRateMessage(UpdateGovExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IGovExchangeRateRepository govExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
        IGovExchangeRateHistoryRepository govExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection);
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        List<GovExchangeRate> govExchangeRates = FilterExistingValues(message, govExchangeRateRepository);

        if (!govExchangeRates.Any()) {
            Sender.Tell(new GovExchangeRateAndCrossToReturnModel {
                GovExchangeRate = message.GovExchangeRates.FirstOrDefault(),
                GovCrossExchangeRates = govCrossExchangeRateRepository.GetAll()
            });
            return;
        }

        _uah = currencyRepository.GetUAHCurrencyIfExists();
        _eur = currencyRepository.GetEURCurrencyIfExists();
        _usd = currencyRepository.GetUSDCurrencyIfExists();

        UpdateGovExchangeRateValues(govExchangeRates, connection);
        SaveGovExchangeRateHistory(govExchangeRates, message.UserNetId, connection);

        GovCrossExchangeRate savedCrossExchangeRate = GetSavedGovCrossExchangeRateAmount(govExchangeRates, connection);

        if (savedCrossExchangeRate == null) {
            Sender.Tell(new GovExchangeRateAndCrossToReturnModel {
                GovExchangeRate = message.GovExchangeRates.FirstOrDefault(),
                GovCrossExchangeRates = govCrossExchangeRateRepository.GetAll()
            });
            return;
        }

        GovExchangeRate govExchangeRate = govExchangeRateRepository.GetByNetId(savedCrossExchangeRate.GovExchangeRate.NetUid);

        SaveGovCrossExchangeRateHistory(connection, savedCrossExchangeRate, message.UserNetId);

        govExchangeRate.GovExchangeRateHistories =
            govExchangeRateHistoryRepository.GetAllByExchangeRateId(govExchangeRate.Id, 10, 0).ToList();

        Sender.Tell(new GovExchangeRateAndCrossToReturnModel {
            GovExchangeRate = govExchangeRate,
            GovCrossExchangeRates = govCrossExchangeRateRepository.GetAll()
        });
    }

    private List<GovExchangeRate> FilterExistingValues(UpdateGovExchangeRateMessage message, IGovExchangeRateRepository govExchangeRateRepository) {
        List<GovExchangeRate> filtered = new();

        foreach (GovExchangeRate govExchangeRate in message.GovExchangeRates) {
            GovExchangeRate exchangeRateFromDb = govExchangeRateRepository.GetById(govExchangeRate.Id);

            if (exchangeRateFromDb == null) continue;

            if (!govExchangeRate.Amount.Equals(exchangeRateFromDb.Amount)) filtered.Add(govExchangeRate);
        }

        return filtered;
    }

    private void UpdateGovExchangeRateValues(List<GovExchangeRate> govExchangeRates, IDbConnection connection) {
        IGovExchangeRateRepository govExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
        IGovExchangeRateHistoryRepository govExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection);

        foreach (GovExchangeRate govExchangeRate in govExchangeRates) {
            GovExchangeRateHistory firstExchangeRate = govExchangeRateHistoryRepository.GetFirstByExchangeRateId(govExchangeRate.Id);
            if (govExchangeRate.Created > firstExchangeRate.Created)
                govExchangeRateRepository.Update(govExchangeRate);
        }
    }

    private GovCrossExchangeRate GetSavedGovCrossExchangeRateAmount(
        List<GovExchangeRate> govExchangeRates,
        IDbConnection connection) {
        IGovExchangeRateRepository govExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository =
            _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
        decimal crossAmount = 0;

        if (!govExchangeRates.Any(e => e.CurrencyId.Equals(_uah.Id)) ||
            !govExchangeRates.Any(e => e.Code.Equals(_eur.Code))
            && !govExchangeRates.Any(e => e.Code.Equals(_usd.Code))) return null;

        DateTime updated = DateTime.Now;
        GovCrossExchangeRate govCrossExchangeRateEurToUsd =
            govCrossExchangeRateRepository.GetByCurrenciesIds(_eur.Id, _usd.Id);

        GovExchangeRate eurGovExchangeRate = govExchangeRates.FirstOrDefault(e => e.Code.Equals(_eur.Code));
        GovExchangeRate usdGovExchangeRate = govExchangeRates.FirstOrDefault(e => e.Code.Equals(_usd.Code));

        if (eurGovExchangeRate != null) {
            GovExchangeRate lastExchangeRateUahToUsd =
                govExchangeRateRepository
                    .GetByCurrencyIdAndCode(_uah.Id, _usd.Code, eurGovExchangeRate.Created);

            updated = eurGovExchangeRate.Created;
            govCrossExchangeRateEurToUsd.GovExchangeRate = eurGovExchangeRate;

            if (lastExchangeRateUahToUsd != null) {
                if (usdGovExchangeRate != null)
                    crossAmount = eurGovExchangeRate.Amount / usdGovExchangeRate.Amount;
                else
                    crossAmount = eurGovExchangeRate.Amount / lastExchangeRateUahToUsd.Amount;
            } else {
                GovExchangeRate exchangeRateUahToUsd =
                    govExchangeRateRepository
                        .GetExchangeRateByCurrencyIdAndCode(_uah.Id, _usd.Code);

                crossAmount = exchangeRateUahToUsd?.Amount ?? 1m;
            }
        } else if (usdGovExchangeRate != null) {
            GovExchangeRate lastExchangeRateUahToEur =
                govExchangeRateRepository
                    .GetByCurrencyIdAndCode(_uah.Id, _eur.Code, usdGovExchangeRate.Created);

            updated = usdGovExchangeRate.Created;
            govCrossExchangeRateEurToUsd.GovExchangeRate = usdGovExchangeRate;

            if (lastExchangeRateUahToEur != null) {
                crossAmount = lastExchangeRateUahToEur.Amount / usdGovExchangeRate.Amount;
            } else {
                GovExchangeRate exchangeRateUahToEur =
                    govExchangeRateRepository
                        .GetExchangeRateByCurrencyIdAndCode(_uah.Id, _eur.Code);

                crossAmount = exchangeRateUahToEur?.Amount ?? 1m;
            }
        }

        govCrossExchangeRateEurToUsd.Amount = crossAmount;
        govCrossExchangeRateEurToUsd.Updated = updated;
        govCrossExchangeRateRepository.Update(govCrossExchangeRateEurToUsd);

        return govCrossExchangeRateEurToUsd;
    }

    private void SaveGovCrossExchangeRateHistory(
        IDbConnection connection,
        GovCrossExchangeRate govCrossExchangeRateEurToUsd,
        Guid userNetId) {
        IGovCrossExchangeRateHistoryRepository govCrossExchangeRateHistoryRepository =
            _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateHistoryRepository(connection);
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository =
            _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);

        GovCrossExchangeRateHistory lastGovCrossExchangeRateHistory =
            govCrossExchangeRateHistoryRepository.GetFirstByGovCrossExchangeRateId(govCrossExchangeRateEurToUsd.Id);

        if (lastGovCrossExchangeRateHistory == null) {
            long historyId = govCrossExchangeRateHistoryRepository.AddSpecific(new GovCrossExchangeRateHistory {
                UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(userNetId).Id,
                Amount = 1,
                GovCrossExchangeRateId = govCrossExchangeRateEurToUsd.Id,
                Created = TimeZoneInfo.ConvertTimeToUtc(govCrossExchangeRateEurToUsd.GovExchangeRate.Updated.AddMinutes(-1)),
                Updated = TimeZoneInfo.ConvertTimeToUtc(govCrossExchangeRateEurToUsd.GovExchangeRate.Updated.AddMinutes(-1))
            });

            lastGovCrossExchangeRateHistory = govCrossExchangeRateHistoryRepository.GetById(historyId);
        }

        if (govCrossExchangeRateEurToUsd.GovExchangeRate.Updated > lastGovCrossExchangeRateHistory.Created ||
            lastGovCrossExchangeRateHistory.Amount != govCrossExchangeRateEurToUsd.Amount) {
            govCrossExchangeRateEurToUsd.Amount = govCrossExchangeRateEurToUsd.Amount;
            govCrossExchangeRateEurToUsd.Updated = govCrossExchangeRateEurToUsd.GovExchangeRate.Updated;
            govCrossExchangeRateRepository.Update(govCrossExchangeRateEurToUsd);
        }

        govCrossExchangeRateHistoryRepository.AddSpecific(new GovCrossExchangeRateHistory {
            UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(userNetId).Id,
            Amount = govCrossExchangeRateEurToUsd.Amount,
            GovCrossExchangeRateId = govCrossExchangeRateEurToUsd.Id,
            Created = TimeZoneInfo.ConvertTimeToUtc(govCrossExchangeRateEurToUsd.GovExchangeRate.Updated),
            Updated = TimeZoneInfo.ConvertTimeToUtc(govCrossExchangeRateEurToUsd.GovExchangeRate.Updated)
        });
    }

    private void SaveGovExchangeRateHistory(List<GovExchangeRate> govExchangeRates, Guid userNetId, IDbConnection connection) {
        IGovExchangeRateHistoryRepository govExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewGovExchangeRateHistoryRepository(connection);

        foreach (GovExchangeRate govExchangeRate in govExchangeRates) {
            GovExchangeRateHistory firstExchangeRate = govExchangeRateHistoryRepository.GetFirstByExchangeRateId(govExchangeRate.Id);

            if (firstExchangeRate == null)
                govExchangeRateHistoryRepository.AddSpecific(new GovExchangeRateHistory {
                    UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(userNetId).Id,
                    GovExchangeRateId = govExchangeRate.Id,
                    Amount = govExchangeRate.Amount,
                    Created = TimeZoneInfo.ConvertTimeToUtc(govExchangeRate.Updated.AddMinutes(-1)),
                    Updated = TimeZoneInfo.ConvertTimeToUtc(govExchangeRate.Updated.AddMinutes(-1))
                });

            govExchangeRateHistoryRepository.AddSpecific(new GovExchangeRateHistory {
                UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(userNetId).Id,
                GovExchangeRateId = govExchangeRate.Id,
                Amount = govExchangeRate.Amount,
                Created = TimeZoneInfo.ConvertTimeToUtc(govExchangeRate.Updated),
                Updated = TimeZoneInfo.ConvertTimeToUtc(govExchangeRate.Updated)
            });
        }
    }

    private void ProcessDeleteGovExchangeRateMessage(DeleteGovExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection).Remove(message.NetId);
    }
}