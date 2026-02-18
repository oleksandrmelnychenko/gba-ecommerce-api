using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.PaymentStatuses;

public sealed class OverpaidSalePaymentStatus : BaseSalePaymentStatus {
    public OverpaidSalePaymentStatus() {
        SalePaymentStatusType = SalePaymentStatusType.Overpaid;
    }
}