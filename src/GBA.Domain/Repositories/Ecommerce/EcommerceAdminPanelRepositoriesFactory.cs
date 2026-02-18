using System.Data;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommerceAdminPanelRepositoriesFactory : IEcommerceAdminPanelRepositoriesFactory {
    public IEcommerceContactInfoRepository NewEcommerceContactInfoRepository(IDbConnection connection) {
        return new EcommerceContactInfoRepository(connection);
    }

    public IEcommerceContactsRepository NewEcommerceContactsRepository(IDbConnection connection) {
        return new EcommerceContactsRepository(connection);
    }

    public IEcommercePageRepository NewEcommercePageRepository(IDbConnection connection) {
        return new EcommercePageRepository(connection);
    }

    public IEcommerceDefaultPricingRepository NewEcommerceDefaultPricingRepository(IDbConnection connection) {
        return new EcommerceDefaultPricingRepository(connection);
    }

    public IEcommerceRetailPaymentTypeTranslateRepository NewEcommercePaymentTypeRepository(IDbConnection connection) {
        return new EcommerceRetailPaymentTypeTranslateRepository(connection);
    }

    public IEcommerceRegionRepository NewEcommerceRegionRepository(IDbConnection connection) {
        return new EcommerceRegionRepository(connection);
    }
}