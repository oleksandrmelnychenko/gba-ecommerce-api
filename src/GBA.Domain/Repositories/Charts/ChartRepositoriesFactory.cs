using System.Data;
using GBA.Domain.Repositories.Charts.Contracts;

namespace GBA.Domain.Repositories.Charts;

public sealed class ChartRepositoriesFactory : IChartRepositoriesFactory {
    public IExchangeRateChartsRepository NewExchangeRateChartsRepository(IDbConnection connection) {
        return new ExchangeRateChartsRepository(connection);
    }

    public ICurrencyTraderExchangeRateChartsRepository NewCurrencyTraderExchangeRateChartsRepository(IDbConnection connection) {
        return new CurrencyTraderExchangeRateChartsRepository(connection);
    }
}