using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class ReSaleAvailabilityFilterOptions {
    public IEnumerable<string> SpecificationCodes { get; set; } = new List<string>();

    public IEnumerable<Storage> Storages { get; set; } = new List<Storage>();

    public IEnumerable<ProductGroup> ProductGroups { get; set; } = new List<ProductGroup>();
}