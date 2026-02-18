using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.PaymentStatuses;

public sealed class RefundSalePaymentStatus : BaseSalePaymentStatus {
    public RefundSalePaymentStatus() {
        SalePaymentStatusType = SalePaymentStatusType.Refund;
    }
}