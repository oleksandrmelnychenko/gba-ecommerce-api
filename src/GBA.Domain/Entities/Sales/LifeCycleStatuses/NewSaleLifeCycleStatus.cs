using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.LifeCycleStatuses;

public sealed class NewSaleLifeCycleStatus : BaseLifeCycleStatus {
    public NewSaleLifeCycleStatus() {
        SaleLifeCycleType = SaleLifeCycleType.New;
    }
}