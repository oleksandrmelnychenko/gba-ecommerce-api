using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.OrderItemShiftStatuses;

public sealed class OrderItemStoreShiftStatus : OrderItemBaseShiftStatus {
    public OrderItemStoreShiftStatus() {
        ShiftStatus = OrderItemShiftStatus.Store;
    }
}