using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Messages.ExchangeRates;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.ExchangeRates;

public sealed class ExchangeRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ExchangeRatesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<AddExchangeRateMessage>(ProcessAddExchangeRateMessage);

        Receive<UpdateExchangeRateMessage>(ProcessUpdateExchangeRateMessage);

        Receive<DeleteExchangeRateMessage>(ProcessDeleteExchangeRateMessage);
    }

    private void ProcessAddExchangeRateMessage(AddExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
        IExchangeRateHistoryRepository exchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewExchangeRateHistoryRepository(connection);

        message.ExchangeRate.Id = exchangeRateRepository.Add(message.ExchangeRate);

        exchangeRateHistoryRepository.Add(new ExchangeRateHistory {
            UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
            ExchangeRateId = message.ExchangeRate.Id,
            Amount = message.ExchangeRate.Amount
        });

        message.ExchangeRate = exchangeRateRepository.GetById(message.ExchangeRate.Id);

        message.ExchangeRate.ExchangeRateHistories = exchangeRateHistoryRepository.GetAllByExchangeRateId(message.ExchangeRate.Id, 10, 0).ToList();

        Sender.Tell(message.ExchangeRate);
    }

    private void ProcessUpdateExchangeRateMessage(UpdateExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
        IExchangeRateHistoryRepository exchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewExchangeRateHistoryRepository(connection);

        ExchangeRate exchangeRateFromDb = exchangeRateRepository.GetById(message.ExchangeRate.Id);

        if (exchangeRateFromDb != null)
            if (!message.ExchangeRate.Amount.Equals(exchangeRateFromDb.Amount)) {
                ExchangeRateHistory firstExchangeRate = exchangeRateHistoryRepository.GetFirstByExchangeRateId(message.ExchangeRate.Id);

                if (firstExchangeRate == null) {
                    long historyId = exchangeRateHistoryRepository.AddSpecific(new ExchangeRateHistory {
                        UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                        ExchangeRateId = message.ExchangeRate.Id,
                        Amount = message.ExchangeRate.Amount,
                        Created = TimeZoneInfo.ConvertTimeToUtc(message.ExchangeRate.Created),
                        Updated = TimeZoneInfo.ConvertTimeToUtc(message.ExchangeRate.Updated)
                    });

                    firstExchangeRate = exchangeRateHistoryRepository.GetById(historyId);
                }

                if (message.ExchangeRate.Created > firstExchangeRate.Created)
                    exchangeRateRepository.Update(message.ExchangeRate);

                exchangeRateHistoryRepository.AddSpecific(new ExchangeRateHistory {
                    UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                    ExchangeRateId = message.ExchangeRate.Id,
                    Amount = message.ExchangeRate.Amount,
                    Created = TimeZoneInfo.ConvertTimeToUtc(message.ExchangeRate.Created),
                    Updated = TimeZoneInfo.ConvertTimeToUtc(message.ExchangeRate.Updated)
                });

                message.ExchangeRate = exchangeRateRepository.GetByNetId(message.ExchangeRate.NetUid);

                message.ExchangeRate.ExchangeRateHistories = exchangeRateHistoryRepository.GetAllByExchangeRateId(message.ExchangeRate.Id, 10, 0).ToList();
            }

        Sender.Tell(message.ExchangeRate);
    }

    private void ProcessDeleteExchangeRateMessage(DeleteExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).Remove(message.NetId);
    }
}