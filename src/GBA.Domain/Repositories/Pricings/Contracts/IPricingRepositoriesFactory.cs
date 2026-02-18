using System.Data;

namespace GBA.Domain.Repositories.Pricings.Contracts;

public interface IPricingRepositoriesFactory {
    IPricingRepository NewPricingRepository(IDbConnection connection);

    IPriceTypeRepository NewPriceTypeRepository(IDbConnection connection);

    IPriceTypeTranslationRepository NewPriceTypeTranslationRepository(IDbConnection connection);

    IPricingTranslationRepository NewPricingTranslationRepository(IDbConnection connection);

    IProviderPricingRepository NewProviderPricingRepository(IDbConnection connection);
}