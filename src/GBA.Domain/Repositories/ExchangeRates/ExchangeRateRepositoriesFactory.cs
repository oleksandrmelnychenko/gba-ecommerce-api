using System.Data;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Domain.Repositories.ExchangeRates;

public sealed class ExchangeRateRepositoriesFactory : IExchangeRateRepositoriesFactory {
    public IExchangeRateRepository NewExchangeRateRepository(IDbConnection connection) {
        return new ExchangeRateRepository(connection);
    }

    public ICrossExchangeRateRepository NewCrossExchangeRateRepository(IDbConnection connection) {
        return new CrossExchangeRateRepository(connection);
    }

    public IExchangeRateHistoryRepository NewExchangeRateHistoryRepository(IDbConnection connection) {
        return new ExchangeRateHistoryRepository(connection);
    }

    public ICrossExchangeRateHistoryRepository NewCrossExchangeRateHistoryRepository(IDbConnection connection) {
        return new CrossExchangeRateHistoryRepository(connection);
    }

    public IGovExchangeRateRepository NewGovExchangeRateRepository(IDbConnection connection) {
        return new GovExchangeRateRepository(connection);
    }

    public IGovExchangeRateHistoryRepository NewGovExchangeRateHistoryRepository(IDbConnection connection) {
        return new GovExchangeRateHistoryRepository(connection);
    }

    public IGovCrossExchangeRateRepository NewGovCrossExchangeRateRepository(IDbConnection connection) {
        return new GovCrossExchangeRateRepository(connection);
    }

    public IGovCrossExchangeRateHistoryRepository NewGovCrossExchangeRateHistoryRepository(IDbConnection connection) {
        return new GovCrossExchangeRateHistoryRepository(connection);
    }
}