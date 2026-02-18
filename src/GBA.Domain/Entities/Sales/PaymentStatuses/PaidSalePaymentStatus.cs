using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.PaymentStatuses;

public sealed class PaidSalePaymentStatus : BaseSalePaymentStatus {
    public PaidSalePaymentStatus() {
        SalePaymentStatusType = SalePaymentStatusType.Paid;
    }
}