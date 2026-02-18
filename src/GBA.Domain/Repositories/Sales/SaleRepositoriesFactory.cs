using System.Data;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Sales.OrderPackages;
using GBA.Domain.Repositories.Sales.Shipments;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleRepositoriesFactory : ISaleRepositoriesFactory {
    public IBaseLifeCycleStatusRepository NewBaseLifeCycleStatusRepository(IDbConnection connection) {
        return new BaseLifeCycleStatusRepository(connection);
    }

    public IBaseSalePaymentStatusRepository NewBaseSalePaymentStatusRepository(IDbConnection connection) {
        return new BaseSalePaymentStatusRepository(connection);
    }

    public IDebtRepository NewDebtRepository(IDbConnection connection) {
        return new DebtRepository(connection);
    }

    public IOrderItemRepository NewOrderItemRepository(IDbConnection connection) {
        return new OrderItemRepository(connection);
    }

    public IOrderRepository NewOrderRepository(IDbConnection connection) {
        return new OrderRepository(connection);
    }

    public ISaleExchangeRateRepository NewSaleExchangeRateRepository(IDbConnection connection) {
        return new SaleExchangeRateRepository(connection);
    }

    public ISaleNumberRepository NewSaleNumberRepository(IDbConnection connection) {
        return new SaleNumberRepository(connection);
    }

    public ISaleRepository NewSaleRepository(IDbConnection connection) {
        return new SaleRepository(connection);
    }

    public IOrderItemBaseShiftStatusRepository NewOrderItemBaseShiftStatusRepository(IDbConnection connection) {
        return new OrderItemBaseShiftStatusRepository(connection);
    }

    public ISaleBaseShiftStatusRepository NewSaleBaseShiftStatusRepository(IDbConnection connection) {
        return new SaleBaseShiftStatusRepository(connection);
    }

    public ISaleMergedRepository NewSaleMergedRepository(IDbConnection connection) {
        return new SaleMergedRepository(connection);
    }

    public IOrderItemMergedRepository NewOrderItemMergedRepository(IDbConnection connection) {
        return new OrderItemMergedRepository(connection);
    }

    public ISaleInvoiceDocumentRepository NewSaleInvoiceDocumentRepository(IDbConnection connection) {
        return new SaleInvoiceDocumentRepository(connection);
    }

    public ISaleFutureReservationRepository NewSaleFutureReservationRepository(IDbConnection connection) {
        return new SaleFutureReservationRepository(connection);
    }

    public IOrderPackageRepository NewOrderPackageRepository(IDbConnection connection) {
        return new OrderPackageRepository(connection);
    }

    public IOrderPackageItemRepository NewOrderPackageItemRepository(IDbConnection connection) {
        return new OrderPackageItemRepository(connection);
    }

    public IOrderPackageUserRepository NewOrderPackageUserRepository(IDbConnection connection) {
        return new OrderPackageUserRepository(connection);
    }

    public ISaleInvoiceNumberRepository NewSaleInvoiceNumberRepository(IDbConnection connection) {
        return new SaleInvoiceNumberRepository(connection);
    }

    public IOrderItemMovementRepository NewOrderItemMovementRepository(IDbConnection connection) {
        return new OrderItemMovementRepository(connection);
    }

    public IPreOrderRepository NewPreOrderRepository(IDbConnection connection) {
        return new PreOrderRepository(connection);
    }

    public IShipmentListRepository NewShipmentListRepository(IDbConnection connection) {
        return new ShipmentListRepository(connection);
    }

    public IShipmentListItemRepository NewShipmentListItemRepository(IDbConnection connection) {
        return new ShipmentListItemRepository(connection);
    }

    public IMisplacedSaleRepository NewMisplacedSaleRepository(IDbConnection connection) {
        return new MisplacedSaleRepository(connection);
    }

    public IHistoryInvoiceEditRepository NewHistoryInvoiceEditRepository(IDbConnection connection) {
        return new HistoryInvoiceEditRepository(connection);
    }

    public IProtocolActEditInvoiceRepository NewProtocolActEditInvoiceRepository(IDbConnection connection) {
        return new ProtocolActEditInvoiceRepository(connection);
    }

    public IWarehousesShipmentRepository NewWarehousesShipmentRepository(IDbConnection connection) {
        return new WarehousesShipmentRepository(connection);
    }
}