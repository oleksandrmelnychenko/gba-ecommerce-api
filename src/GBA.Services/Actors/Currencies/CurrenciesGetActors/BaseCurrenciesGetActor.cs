using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Currencies;
using GBA.Domain.Repositories.Currencies.Contracts;

namespace GBA.Services.Actors.Currencies.CurrenciesGetActors;

public sealed class BaseCurrenciesGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;

    public BaseCurrenciesGetActor(IDbConnectionFactory connectionFactory, ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<GetAllCurrenciesMessage>(ProcessGetAllCurrenciesMessage);

        Receive<GetCurrencyByNetIdMessage>(ProcessGetCurrencyByNetIdMessage);
    }

    private void ProcessGetAllCurrenciesMessage(GetAllCurrenciesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_currencyRepositoriesFactory.NewCurrencyRepository(connection).GetAll());
    }

    private void ProcessGetCurrencyByNetIdMessage(GetCurrencyByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_currencyRepositoriesFactory.NewCurrencyRepository(connection).GetByNetId(message.NetId));
    }
}