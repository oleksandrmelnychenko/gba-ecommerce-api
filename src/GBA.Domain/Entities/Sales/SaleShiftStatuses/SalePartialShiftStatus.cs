using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.SaleShiftStatuses;

public sealed class SalePartialShiftStatus : SaleBaseShiftStatus {
    public SalePartialShiftStatus() {
        ShiftStatus = SaleShiftStatus.Partial;
    }
}