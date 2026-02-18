using System.Collections.Generic;
using System.Linq;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class ReSaleAvailabilityWithTotalsModel {
    public ReSaleAvailabilityWithTotalsModel() {
        GroupReSaleAvailabilities = new List<GroupingReSaleAvailabilityModel>();
    }

    public IEnumerable<GroupingReSaleAvailabilityModel> GroupReSaleAvailabilities { get; set; }

    public double TotalQty {
        get {
            return GroupReSaleAvailabilities.Sum(x => x.Qty);
        }
    }

    public decimal TotalValueWithVat {
        get {
            return GroupReSaleAvailabilities.Sum(x => x.TotalAccountingPrice);
        }
    }

    public decimal TotalWithExtraValue {
        get {
            return GroupReSaleAvailabilities.Sum(x => x.TotalSalePrice);
        }
    }
}