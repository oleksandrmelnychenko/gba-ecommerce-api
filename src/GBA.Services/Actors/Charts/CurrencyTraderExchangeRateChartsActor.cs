using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Charts.CurrencyTraderExchangeRatesCharts;
using GBA.Domain.Repositories.Charts.Contracts;

namespace GBA.Services.Actors.Charts;

public sealed class CurrencyTraderExchangeRateChartsActor : ReceiveActor {
    private readonly IChartRepositoriesFactory _chartRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public CurrencyTraderExchangeRateChartsActor(
        IDbConnectionFactory connectionFactory,
        IChartRepositoriesFactory chartRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _chartRepositoriesFactory = chartRepositoriesFactory;

        Receive<GetCurrencyTraderExchangeRatesFilteredMessage>(ProcessGetCurrencyTraderExchangeRatesFilteredMessage);
    }

    private void ProcessGetCurrencyTraderExchangeRatesFilteredMessage(GetCurrencyTraderExchangeRatesFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _chartRepositoriesFactory
                .NewCurrencyTraderExchangeRateChartsRepository(connection)
                .GetCurrencyTraderExchangeRatesFiltered(
                    message.From,
                    message.To,
                    message.TraderNetIds,
                    message.Currencies
                )
        );
    }
}