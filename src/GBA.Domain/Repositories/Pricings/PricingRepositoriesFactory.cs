using System.Data;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Domain.Repositories.Pricings;

public sealed class PricingRepositoriesFactory : IPricingRepositoriesFactory {
    public IPriceTypeRepository NewPriceTypeRepository(IDbConnection connection) {
        return new PriceTypeRepository(connection);
    }

    public IPricingRepository NewPricingRepository(IDbConnection connection) {
        return new PricingRepository(connection);
    }

    public IPriceTypeTranslationRepository NewPriceTypeTranslationRepository(IDbConnection connection) {
        return new PriceTypeTranslationRepository(connection);
    }

    public IPricingTranslationRepository NewPricingTranslationRepository(IDbConnection connection) {
        return new PricingTranslationRepository(connection);
    }

    public IProviderPricingRepository NewProviderPricingRepository(IDbConnection connection) {
        return new ProviderPricingRepository(connection);
    }
}