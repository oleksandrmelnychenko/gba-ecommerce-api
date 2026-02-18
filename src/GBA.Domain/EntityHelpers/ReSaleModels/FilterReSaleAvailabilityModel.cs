using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class FilterReSaleAvailabilityModel {
    public decimal ExtraChargePercent { get; set; }

    public string Search { get; set; }

    public IEnumerable<long> IncludedProductGroups { get; set; }

    public IEnumerable<long> IncludedStorages { get; set; }

    public IEnumerable<string> IncludedSpecificationCodes { get; set; }
}