using System.Data;

namespace GBA.Domain.Repositories.Currencies.Contracts;

public interface ICurrencyRepositoriesFactory {
    ICurrencyRepository NewCurrencyRepository(IDbConnection connection);

    ICurrencyTranslationRepository NewCurrencyTranslationRepository(IDbConnection connection);

    ICurrencyTraderRepository NewCurrencyTraderRepository(IDbConnection connection);

    ICurrencyTraderExchangeRateRepository NewCurrencyTraderExchangeRateRepository(IDbConnection connection);
}