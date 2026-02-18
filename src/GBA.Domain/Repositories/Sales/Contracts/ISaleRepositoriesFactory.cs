using System.Data;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleRepositoriesFactory {
    IProtocolActEditInvoiceRepository NewProtocolActEditInvoiceRepository(IDbConnection connection);
    IDebtRepository NewDebtRepository(IDbConnection connection);

    IOrderItemRepository NewOrderItemRepository(IDbConnection connection);

    IOrderRepository NewOrderRepository(IDbConnection connection);

    ISaleRepository NewSaleRepository(IDbConnection connection);

    IBaseLifeCycleStatusRepository NewBaseLifeCycleStatusRepository(IDbConnection connection);

    IBaseSalePaymentStatusRepository NewBaseSalePaymentStatusRepository(IDbConnection connection);

    ISaleNumberRepository NewSaleNumberRepository(IDbConnection connection);

    ISaleExchangeRateRepository NewSaleExchangeRateRepository(IDbConnection connection);

    IOrderItemBaseShiftStatusRepository NewOrderItemBaseShiftStatusRepository(IDbConnection connection);
    IHistoryInvoiceEditRepository NewHistoryInvoiceEditRepository(IDbConnection connection);

    ISaleBaseShiftStatusRepository NewSaleBaseShiftStatusRepository(IDbConnection connection);

    ISaleMergedRepository NewSaleMergedRepository(IDbConnection connection);

    IOrderItemMergedRepository NewOrderItemMergedRepository(IDbConnection connection);

    ISaleInvoiceDocumentRepository NewSaleInvoiceDocumentRepository(IDbConnection connection);

    ISaleFutureReservationRepository NewSaleFutureReservationRepository(IDbConnection connection);

    IOrderPackageRepository NewOrderPackageRepository(IDbConnection connection);

    IOrderPackageItemRepository NewOrderPackageItemRepository(IDbConnection connection);

    IOrderPackageUserRepository NewOrderPackageUserRepository(IDbConnection connection);

    ISaleInvoiceNumberRepository NewSaleInvoiceNumberRepository(IDbConnection connection);

    IOrderItemMovementRepository NewOrderItemMovementRepository(IDbConnection connection);

    IPreOrderRepository NewPreOrderRepository(IDbConnection connection);

    IShipmentListRepository NewShipmentListRepository(IDbConnection connection);

    IShipmentListItemRepository NewShipmentListItemRepository(IDbConnection connection);

    IMisplacedSaleRepository NewMisplacedSaleRepository(IDbConnection connection);

    IWarehousesShipmentRepository NewWarehousesShipmentRepository(IDbConnection connection);
}