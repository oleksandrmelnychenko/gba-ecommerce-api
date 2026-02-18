using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.LifeCycleStatuses;

public sealed class ShippingSaleLifeCycleStatus : BaseLifeCycleStatus {
    public ShippingSaleLifeCycleStatus() {
        SaleLifeCycleType = SaleLifeCycleType.Shipping;
    }
}