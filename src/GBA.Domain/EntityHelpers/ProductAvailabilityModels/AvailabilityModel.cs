using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.EntityHelpers.ProductAvailabilityModels;

public sealed class AvailabilityModel {
    public Guid NetId { get; set; }

    public string Name { get; set; }

    public double Amount { get; set; }

    public string RegionCode { get; set; }

    public OrderItem OrderItem { get; set; }
    public DateTime Created { get; set; }
}