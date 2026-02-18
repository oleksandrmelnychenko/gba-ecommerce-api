using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceDefaultPricingRepository {
    EcommerceDefaultPricing GetLast();

    long Add(EcommerceDefaultPricing defaultPricing);
}