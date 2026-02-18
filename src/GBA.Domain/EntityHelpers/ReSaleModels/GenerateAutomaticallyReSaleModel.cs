using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class GenerateAutomaticallyReSaleModel {
    public decimal Amount { get; set; }

    public decimal PossibleAmountDistinct { get; set; }

    public decimal ExtraChargePercent { get; set; }

    public string Search { get; set; }

    public Guid SelectedStorageNetId { get; set; }

    public IEnumerable<long> IncludedProductGroups { get; set; }

    public IEnumerable<long> IncludedStorages { get; set; }

    public IEnumerable<string> IncludedSpecificationCodes { get; set; }
}