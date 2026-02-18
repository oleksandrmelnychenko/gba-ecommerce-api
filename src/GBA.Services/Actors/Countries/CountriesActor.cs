using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Countries;
using GBA.Domain.Repositories.Countries.Contracts;

namespace GBA.Services.Actors.Countries;

public sealed class CountriesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICountryRepositoryFactory _countryRepositoryFactory;

    public CountriesActor(
        IDbConnectionFactory connectionFactory,
        ICountryRepositoryFactory countryRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _countryRepositoryFactory = countryRepositoryFactory;

        Receive<GetAllCountriesMessage>(ProcessGetAllCountriesMessage);

        Receive<AddNewCountryMessage>(ProcessAddNewCountryMessage);

        Receive<UpdateCountryMessage>(ProcessUpdateCountryMessage);

        Receive<GetCountryByNetIdMessage>(ProcessGetCountryByNetIdMessage);

        Receive<DeleteCountryByNetIdMessage>(ProcessDeleteCountryByNetIdMessage);
    }

    private void ProcessGetAllCountriesMessage(GetAllCountriesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _countryRepositoryFactory
                .NewCountryRepository(connection)
                .GetAll()
        );
    }

    private void ProcessAddNewCountryMessage(AddNewCountryMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICountryRepository countryRepository = _countryRepositoryFactory.NewCountryRepository(connection);

        message.Country.Id = countryRepository.Add(message.Country);

        Sender.Tell(
            countryRepository
                .GetById(
                    message.Country.Id
                )
        );
    }

    private void ProcessUpdateCountryMessage(UpdateCountryMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICountryRepository countryRepository = _countryRepositoryFactory.NewCountryRepository(connection);

        countryRepository.Update(message.Country);

        Sender.Tell(
            countryRepository
                .GetById(
                    message.Country.Id
                )
        );
    }

    private void ProcessGetCountryByNetIdMessage(GetCountryByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _countryRepositoryFactory
                .NewCountryRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessDeleteCountryByNetIdMessage(DeleteCountryByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _countryRepositoryFactory
            .NewCountryRepository(connection)
            .Remove(message.NetId);
    }
}