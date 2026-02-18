using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Messages.ExchangeRates.GovCrossExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.CrossExchangeRates;

public sealed class GovCrossExchangeRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public GovCrossExchangeRatesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<UpdateGovCrossExchangeRateMessage>(ProcessUpdateGovCrossExchangeRateMessage);
    }

    private void ProcessUpdateGovCrossExchangeRateMessage(UpdateGovCrossExchangeRateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository =
            _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
        IGovCrossExchangeRateHistoryRepository govCrossExchangeRateHistoryRepository =
            _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateHistoryRepository(connection);

        GovCrossExchangeRate govCrossExchangeRate =
            govCrossExchangeRateRepository
                .GetByNetId(
                    message.GovCrossExchangeRate.NetUid
                );

        if (govCrossExchangeRate != null)
            if (!message.GovCrossExchangeRate.Amount.Equals(govCrossExchangeRate.Amount)) {
                GovCrossExchangeRateHistory firstExchangeRate = govCrossExchangeRateHistoryRepository.GetFirstByGovCrossExchangeRateId(message.GovCrossExchangeRate.Id);

                if (message.GovCrossExchangeRate.Created > firstExchangeRate.Created)
                    govCrossExchangeRateRepository.Update(message.GovCrossExchangeRate);

                govCrossExchangeRateHistoryRepository.AddSpecific(new GovCrossExchangeRateHistory {
                    UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                    Amount = message.GovCrossExchangeRate.Amount,
                    GovCrossExchangeRateId = message.GovCrossExchangeRate.Id,
                    Created = TimeZoneInfo.ConvertTimeToUtc(message.GovCrossExchangeRate.Created),
                    Updated = TimeZoneInfo.ConvertTimeToUtc(message.GovCrossExchangeRate.Updated)
                });

                govCrossExchangeRate = govCrossExchangeRateRepository.GetByNetId(message.GovCrossExchangeRate.NetUid);

                message.GovCrossExchangeRate.GovCrossExchangeRateHistories =
                    govCrossExchangeRateHistoryRepository
                        .GetAllByGovCrossExchangeRateNetId(
                            message.GovCrossExchangeRate.NetUid,
                            10,
                            0
                        );
            }

        Sender.Tell(govCrossExchangeRate ?? message.GovCrossExchangeRate);
    }
}