using System.Data;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IExchangeRateRepositoriesFactory {
    IExchangeRateRepository NewExchangeRateRepository(IDbConnection connection);

    ICrossExchangeRateRepository NewCrossExchangeRateRepository(IDbConnection connection);

    IExchangeRateHistoryRepository NewExchangeRateHistoryRepository(IDbConnection connection);

    ICrossExchangeRateHistoryRepository NewCrossExchangeRateHistoryRepository(IDbConnection connection);

    IGovExchangeRateRepository NewGovExchangeRateRepository(IDbConnection connection);

    IGovExchangeRateHistoryRepository NewGovExchangeRateHistoryRepository(IDbConnection connection);

    IGovCrossExchangeRateRepository NewGovCrossExchangeRateRepository(IDbConnection connection);

    IGovCrossExchangeRateHistoryRepository NewGovCrossExchangeRateHistoryRepository(IDbConnection connection);
}