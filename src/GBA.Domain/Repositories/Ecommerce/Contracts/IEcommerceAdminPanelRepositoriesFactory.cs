using System.Data;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceAdminPanelRepositoriesFactory {
    IEcommerceContactInfoRepository NewEcommerceContactInfoRepository(IDbConnection connection);

    IEcommerceContactsRepository NewEcommerceContactsRepository(IDbConnection connection);

    IEcommercePageRepository NewEcommercePageRepository(IDbConnection connection);

    IEcommerceDefaultPricingRepository NewEcommerceDefaultPricingRepository(IDbConnection connection);

    IEcommerceRetailPaymentTypeTranslateRepository NewEcommercePaymentTypeRepository(IDbConnection connection);

    IEcommerceRegionRepository NewEcommerceRegionRepository(IDbConnection connection);
}