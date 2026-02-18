using System.Data;
using GBA.Domain.Repositories.Currencies.Contracts;

namespace GBA.Domain.Repositories.Currencies;

public sealed class CurrencyRepositoriesFactory : ICurrencyRepositoriesFactory {
    public ICurrencyRepository NewCurrencyRepository(IDbConnection connection) {
        return new CurrencyRepository(connection);
    }

    public ICurrencyTranslationRepository NewCurrencyTranslationRepository(IDbConnection connection) {
        return new CurrencyTranslationRepository(connection);
    }

    public ICurrencyTraderRepository NewCurrencyTraderRepository(IDbConnection connection) {
        return new CurrencyTraderRepository(connection);
    }

    public ICurrencyTraderExchangeRateRepository NewCurrencyTraderExchangeRateRepository(IDbConnection connection) {
        return new CurrencyTraderExchangeRateRepository(connection);
    }
}