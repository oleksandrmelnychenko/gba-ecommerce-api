using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.Entities.Sales.LifeCycleStatuses;

public class BaseLifeCycleStatus : EntityBase {
    public BaseLifeCycleStatus() {
        Sales = new HashSet<Sale>();

        ReSales = new HashSet<ReSale>();
    }

    public SaleLifeCycleType SaleLifeCycleType { get; set; }

    public virtual ICollection<Sale> Sales { get; set; }

    public virtual ICollection<ReSale> ReSales { get; set; }
}