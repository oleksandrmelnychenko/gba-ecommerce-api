using System.Data;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumablesRepositoriesFactory {
    IConsumableProductCategoryRepository NewConsumableProductCategoryRepository(IDbConnection connection);

    IConsumableProductCategoryTranslationRepository NewConsumableProductCategoryTranslationRepository(IDbConnection connection);

    IConsumableProductRepository NewConsumableProductRepository(IDbConnection connection);

    IConsumableProductTranslationRepository NewConsumableProductTranslationRepository(IDbConnection connection);

    IConsumablesOrderRepository NewConsumablesOrderRepository(IDbConnection connection);

    IConsumablesOrderItemRepository NewConsumablesOrderItemRepository(IDbConnection connection);

    IConsumablesStorageRepository NewConsumablesStorageRepository(IDbConnection connection);

    IDepreciatedConsumableOrderRepository NewDepreciatedConsumableOrderRepository(IDbConnection connection);

    IDepreciatedConsumableOrderItemRepository NewDepreciatedConsumableOrderItemRepository(IDbConnection connection);

    ICompanyCarRepository NewCompanyCarRepository(IDbConnection connection);

    ICompanyCarRoadListRepository NewCompanyCarRoadListRepository(IDbConnection connection);

    ICompanyCarFuelingRepository NewCompanyCarFuelingRepository(IDbConnection connection);

    ICompanyCarRoadListDriverRepository NewCompanyCarRoadListDriverRepository(IDbConnection connection);

    IConsumablesOrderDocumentRepository NewConsumablesOrderDocumentRepository(IDbConnection connection);
}