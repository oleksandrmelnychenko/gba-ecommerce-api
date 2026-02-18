using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.OrderItemShiftStatuses;

public sealed class OrderItemBillShiftStatus : OrderItemBaseShiftStatus {
    public OrderItemBillShiftStatus() {
        ShiftStatus = OrderItemShiftStatus.Bill;
    }
}