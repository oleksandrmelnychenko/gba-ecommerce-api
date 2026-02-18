using System.Data;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyUkraineRepositoriesFactory : ISupplyUkraineRepositoriesFactory {
    public ICarrierStathamRepository NewCarrierStathamRepository(IDbConnection connection) {
        return new CarrierStathamRepository(connection);
    }

    public IStathamCarRepository NewStathamCarRepository(IDbConnection connection) {
        return new StathamCarRepository(connection);
    }

    public ISupplyOrderUkraineRepository NewSupplyOrderUkraineRepository(IDbConnection connection) {
        return new SupplyOrderUkraineRepository(connection);
    }

    public ISupplyOrderUkraineItemRepository NewSupplyOrderUkraineItemRepository(IDbConnection connection) {
        return new SupplyOrderUkraineItemRepository(connection);
    }

    public IActReconciliationRepository NewActReconciliationRepository(IDbConnection connection) {
        return new ActReconciliationRepository(connection);
    }

    public IActReconciliationItemRepository NewActReconciliationItemRepository(IDbConnection connection) {
        return new ActReconciliationItemRepository(connection);
    }

    public IActReconciliationAppliedActionsRepository ActReconciliationAppliedActionsRepository(IDbConnection connection) {
        return new ActReconciliationAppliedActionsRepository(connection);
    }

    public ISupplyOrderUkraineCartItemRepository NewSupplyOrderUkraineCartItemRepository(IDbConnection connection) {
        return new SupplyOrderUkraineCartItemRepository(connection);
    }

    public ITaxFreePackListRepository NewTaxFreePackListRepository(IDbConnection connection, IDbConnection exchangeRateConnection) {
        return new TaxFreePackListRepository(connection, exchangeRateConnection);
    }

    public ITaxFreeRepository NewTaxFreeRepository(IDbConnection connection, IDbConnection exchangeRateConnection) {
        return new TaxFreeRepository(connection, exchangeRateConnection);
    }

    public ITaxFreeItemRepository NewTaxFreeItemRepository(IDbConnection connection) {
        return new TaxFreeItemRepository(connection);
    }

    public IDynamicProductPlacementRepository NewDynamicProductPlacementRepository(IDbConnection connection) {
        return new DynamicProductPlacementRepository(connection);
    }

    public IDynamicProductPlacementRowRepository NewDynamicProductPlacementRowRepository(IDbConnection connection) {
        return new DynamicProductPlacementRowRepository(connection);
    }

    public IDynamicProductPlacementColumnRepository NewDynamicProductPlacementColumnRepository(IDbConnection connection) {
        return new DynamicProductPlacementColumnRepository(connection);
    }

    public ITaxFreeDocumentRepository NewTaxFreeDocumentRepository(IDbConnection connection) {
        return new TaxFreeDocumentRepository(connection);
    }

    public ISadRepository NewSadRepository(IDbConnection connection, IDbConnection exchangeRateConnection) {
        return new SadRepository(connection, exchangeRateConnection);
    }

    public ISadItemRepository NewSadItemRepository(IDbConnection connection) {
        return new SadItemRepository(connection);
    }

    public ISadDocumentRepository NewSadDocumentRepository(IDbConnection connection) {
        return new SadDocumentRepository(connection);
    }

    public ITaxFreePackListOrderItemRepository NewTaxFreePackListOrderItemRepository(IDbConnection connection) {
        return new TaxFreePackListOrderItemRepository(connection);
    }

    public IStathamPassportRepository NewStathamPassportRepository(IDbConnection connection) {
        return new StathamPassportRepository(connection);
    }

    public ISadPalletTypeRepository NewSadPalletTypeRepository(IDbConnection connection) {
        return new SadPalletTypeRepository(connection);
    }

    public ISadPalletRepository NewSadPalletRepository(IDbConnection connection) {
        return new SadPalletRepository(connection);
    }

    public ISadPalletItemRepository NewSadPalletItemRepository(IDbConnection connection) {
        return new SadPalletItemRepository(connection);
    }

    public ISupplyOrderUkrainePaymentDeliveryProtocolKeyRepository NewSupplyOrderUkrainePaymentDeliveryProtocolKeyRepository(IDbConnection connection) {
        return new SupplyOrderUkrainePaymentDeliveryProtocolKeyRepository(connection);
    }

    public ISupplyOrderUkrainePaymentDeliveryProtocolRepository NewSupplyOrderUkrainePaymentDeliveryProtocolRepository(IDbConnection connection) {
        return new SupplyOrderUkrainePaymentDeliveryProtocolRepository(connection);
    }

    public ISupplyOrderUkraineCartItemReservationRepository NewSupplyOrderUkraineCartItemReservationRepository(IDbConnection connection) {
        return new SupplyOrderUkraineCartItemReservationRepository(connection);
    }

    public ISupplyOrderUkraineCartItemReservationProductPlacementRepository NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(IDbConnection connection) {
        return new SupplyOrderUkraineCartItemReservationProductPlacementRepository(connection);
    }

    public ISupplyOrderUkraineDocumentRepository NewSupplyOrderUkraineDocumentRepository(IDbConnection connection) {
        return new SupplyOrderUkraineDocumentRepository(connection);
    }

    public IDeliveryExpenseRepository NewDeliveryExpenseRepository(IDbConnection connection) {
        return new DeliveryExpenseRepository(connection);
    }
}