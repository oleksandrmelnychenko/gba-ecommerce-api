using System.Data;

namespace GBA.Domain.Repositories.Charts.Contracts;

public interface IChartRepositoriesFactory {
    IExchangeRateChartsRepository NewExchangeRateChartsRepository(IDbConnection connection);

    ICurrencyTraderExchangeRateChartsRepository NewCurrencyTraderExchangeRateChartsRepository(IDbConnection connection);
}