using System;

namespace GBA.Domain.EntityHelpers.TotalDashboards.Charts;

public sealed class GroupedPaymentsByPeriod {
    public GroupedPaymentsByPeriod() {
        Vat = new PaymentItem();
        NotVat = new PaymentItem();
    }

    public DateTime Period { get; set; }

    public decimal TotalIncome { get; set; }

    public decimal TotalOutcome { get; set; }

    public PaymentItem Vat { get; set; }

    public PaymentItem NotVat { get; set; }
}