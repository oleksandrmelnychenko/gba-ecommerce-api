using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Messages.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.CrossExchangeRates;

public sealed class CrossExchangeRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public CrossExchangeRatesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<UpdateCrossExchangeRateMessage>(ProcessUpdateCrossExchangeRateMessage);
    }

    private void ProcessUpdateCrossExchangeRateMessage(UpdateCrossExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
        ICrossExchangeRateHistoryRepository crossExchangeRateHistoryRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateHistoryRepository(connection);

        CrossExchangeRate crossExchangeRateFromDb = crossExchangeRateRepository.GetByNetId(message.CrossExchangeRate.NetUid);

        if (crossExchangeRateFromDb != null)
            if (!message.CrossExchangeRate.Amount.Equals(crossExchangeRateFromDb.Amount)) {
                CrossExchangeRateHistory firstExchangeRate = crossExchangeRateHistoryRepository.GetFirstByCrossExchangeRateId(message.CrossExchangeRate.Id);

                if (firstExchangeRate != null)
                    if (message.CrossExchangeRate.Created > firstExchangeRate.Created)
                        crossExchangeRateRepository.Update(message.CrossExchangeRate);

                crossExchangeRateHistoryRepository.AddSpecific(new CrossExchangeRateHistory {
                    UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                    Amount = message.CrossExchangeRate.Amount,
                    CrossExchangeRateId = message.CrossExchangeRate.Id,
                    Created = TimeZoneInfo.ConvertTimeToUtc(message.CrossExchangeRate.Created),
                    Updated = TimeZoneInfo.ConvertTimeToUtc(message.CrossExchangeRate.Updated)
                });

                message.CrossExchangeRate = crossExchangeRateRepository.GetByNetId(message.CrossExchangeRate.NetUid);

                message.CrossExchangeRate.CrossExchangeRateHistories =
                    crossExchangeRateHistoryRepository.GetAllByCrossExchangeRateNetId(message.CrossExchangeRate.NetUid, 10, 0);
            }

        Sender.Tell(message.CrossExchangeRate);
    }
}