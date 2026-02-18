using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.Entities.Sales.PaymentStatuses;

public class BaseSalePaymentStatus : EntityBase {
    public BaseSalePaymentStatus() {
        Sales = new HashSet<Sale>();

        ReSales = new HashSet<ReSale>();
    }

    public SalePaymentStatusType SalePaymentStatusType { get; set; }

    public decimal Amount { get; set; }

    public virtual ICollection<Sale> Sales { get; set; }

    public virtual ICollection<ReSale> ReSales { get; set; }
}