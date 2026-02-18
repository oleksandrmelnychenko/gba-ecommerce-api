using System.Data;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductRepositoriesFactory : IProductRepositoriesFactory {
    public IProductAnalogueRepository NewProductAnalogueRepository(IDbConnection connection) {
        return new ProductAnalogueRepository(connection);
    }

    public IProductCategoryRepository NewProductCategoryRepository(IDbConnection connection) {
        return new ProductCategoryRepository(connection);
    }

    public IProductGroupRepository NewProductGroupRepository(IDbConnection connection) {
        return new ProductGroupRepository(connection);
    }

    public IProductOriginalNumberRepository NewProductOriginalNumberRepository(IDbConnection connection) {
        return new ProductOriginalNumberRepository(connection);
    }

    public IProductPricingRepository NewProductPricingRepository(IDbConnection connection) {
        return new ProductPricingRepository(connection);
    }

    public IProductProductGroupRepository NewProductProductGroupRepository(IDbConnection connection) {
        return new ProductProductGroupRepository(connection);
    }

    public IProductRepository NewProductRepository(IDbConnection connection) {
        return new ProductRepository(connection);
    }

    public IGetSingleProductRepository NewGetSingleProductRepository(IDbConnection connection) {
        return new GetSingleProductRepository(connection);
    }

    public IGetMultipleProductsRepository NewGetMultipleProductsRepository(IDbConnection connection) {
        return new GetMultipleProductsRepository(connection);
    }

    public IProductSetRepository NewProductSetRepository(IDbConnection connection) {
        return new ProductSetRepository(connection);
    }

    public IProductSubGroupRepository NewProductSubGroupRepository(IDbConnection connection) {
        return new ProductSubGroupRepository(connection);
    }

    public IProductGroupDiscountRepository NewProductGroupDiscountRepository(IDbConnection connection) {
        return new ProductGroupDiscountRepository(connection);
    }

    public IProductReservationRepository NewProductReservationRepository(IDbConnection connection) {
        return new ProductReservationRepository(connection);
    }

    public IProductAvailabilityRepository NewProductAvailabilityRepository(IDbConnection connection) {
        return new ProductAvailabilityRepository(connection);
    }

    public IProductSpecificationRepository NewProductSpecificationRepository(IDbConnection connection) {
        return new ProductSpecificationRepository(connection);
    }

    public IProductImageRepository NewProductImageRepository(IDbConnection connection) {
        return new ProductImageRepository(connection);
    }

    public IProductIncomeRepository NewProductIncomeRepository(IDbConnection connection) {
        return new ProductIncomeRepository(connection);
    }

    public IProductIncomeItemRepository NewProductIncomeItemRepository(IDbConnection connection) {
        return new ProductIncomeItemRepository(connection);
    }

    public IProductWriteOffRuleRepository NewProductWriteOffRuleRepository(IDbConnection connection) {
        return new ProductWriteOffRuleRepository(connection);
    }

    public IProductTransferRepository NewProductTransferRepository(IDbConnection connection) {
        return new ProductTransferRepository(connection);
    }

    public IProductTransferItemRepository NewProductTransferItemRepository(IDbConnection connection) {
        return new ProductTransferItemRepository(connection);
    }

    public IProductPlacementRepository NewProductPlacementRepository(IDbConnection connection) {
        return new ProductPlacementRepository(connection);
    }

    public IProductPlacementHistoryRepository NewProductPlacementHistoryRepository(IDbConnection connection) {
        return new ProductPlacementHistoryRepository(connection);
    }

    public IProductLocationRepository NewProductLocationRepository(IDbConnection connection) {
        return new ProductLocationRepository(connection);
    }

    public IProductLocationHistoryRepository NewProductLocationHistoryRepository(IDbConnection connection) {
        return new ProductLocationHistoryRepository(connection);
    }

    public IProductPlacementMovementRepository NewProductPlacementMovementRepository(IDbConnection connection) {
        return new ProductPlacementMovementRepository(connection);
    }

    public IProductPlacementStorageRepository NewProductPlacementStorageRepository(IDbConnection connection) {
        return new ProductPlacementStorageRepository(connection);
    }

    public IProductCapitalizationRepository NewProductCapitalizationRepository(IDbConnection connection) {
        return new ProductCapitalizationRepository(connection);
    }

    public IProductCapitalizationItemRepository NewProductCapitalizationItemRepository(IDbConnection connection) {
        return new ProductCapitalizationItemRepository(connection);
    }

    public ICarBrandRepository NewCarBrandRepository(IDbConnection connection) {
        return new CarBrandRepository(connection);
    }

    public IProductAvailabilityCartLimitsRepository NewProductAvailabilityCartLimitsRepository(IDbConnection connection) {
        return new ProductAvailabilityCartLimitsRepository(connection);
    }
}