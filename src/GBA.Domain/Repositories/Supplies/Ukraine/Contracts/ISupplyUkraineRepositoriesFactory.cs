using System.Data;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyUkraineRepositoriesFactory {
    ICarrierStathamRepository NewCarrierStathamRepository(IDbConnection connection);

    IStathamCarRepository NewStathamCarRepository(IDbConnection connection);

    ISupplyOrderUkraineRepository NewSupplyOrderUkraineRepository(IDbConnection connection);

    ISupplyOrderUkraineItemRepository NewSupplyOrderUkraineItemRepository(IDbConnection connection);

    IActReconciliationRepository NewActReconciliationRepository(IDbConnection connection);

    IActReconciliationItemRepository NewActReconciliationItemRepository(IDbConnection connection);

    IActReconciliationAppliedActionsRepository ActReconciliationAppliedActionsRepository(IDbConnection connection);

    ISupplyOrderUkraineCartItemRepository NewSupplyOrderUkraineCartItemRepository(IDbConnection connection);

    ITaxFreePackListRepository NewTaxFreePackListRepository(IDbConnection connection, IDbConnection exchangeRateConnection);

    ITaxFreeRepository NewTaxFreeRepository(IDbConnection connection, IDbConnection exchangeRateConnection);

    ITaxFreeItemRepository NewTaxFreeItemRepository(IDbConnection connection);

    IDynamicProductPlacementRepository NewDynamicProductPlacementRepository(IDbConnection connection);

    IDynamicProductPlacementRowRepository NewDynamicProductPlacementRowRepository(IDbConnection connection);

    IDynamicProductPlacementColumnRepository NewDynamicProductPlacementColumnRepository(IDbConnection connection);

    ITaxFreeDocumentRepository NewTaxFreeDocumentRepository(IDbConnection connection);

    ISadRepository NewSadRepository(IDbConnection connection, IDbConnection exchangeRateConnection);

    ISadItemRepository NewSadItemRepository(IDbConnection connection);

    ISadDocumentRepository NewSadDocumentRepository(IDbConnection connection);

    ITaxFreePackListOrderItemRepository NewTaxFreePackListOrderItemRepository(IDbConnection connection);

    IStathamPassportRepository NewStathamPassportRepository(IDbConnection connection);

    ISadPalletTypeRepository NewSadPalletTypeRepository(IDbConnection connection);

    ISadPalletRepository NewSadPalletRepository(IDbConnection connection);

    ISadPalletItemRepository NewSadPalletItemRepository(IDbConnection connection);

    ISupplyOrderUkrainePaymentDeliveryProtocolKeyRepository NewSupplyOrderUkrainePaymentDeliveryProtocolKeyRepository(IDbConnection connection);

    ISupplyOrderUkrainePaymentDeliveryProtocolRepository NewSupplyOrderUkrainePaymentDeliveryProtocolRepository(IDbConnection connection);

    ISupplyOrderUkraineCartItemReservationRepository NewSupplyOrderUkraineCartItemReservationRepository(IDbConnection connection);

    ISupplyOrderUkraineCartItemReservationProductPlacementRepository NewSupplyOrderUkraineCartItemReservationProductPlacementRepository(IDbConnection connection);

    ISupplyOrderUkraineDocumentRepository NewSupplyOrderUkraineDocumentRepository(IDbConnection connection);

    IDeliveryExpenseRepository NewDeliveryExpenseRepository(IDbConnection connection);
}