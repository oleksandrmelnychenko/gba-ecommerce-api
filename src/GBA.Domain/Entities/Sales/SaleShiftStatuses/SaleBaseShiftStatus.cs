using System.Collections.Generic;
using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales.SaleShiftStatuses;

public class SaleBaseShiftStatus : EntityBase {
    public SaleShiftStatus ShiftStatus { get; set; }

    public string Comment { get; set; }

    public virtual ICollection<Sale> Sales { get; set; } = new HashSet<Sale>();
}