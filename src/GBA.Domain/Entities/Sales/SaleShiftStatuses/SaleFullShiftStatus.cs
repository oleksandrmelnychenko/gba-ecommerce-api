using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.SaleShiftStatuses;

public sealed class SaleFullShiftStatus : SaleBaseShiftStatus {
    public SaleFullShiftStatus() {
        ShiftStatus = SaleShiftStatus.Full;
    }
}