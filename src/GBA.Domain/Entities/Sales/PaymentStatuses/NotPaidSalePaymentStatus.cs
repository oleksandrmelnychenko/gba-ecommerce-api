using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.PaymentStatuses;

public sealed class NotPaidSalePaymentStatus : BaseSalePaymentStatus {
    public NotPaidSalePaymentStatus() {
        SalePaymentStatusType = SalePaymentStatusType.NotPaid;
    }
}