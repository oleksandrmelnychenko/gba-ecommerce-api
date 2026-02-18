using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.PaymentStatuses;

public sealed class PartialPaidSalePaymentStatus : BaseSalePaymentStatus {
    public PartialPaidSalePaymentStatus() {
        SalePaymentStatusType = SalePaymentStatusType.PartialPaid;
    }
}