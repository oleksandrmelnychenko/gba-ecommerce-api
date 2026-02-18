using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class CreatedReSaleAvailabilityWithTotalModel {
    public List<ReSaleAvailabilityItemModel> ReSaleAvailabilityItemModels { get; set; } = new();

    public double Qty { get; set; }

    public decimal Value { get; set; }

    public decimal Vat { get; set; }

    public double Weight { get; set; }

    public Organization Organization { get; set; }
}