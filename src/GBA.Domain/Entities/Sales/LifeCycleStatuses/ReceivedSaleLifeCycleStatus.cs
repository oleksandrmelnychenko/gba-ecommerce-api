using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.LifeCycleStatuses;

public sealed class ReceivedSaleLifeCycleStatus : BaseLifeCycleStatus {
    public ReceivedSaleLifeCycleStatus() {
        SaleLifeCycleType = SaleLifeCycleType.Received;
    }
}