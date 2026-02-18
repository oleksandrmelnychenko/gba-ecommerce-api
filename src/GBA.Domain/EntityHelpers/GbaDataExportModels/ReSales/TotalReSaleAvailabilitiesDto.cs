using System.Collections.Generic;
using System.Linq;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels.ReSales;

public sealed class TotalReSaleAvailabilitiesDto {
    public TotalReSaleAvailabilitiesDto() {
        GroupReSaleAvailabilities = new List<ReSaleAvailabilityDto>();
    }

    public IEnumerable<ReSaleAvailabilityDto> GroupReSaleAvailabilities { get; set; }

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