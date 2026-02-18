using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Charts.ExchangeRateCharts;
using GBA.Domain.Repositories.Charts.Contracts;

namespace GBA.Services.Actors.Charts;

public sealed class ExchangeRateChartsActor : ReceiveActor {
    private readonly IChartRepositoriesFactory _chartRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ExchangeRateChartsActor(
        IDbConnectionFactory connectionFactory,
        IChartRepositoriesFactory chartRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _chartRepositoriesFactory = chartRepositoriesFactory;

        Receive<GetForUkrainianExchangeRatesRangedMessage>(ProcessGetForUkrainianExchangeRatesRangedMessage);

        Receive<GetForPolandExchangeRatesRangedMessage>(ProcessGetForPolandExchangeRatesRangedMessage);

        Receive<GetCrossExchangeRatesRangedMessage>(ProcessGetCrossExchangeRatesRangedMessage);
    }

    private void ProcessGetForUkrainianExchangeRatesRangedMessage(GetForUkrainianExchangeRatesRangedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_chartRepositoriesFactory.NewExchangeRateChartsRepository(connection).GetForUkrainianExchangeRatesRanged(message.From, message.To));
    }

    private void ProcessGetForPolandExchangeRatesRangedMessage(GetForPolandExchangeRatesRangedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_chartRepositoriesFactory.NewExchangeRateChartsRepository(connection).GetForPolandExchangeRatesRanged(message.From, message.To));
    }

    private void ProcessGetCrossExchangeRatesRangedMessage(GetCrossExchangeRatesRangedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_chartRepositoriesFactory.NewExchangeRateChartsRepository(connection).GetCrossExchangeRatesRanged(message.From, message.To));
    }
}