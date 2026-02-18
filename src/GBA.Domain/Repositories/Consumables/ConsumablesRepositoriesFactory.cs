using System.Data;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumablesRepositoriesFactory : IConsumablesRepositoriesFactory {
    public IConsumableProductCategoryRepository NewConsumableProductCategoryRepository(IDbConnection connection) {
        return new ConsumableProductCategoryRepository(connection);
    }

    public IConsumableProductCategoryTranslationRepository NewConsumableProductCategoryTranslationRepository(IDbConnection connection) {
        return new ConsumableProductCategoryTranslationRepository(connection);
    }

    public IConsumableProductRepository NewConsumableProductRepository(IDbConnection connection) {
        return new ConsumableProductRepository(connection);
    }

    public IConsumableProductTranslationRepository NewConsumableProductTranslationRepository(IDbConnection connection) {
        return new ConsumableProductTranslationRepository(connection);
    }

    public IConsumablesOrderRepository NewConsumablesOrderRepository(IDbConnection connection) {
        return new ConsumablesOrderRepository(connection);
    }

    public IConsumablesOrderItemRepository NewConsumablesOrderItemRepository(IDbConnection connection) {
        return new ConsumablesOrderItemRepository(connection);
    }

    public IConsumablesStorageRepository NewConsumablesStorageRepository(IDbConnection connection) {
        return new ConsumablesStorageRepository(connection);
    }

    public IDepreciatedConsumableOrderRepository NewDepreciatedConsumableOrderRepository(IDbConnection connection) {
        return new DepreciatedConsumableOrderRepository(connection);
    }

    public IDepreciatedConsumableOrderItemRepository NewDepreciatedConsumableOrderItemRepository(IDbConnection connection) {
        return new DepreciatedConsumableOrderItemRepository(connection);
    }

    public ICompanyCarRepository NewCompanyCarRepository(IDbConnection connection) {
        return new CompanyCarRepository(connection);
    }

    public ICompanyCarRoadListRepository NewCompanyCarRoadListRepository(IDbConnection connection) {
        return new CompanyCarRoadListRepository(connection);
    }

    public ICompanyCarFuelingRepository NewCompanyCarFuelingRepository(IDbConnection connection) {
        return new CompanyCarFuelingRepository(connection);
    }

    public ICompanyCarRoadListDriverRepository NewCompanyCarRoadListDriverRepository(IDbConnection connection) {
        return new CompanyCarRoadListDriverRepository(connection);
    }

    public IConsumablesOrderDocumentRepository NewConsumablesOrderDocumentRepository(IDbConnection connection) {
        return new ConsumablesOrderDocumentRepository(connection);
    }
}