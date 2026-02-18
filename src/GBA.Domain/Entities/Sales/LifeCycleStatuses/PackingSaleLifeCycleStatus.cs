using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.LifeCycleStatuses;

public sealed class PackingSaleLifeCycleStatus : BaseLifeCycleStatus {
    public PackingSaleLifeCycleStatus() {
        SaleLifeCycleType = SaleLifeCycleType.Packaging;
    }
}