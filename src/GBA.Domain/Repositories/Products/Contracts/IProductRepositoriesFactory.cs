using System.Data;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductRepositoriesFactory {
    IProductRepository NewProductRepository(IDbConnection connection);

    IGetSingleProductRepository NewGetSingleProductRepository(IDbConnection connection);

    IGetMultipleProductsRepository NewGetMultipleProductsRepository(IDbConnection connection);

    IProductCategoryRepository NewProductCategoryRepository(IDbConnection connection);

    IProductOriginalNumberRepository NewProductOriginalNumberRepository(IDbConnection connection);

    IProductGroupRepository NewProductGroupRepository(IDbConnection connection);

    IProductProductGroupRepository NewProductProductGroupRepository(IDbConnection connection);

    IProductAnalogueRepository NewProductAnalogueRepository(IDbConnection connection);

    IProductSetRepository NewProductSetRepository(IDbConnection connection);

    IProductPricingRepository NewProductPricingRepository(IDbConnection connection);

    IProductSubGroupRepository NewProductSubGroupRepository(IDbConnection connection);

    IProductGroupDiscountRepository NewProductGroupDiscountRepository(IDbConnection connection);

    IProductReservationRepository NewProductReservationRepository(IDbConnection connection);

    IProductAvailabilityRepository NewProductAvailabilityRepository(IDbConnection connection);

    IProductSpecificationRepository NewProductSpecificationRepository(IDbConnection connection);

    IProductImageRepository NewProductImageRepository(IDbConnection connection);

    IProductIncomeRepository NewProductIncomeRepository(IDbConnection connection);

    IProductIncomeItemRepository NewProductIncomeItemRepository(IDbConnection connection);

    IProductWriteOffRuleRepository NewProductWriteOffRuleRepository(IDbConnection connection);

    IProductTransferRepository NewProductTransferRepository(IDbConnection connection);

    IProductTransferItemRepository NewProductTransferItemRepository(IDbConnection connection);

    IProductPlacementRepository NewProductPlacementRepository(IDbConnection connection);

    IProductPlacementHistoryRepository NewProductPlacementHistoryRepository(IDbConnection connection);

    IProductLocationRepository NewProductLocationRepository(IDbConnection connection);
    IProductLocationHistoryRepository NewProductLocationHistoryRepository(IDbConnection connection);

    IProductPlacementMovementRepository NewProductPlacementMovementRepository(IDbConnection connection);

    IProductPlacementStorageRepository NewProductPlacementStorageRepository(IDbConnection connection);

    IProductCapitalizationRepository NewProductCapitalizationRepository(IDbConnection connection);

    IProductCapitalizationItemRepository NewProductCapitalizationItemRepository(IDbConnection connection);

    ICarBrandRepository NewCarBrandRepository(IDbConnection connection);

    IProductAvailabilityCartLimitsRepository NewProductAvailabilityCartLimitsRepository(IDbConnection connection);
}